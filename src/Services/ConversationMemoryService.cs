using Azure;
using Azure.AI.OpenAI;
using HSB.BE.Models;
using HSB.BE.Settings;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace HSB.BE.Services
{
	public interface IConversationMemoryService
	{
		Task SaveMessageAsync(string userId, string conversationId, string role, string content);
		Task<ConversationContext> BuildContextAsync(string userId, string conversationId, string currentQuery, int maxTokens = 4000);
		Task SummarizeOldMessagesAsync(string userId, string conversationId, int keepRecentCount = 10);
	}

	public class ConversationMemoryService : IConversationMemoryService
	{
		private readonly CosmosClient _cosmosClient;
		private readonly Microsoft.Azure.Cosmos.Container _container;
		private readonly EmbeddingClient _embeddingClient;
		private readonly ChatClient _chatClient;
		private const int EMBEDDING_DIMENSIONS = 1536; // text-embedding-ada-002

		public ConversationMemoryService(
			IOptions<CosmosDbOptions> cosmosOptions,
			IOptions<AzureOpenAIOptions> openAIOptions)
		{
			var cosmosSettings = cosmosOptions.Value;
			_cosmosClient = new CosmosClient(cosmosSettings.Endpoint, cosmosSettings.Key);

			var database = _cosmosClient.GetDatabase(cosmosSettings.DatabaseName);
			_container = database.GetContainer(cosmosSettings.ContainerName);

			var openAISettings = openAIOptions.Value;
			var azureClient = new AzureOpenAIClient(
				new Uri(openAISettings.Endpoint),
				new AzureKeyCredential(openAISettings.ApiKey));

			_embeddingClient = azureClient.GetEmbeddingClient(openAISettings.EmbeddingDeploymentName);
			_chatClient = azureClient.GetChatClient(openAISettings.DeploymentName);
		}

		public async Task SaveMessageAsync(string userId, string conversationId, string role, string content)
		{
			var embedding = await GenerateEmbeddingAsync(content);
			var tokenCount = EstimateTokenCount(content);

			var message = new ConversationMessage
			{
				UserId = userId,
				ConversationId = conversationId,
				Role = role,
				Content = content,
				Embedding = embedding,
				Timestamp = DateTime.UtcNow,
				TokenCount = tokenCount
			};

			await _container.CreateItemAsync(message, new PartitionKey(userId));
		}

		public async Task<ConversationContext> BuildContextAsync(
			string userId,
			string conversationId,
			string currentQuery,
			int maxTokens = 3000)
		{
			var context = new ConversationContext();

			// 1. Get recent messages from current conversation (last 10-20 messages)
			var recentMessages = await GetRecentMessagesAsync(userId, conversationId, count: 20);

			// 2. Calculate tokens used by recent messages
			var recentTokens = recentMessages.Sum(m => m.TokenCount);

			// 3. If we have room, search for semantically relevant past messages
			var remainingTokens = maxTokens - recentTokens - 500; // Reserve 500 for system prompt

			if (remainingTokens > 500)
			{
				var relevantPast = await SearchRelevantPastMessagesAsync(
					userId,
					conversationId,
					currentQuery,
					remainingTokens);
				context.RelevantPastMessages = relevantPast;
			}

			// 4. Check if we need to summarize
			if (recentTokens > maxTokens * 0.6) // If recent messages use >60% of budget
			{
				await SummarizeOldMessagesAsync(userId, conversationId, keepRecentCount: 20);
				recentMessages = await GetRecentMessagesAsync(userId, conversationId, count: 20);
			}

			// 5. Convert to ChatMessage format
			context.RecentMessages = recentMessages.Select(m => m.Role.ToLower() switch
			{
				"user" => (ChatMessage)new UserChatMessage(m.Content),
				"assistant" => new AssistantChatMessage(m.Content),
				_ => new SystemChatMessage(m.Content)
			}).ToList();

			context.TotalTokens = recentMessages.Sum(m => m.TokenCount) +
								 context.RelevantPastMessages.Sum(m => m.TokenCount);

			return context;
		}

		private async Task<List<ConversationMessage>> GetRecentMessagesAsync(
			string userId,
			string conversationId,
			int count)
		{
			var queryDefinition = new QueryDefinition(
				@"SELECT TOP @count * FROM c 
              WHERE c.userId = @userId 
              AND c.conversationId = @conversationId 
              AND (c.summary = null OR c.summary = '')
              ORDER BY c.timestamp DESC")
				.WithParameter("@count", count)
				.WithParameter("@userId", userId)
				.WithParameter("@conversationId", conversationId);

			var results = new List<ConversationMessage>();
			var iterator = _container.GetItemQueryIterator<ConversationMessage>(queryDefinition);

			while (iterator.HasMoreResults)
			{
				var response = await iterator.ReadNextAsync();
				results.AddRange(response);
			}

			results.Reverse(); // Chronological order
			return results;
		}

		private async Task<List<ConversationMessage>> SearchRelevantPastMessagesAsync(
			string userId,
			string conversationId,
			string query,
			int maxTokens)
		{
			var queryEmbedding = await GenerateEmbeddingAsync(query);

			// Search across all user's past conversations, excluding current one
			var queryDefinition = new QueryDefinition(
			@"SELECT TOP 10 c.id, c.content, c.role, c.timestamp, c.conversationId, c.tokenCount,
				VectorDistance(c.embedding, @embedding) AS similarity
				FROM c
				WHERE c.userId = @userId 
				AND c.conversationId != @currentConversationId
				AND c.role = 'user'
				ORDER BY VectorDistance(c.embedding, @embedding)")
				.WithParameter("@embedding", queryEmbedding)
				.WithParameter("@userId", userId)
				.WithParameter("@currentConversationId", conversationId);

			var results = new List<ConversationMessage>();
			var iterator = _container.GetItemQueryIterator<ConversationMessage>(queryDefinition);
			var currentTokens = 0;

			while (iterator.HasMoreResults)
			{
				var response = await iterator.ReadNextAsync();
				foreach (var message in response)
				{
					if (currentTokens + message.TokenCount <= maxTokens)
					{
						results.Add(message);
						currentTokens += message.TokenCount;
					}
					else
					{
						break;
					}
				}
			}

			return results;
		}

		public async Task SummarizeOldMessagesAsync(string userId, string conversationId, int keepRecentCount = 20)
		{
			var allMessages = await GetAllConversationMessagesAsync(userId, conversationId);

			if (allMessages.Count <= keepRecentCount)
				return; // Nothing to summarize

			// Skip summarizing the messages we are keeping.
			var oldMessages = allMessages.Take(allMessages.Count - keepRecentCount).ToList();

			// Create summary
			var messagesToSummarize = string.Join("\n", oldMessages.Select(m =>
				$"{m.Role}: {m.Content}"));

			var summaryPrompt = $@"Summarize this conversation history concisely, preserving key investment topics, 
				preferences, and important context. Keep it under 200 tokens: 
				{messagesToSummarize}";

			var chatMessages = new List<ChatMessage>
				{
					new UserChatMessage(summaryPrompt)
				};

			var response = await _chatClient.CompleteChatAsync(chatMessages);
			var summary = response.Value.Content[0].Text;

			// Store summary as a special message
			var summaryMessage = new ConversationMessage
			{
				UserId = userId,
				ConversationId = conversationId,
				Role = "system",
				Content = $"[Previous conversation summary]: {summary}",
				Embedding = await GenerateEmbeddingAsync(summary),
				Timestamp = oldMessages.First().Timestamp,
				TokenCount = EstimateTokenCount(summary),
				Summary = "summary" // Flag to identify summaries
			};

			await _container.CreateItemAsync(summaryMessage, new PartitionKey(userId));

			// Delete the old individual messages to save storage
			foreach (var oldMessage in oldMessages)
			{
				await _container.DeleteItemAsync<ConversationMessage>(
					oldMessage.Id,
					new PartitionKey(userId));
			}
		}

		private async Task<List<ConversationMessage>> GetAllConversationMessagesAsync(
			string userId,
			string conversationId)
		{
			var queryDefinition = new QueryDefinition(
				@"SELECT * FROM c 
				WHERE c.userId = @userId 
				AND c.conversationId = @conversationId 
				ORDER BY c.timestamp ASC")
				.WithParameter("@userId", userId)
				.WithParameter("@conversationId", conversationId);

			var results = new List<ConversationMessage>();
			var iterator = _container.GetItemQueryIterator<ConversationMessage>(queryDefinition);

			while (iterator.HasMoreResults)
			{
				var response = await iterator.ReadNextAsync();
				results.AddRange(response);
			}

			return results;
		}

		private async Task<float[]> GenerateEmbeddingAsync(string text)
		{
			OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(text);
			float[] vector = embedding.ToFloats().ToArray();
			return vector;
		}

		private int EstimateTokenCount(string text)
		{
			// Rough estimate: ~4 characters per token
			// For production, use tiktoken or similar
			return (int)Math.Ceiling(text.Length / 4.0);
		}
	}
}
