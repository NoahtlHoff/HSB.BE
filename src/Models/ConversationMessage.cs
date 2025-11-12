using Newtonsoft.Json;
using OpenAI.Chat;
using System.ComponentModel.DataAnnotations;

namespace HSB.BE.Models
{
	public class ConversationMessage
	{
		[JsonProperty("id")]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		[Required]
		[JsonProperty("userId")]
		public string UserId { get; set; } = default!;

		[JsonProperty("conversationId")]
		public string? ConversationId { get; set; }

		[Required]
		[JsonProperty("role")]
		public string Role { get; set; } = default!;  // "user" or "assistant"

		[JsonProperty("content")]
		public string Content { get; set; } = default!;

		[Required]
		[JsonProperty("embedding")]
		public float[] Embedding { get; set; } = default!;


		[JsonProperty("timestamp")]
		public DateTime Timestamp { get; set; }

		[JsonProperty("tokenCount")]
		public int TokenCount { get; set; }

		[JsonProperty("summary")]
		public string? Summary { get; set; } // Flag to see if it's a summary

		[JsonProperty("_partitionKey")]
		public string PartitionKey => UserId; // Partition by user for efficient queries
	}

	public class ConversationContext
	{
		public List<ChatMessage> RecentMessages { get; set; } = [];
		public List<ConversationMessage> RelevantPastMessages { get; set; } = [];
		public string ConversationSummary { get; set; } = default!;
		public int TotalTokens { get; set; }
	}
}
