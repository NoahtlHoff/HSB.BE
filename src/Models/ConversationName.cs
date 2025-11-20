using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace HSB.BE.Models
{
	public class ConversationName
	{
		[Required]
		[JsonProperty("id")]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		[Required]
		[JsonProperty("userId")]
		public string UserId { get; set; } = default!;

		[Required]
		[JsonProperty("conversationId")]
		public string ConversationId { get; set; } = default!;

		[Required]
		[JsonProperty("name")]
		public string Name { get; set; } = default!;
	}
}
