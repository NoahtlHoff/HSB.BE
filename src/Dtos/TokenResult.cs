namespace HSB.BE.Dtos
{
	public class TokenResult
	{
		public string Token { get; set; } = default!;
		public DateTime ExpiresAtUtc { get; set; }
	}
}
