using Azure;
using Azure.AI.OpenAI;
using HSB.BE.Dtos;
using HSB.BE.Services;
using HSB.BE.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace HSB.BE.Controllers
{
	public class ChatBotController : Controller
	{
		private readonly ChatClient _chatClient;
		private readonly IConversationMemoryService _memoryService;

		public ChatBotController(IOptions<AzureOpenAIOptions> options, IConversationMemoryService memoryService)
		{
			var settings = options.Value;
			var endpoint = new Uri(settings.Endpoint);
			var key = new AzureKeyCredential(settings.ApiKey);
			var deploymentName = settings.DeploymentName;

			AzureOpenAIClient azureClient = new AzureOpenAIClient(endpoint, key);
			_chatClient = azureClient.GetChatClient(deploymentName);
			_memoryService = memoryService;
		}

		[HttpPost("chat")]
		public async Task StreamChat([FromBody] ChatRequestDto request)
		{
			Response.Headers.Append("Cache-Control", "no-cache");
			Response.Headers.ContentType = "text/event-stream";
			var userId = request.UserId; // Add this to your DTO
			var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();
			var userMessage = request.Content;

			// Build smart context with relevant past conversations
			var context = await _memoryService.BuildContextAsync(
				userId,
				conversationId,
				userMessage,
				maxTokens: 2000);

			// Build messages list
			var messages = new List<ChatMessage>
			{
				new SystemChatMessage(@"You are an AI investing advisor assistant. 
					You have access to the user's conversation history. 
					Use past conversations to provide personalized advice based on their 
					previously stated goals, risk tolerance, and preferences.")
			};

			// Add relevant past context if available
			if (context.RelevantPastMessages.Count != 0)
			{
				var pastContext = string.Join("\n", context.RelevantPastMessages.Select(m =>
					$"[From past conversation on {m.Timestamp:MMM d}]: {m.Content}"));

				messages.Add(new SystemChatMessage($"Relevant past conversations:\n{pastContext}"));
			}

			messages.AddRange(context.RecentMessages);

			// Add current user message
			messages.Add(new UserChatMessage(userMessage));


			// Start the streaming completion
			var response = _chatClient.CompleteChatStreamingAsync(messages);
			var assistantResponse = new StringBuilder();

			// Iterate over streamed updates and forward them to the client
			await foreach (StreamingChatCompletionUpdate update in response)
			{
				foreach (ChatMessageContentPart part in update.ContentUpdate)
				{
					var text = part.Text;

					if (!string.IsNullOrEmpty(text))
					{
						assistantResponse.Append(text);
						var escapedText = JsonSerializer.Serialize(text);
						await Response.WriteAsync($"data: {escapedText}\n\n");
						await Response.Body.FlushAsync();
					}
				}
			}

			// Save users message and bots response.
			await _memoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);
			await _memoryService.SaveMessageAsync(userId, conversationId, "assistant", assistantResponse.ToString());

			await Response.CompleteAsync();
		}
	}
}