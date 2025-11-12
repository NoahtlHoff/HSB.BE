using System.ComponentModel.DataAnnotations;

namespace HSB.BE.Settings
{
	public class CosmosDbOptions
	{
		[Required]
		public string Endpoint { get; set; } = default!;

		[Required]
		public string Key { get; set; } = default!;

		[Required]
		public string DatabaseName { get; set; } = default!;

		[Required]
		public string ContainerName { get; set; } = default!;
	}
}
