using System.ComponentModel.DataAnnotations;

namespace HSB.BE.Dtos
{
	public class UserInputDto
	{
		public string? Name { get; set; }
		[Required]
		[EmailAddress(ErrorMessage = "Email must be valid")]
		public string Email { get; set; }

		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
		[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?:(?=.*\d)|(?=.*[^a-zA-Z0-9])).+$", ErrorMessage = "Password must contain lowercase, uppercase, and either a number or a special character.")]
		public string Password { get; set; }
	}
}