using HSB.BE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HSB.BE.Controllers
{
	[ApiController]
	[Route("api/conversations")]
	public class ConversationsController : ControllerBase
	{
		private readonly IConversationMemoryService _memoryService;

		public ConversationsController(IConversationMemoryService service)
		{
			_memoryService = service;
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetAllConversationNames()
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId is null)
				return Unauthorized();
			var conversationNames = await _memoryService.GetAllConversationNamesAsync(userId);
			return Ok(conversationNames);
		}

		[Authorize]
		[HttpGet("{conversationId}")]
		public async Task<IActionResult> GetConversationMessagesById(string conversationId)
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId is null)
				return Unauthorized();
			var conversation = await _memoryService.GetAllConversationMessagesAsync(conversationId, userId);
			return conversation != null ? Ok(conversation) : NotFound();
		}
	}
}
