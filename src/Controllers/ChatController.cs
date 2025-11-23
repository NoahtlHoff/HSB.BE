using HSB.BE.Dtos;
using HSB.BE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HSB.BE.Controllers
{
	[ApiController]
	public class ChatController : Controller
	{
		private readonly IChatService _chatBotService;

		public ChatController(IChatService chatBotService)
		{
			_chatBotService = chatBotService;
		}

		[HttpPost("chat")]
		public async Task StreamChat([FromBody] ChatRequestDto request)
		{
			Response.Headers.Append("Cache-Control", "no-cache");
			Response.Headers.ContentType = "text/event-stream";

			await foreach (var update in _chatBotService.StreamChatAsync(request))
			{
				if (update.StartsWith("id:"))
				{
					await Response.WriteAsync($"{update}\n\n");
				}
				else
				{
					var serialized = JsonSerializer.Serialize(update);
					await Response.WriteAsync($"data: {serialized}\n\n");
					await Response.Body.FlushAsync();
				}
			}

			await Response.CompleteAsync();
		}
	}
}