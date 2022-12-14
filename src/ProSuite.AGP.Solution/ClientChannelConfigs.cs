using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client;

namespace ProSuite.AGP.Solution
{
	[UsedImplicitly]
	public class ClientChannelConfigs
	{
		[UsedImplicitly]
		public List<ClientChannelConfig> Channels { get; set; } = new List<ClientChannelConfig>();
	}
}
