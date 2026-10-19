using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public static class KnownWorkspaceFactories
	{
		[NotNull] private static readonly IList<IKnownWorkspaceFactory> _factories =
			new List<IKnownWorkspaceFactory>();

		public static void Register([NotNull] IKnownWorkspaceFactory factory)
		{
			Assert.ArgumentNotNull(factory, nameof(factory));

			lock (_factories)
			{
				if (_factories.Contains(factory))
				{
					return;
				}

				_factories.Add(factory);
			}
		}

		[CanBeNull]
		public static IKnownWorkspaceFactory GetFactory(
			[NotNull] ConnectionProvider connectionProvider)
		{
			Assert.ArgumentNotNull(connectionProvider, nameof(connectionProvider));

			lock (_factories)
			{
				return _factories.FirstOrDefault(f => f.CanOpen(connectionProvider));
			}
		}
	}
}
