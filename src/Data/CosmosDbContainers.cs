using HSB.BE.Settings;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace HSB.BE.Data
{
	public interface ICosmosDbContainers
	{
		Microsoft.Azure.Cosmos.Container ConversationsContainer { get; }
		Microsoft.Azure.Cosmos.Container ConversationNamesContainer { get; }
	}
	public class CosmosDbContainers : ICosmosDbContainers
	{
		public Microsoft.Azure.Cosmos.Container ConversationsContainer { get; }
		public Microsoft.Azure.Cosmos.Container ConversationNamesContainer { get; }
		public CosmosDbContainers(CosmosClient client, IOptions<CosmosDbOptions> options)
		{
			var opt = options.Value;
			Database database = client.GetDatabase(opt.DatabaseName);
			ConversationsContainer = database.GetContainer(opt.ConversationsContainerName);
			ConversationNamesContainer = database.GetContainer(opt.ConversationNamesContainerName);
		}
	}
}
