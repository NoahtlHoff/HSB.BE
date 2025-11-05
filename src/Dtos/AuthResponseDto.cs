namespace HSB.BE.Dtos
{
	public class AuthResponseDto
	{
		public string Token { get; set; } = default!;
		public DateTime ExpiresAtUtc { get; set; }
		public string Email { get; set; } = default!;
		public int UserId { get; set; }
	}
}
