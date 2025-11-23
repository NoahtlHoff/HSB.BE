using System.ComponentModel.DataAnnotations;

namespace HSB.BE.Dtos
{
	public class ChatRequestDto
	{
		[StringLength(100)]
		public string Role { get; set; } = string.Empty;   // "system", "user", "assistant"
		[StringLength(5000)]
		public string Content { get; set; } = string.Empty;
		[StringLength(200)]
		public string UserId { get; set; } = string.Empty;
		[StringLength(200)]
		public string ConversationId { get; set; } = string.Empty;
		public ChatSettingsDto Settings { get; set; } = new();
	}
	public class ChatSettingsDto
	{
		[StringLength(100)]
		public string Trader { get; set; } = string.Empty;
		[StringLength(100)]
		public string Strategy { get; set; } = string.Empty;
	}
}