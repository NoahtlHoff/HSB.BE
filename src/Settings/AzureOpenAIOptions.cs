using System.ComponentModel.DataAnnotations;

namespace HSB.BE.Settings
{
	public class AzureOpenAIOptions
	{
		[Required]
		public string Endpoint { get; set; } = default!;

		[Required]
		public string ApiKey { get; set; } = default!;

		[Required]
		public string DeploymentName { get; set; } = default!;

		[Required]
		public string EmbeddingDeploymentName { get; set; } = default!;
	}
}