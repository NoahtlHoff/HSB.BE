using HSB.BE.Dtos;
using HSB.BE.Models;
using HSB.BE.Repository;
using OpenAI.Chat;
using System.Text;

namespace HSB.BE.Services
{
	public interface IChatService
	{
		IAsyncEnumerable<string> StreamChatAsync(ChatRequestDto request);
	}

	public class ChatService : IChatService
	{
		private readonly IChatRepository _chatRepository;
		private readonly IConversationMemoryService _memoryService;
		private readonly IChatTokenService _chatTokenService;

		public ChatService(
			IChatRepository chatRepository,
			IConversationMemoryService memoryService,
			IChatTokenService chatTokenService)
		{
			_chatRepository = chatRepository;
			_memoryService = memoryService;
			_chatTokenService = chatTokenService;
		}

		public async IAsyncEnumerable<string> StreamChatAsync(ChatRequestDto request)
		{
			var userId = request.UserId;
			var conversationId = request.ConversationId;
			var userMessage = request.Content;
			var strategy = request.Settings.Strategy;
			var traderType = request.Settings.Trader;

			if (string.IsNullOrEmpty(conversationId))
			{
				conversationId = Guid.NewGuid().ToString();
				await _memoryService.CreateConversationName(userId, conversationId, userMessage);
				yield return $"id: {conversationId}";
			}

			var context = await _memoryService.BuildContextAsync(
				userId,
				conversationId,
				userMessage,
				maxTokens: 4000);

			string systemPrompt = BuildSystemPrompt(strategy, traderType);
			var messages = BuildMessages(systemPrompt, context, userMessage);


			int tokenCount = 0;
			int totalLength = messages
				.SelectMany(m => m.Content)
				.OfType<ChatMessageContentPart>()
				.Sum(p => p.Text.Length);
			Console.WriteLine($"[ChatService] Total message length: {totalLength} characters");
			var assistantFull = new StringBuilder();

			await foreach (var token in _chatRepository.StreamChatCompletion(messages))
			{
				assistantFull.Append(token);
				yield return token;
			}

			// Save user and assistant messages
			await _memoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);
			await _memoryService.SaveMessageAsync(userId, conversationId, "assistant", assistantFull.ToString());
		}

		private List<ChatMessage> BuildMessages(string prompt, ConversationContext context, string userMessage)
		{
			var messages = new List<ChatMessage>
			{
			new SystemChatMessage(prompt)
			};

			if (context.RelevantPastMessages.Count > 0)
			{
				var past = string.Join("\n", context.RelevantPastMessages.Select(m =>
					$"[From past conversation on {m.Timestamp:MMM d}]: {m.Content}"));

				messages.Add(new SystemChatMessage($"Relevant past conversations:\n{past}"));
			}

			messages.AddRange(context.RecentMessages);
			messages.Add(new UserChatMessage(userMessage));

			return messages;
		}

		private string BuildSystemPrompt(string strategy, string traderType)
		{
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

			if (!string.IsNullOrWhiteSpace(strategy))
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

			return baseMessage;
		}


	}
}
