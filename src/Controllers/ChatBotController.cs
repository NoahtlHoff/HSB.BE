using Azure;
using Azure.AI.OpenAI;
using HSB.BE.Dtos;
using HSB.BE.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;

namespace HSB.BE.Controllers
{
	public class ChatBotController : Controller
	{
		private readonly ChatClient _chatClient;

		public ChatBotController(IOptions<AzureOpenAIOptions> options)
		{
			var settings = options.Value;
			var endpoint = new Uri(settings.Endpoint);
			var key = new AzureKeyCredential(settings.ApiKey);
			var deploymentName = settings.DeploymentName;

			AzureOpenAIClient azureClient = new AzureOpenAIClient(endpoint, key);
			_chatClient = azureClient.GetChatClient(deploymentName);
		}

		[HttpPost("chat")]
		public async Task StreamChat([FromBody] ChatRequestDto request)
		{
			Response.Headers.Append("Cache-Control", "no-cache");
			Response.Headers.ContentType = "text/event-stream";

			// Convert request messages to OpenAI SDK's ChatMessage class
			List<ChatMessage> messages = request.Messages.Select(m => m.Role.ToLower() switch
			{
				"system" => (ChatMessage)new SystemChatMessage(m.Content),
				"user" => new UserChatMessage(m.Content),
				"assistant" => new AssistantChatMessage(m.Content),
				_ => throw new ArgumentException($"Unknown role: {m.Role}")
			})
			.ToList();

			// Start the streaming completion
			var response = _chatClient.CompleteChatStreamingAsync(messages);

			// Iterate over streamed updates and forward them to the client
			await foreach (StreamingChatCompletionUpdate update in response)
			{
				foreach (ChatMessageContentPart part in update.ContentUpdate)
				{
					var text = part.Text;

					if (!string.IsNullOrEmpty(text))
					{
						var escapedText = JsonSerializer.Serialize(text);
						await Response.WriteAsync($"data: {escapedText}\n\n");
						await Response.Body.FlushAsync();
					}
				}
			}

			await Response.CompleteAsync();
		}
	}
}