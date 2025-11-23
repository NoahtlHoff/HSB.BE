using OpenAI.Chat;

namespace HSB.BE.Repository
{
	public interface IChatRepository
	{
		IAsyncEnumerable<string> StreamChatCompletion(IEnumerable<ChatMessage> messages);
	}
	public class ChatRepository : IChatRepository
	{
		private readonly ChatClient _chatClient;

		public ChatRepository(ChatClient chatClient)
		{
			_chatClient = chatClient;
		}

		public async IAsyncEnumerable<string> StreamChatCompletion(IEnumerable<ChatMessage> messages)
		{
			var stream = _chatClient.CompleteChatStreamingAsync(messages);

			await foreach (var update in stream)
			{
				foreach (var part in update.ContentUpdate)
				{
					if (!string.IsNullOrEmpty(part.Text))
						yield return part.Text;
				}
			}
		}
	}
}
