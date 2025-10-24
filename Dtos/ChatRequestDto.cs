namespace HSB.BE.Dtos
{
	public class ChatRequestDto
	{
		public List<ChatMessageDto> Messages { get; set; } = [];
		public class ChatMessageDto
		{
			public string Role { get; set; } = string.Empty;   // "system", "user", "assistant"
			public string Content { get; set; } = string.Empty;
		}
	}
}
