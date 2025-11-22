using Azure;
using Azure.AI.OpenAI;
using HSB.BE.Dtos;
using HSB.BE.Services;
using HSB.BE.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
			var userId = request.UserId;
			var conversationId = request.ConversationId;
			var userMessage = request.Content;
			var strategy = request.Settings.Strategy;
			var traderType = request.Settings.Trader;

			// Create and save a conversationName if it's a new conversation.
			if (conversationId.IsNullOrEmpty())
			{
				conversationId = Guid.NewGuid().ToString();
				var conversationName = await _memoryService.CreateConversationName(userId, conversationId, userMessage);
			}

			// Build smart context with relevant past conversations
			var context = await _memoryService.BuildContextAsync(
				userId,
				conversationId,
				userMessage,
				maxTokens: 4000);

			var baseMessage = @"
				You are an AI investing advisor assistant.

				You always provide clear, actionable, and specific investment suggestions. 
				You reference real stocks, ETFs, sectors, indices, commodities, and other tradeable assets when giving advice.

				You have access to the user's conversation history and must use it to:
				- Align all recommendations with their stated financial goals.
				- Respect their risk tolerance.
				- Remember and adapt to their past preferences.
				- Maintain consistency across sessions.

				When the user asks for investment advice, you should:
				- Provide specific tickers (e.g., AAPL, MSFT, NVDA, SPY).
				- Suggest clear actions (e.g., consider buying, selling, holding, waiting for a better entry, setting stop-losses).
				- Give rationale behind every recommendation.
				- Mention relevant market conditions, trends, or indicators.

				If the user asks for general information, keep responses concise but still accurate and helpful.
				";

			if (!request.Settings.Strategy.IsNullOrEmpty())
			{
				baseMessage += @$"
				You are advising a {traderType} who wants to use a {strategy} strategy. 
				All investment suggestions must be tailored to this profile.

				Adjust:
				- Time horizons
				- Asset selection
				- Risk level
				- Position sizing
				- Use of technical vs. fundamental indicators

				Your advice should fully align with this trader profile and strategy.
				";

			}

			var messages = new List<ChatMessage>
			{
				new SystemChatMessage(baseMessage)
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

			await Response.WriteAsync($"id: {conversationId}\n\n");
			await Response.Body.FlushAsync();

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