namespace HSB.BE.Dtos
{
	public class ChatRequestDto
	{
		public string Role { get; set; } = string.Empty;   // "system", "user", "assistant"
		public string Content { get; set; } = string.Empty;
		public string UserId { get; set; } = string.Empty;
		public string ConversationId { get; set; } = string.Empty;
		public ChatSettingsDto Settings { get; set; } = new();
	}
	public class ChatSettingsDto
	{
		public string Trader { get; set; } = string.Empty;
		public string Strategy { get; set; } = string.Empty;
	}
}