using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client
{
	[UsedImplicitly]
	public class ClientChannelConfigs
	{
		[UsedImplicitly]
		public List<ClientChannelConfig> Channels { get; set; } = new List<ClientChannelConfig>();
	}
}
