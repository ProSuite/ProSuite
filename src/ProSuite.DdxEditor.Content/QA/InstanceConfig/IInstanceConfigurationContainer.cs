using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public interface IInstanceConfigurationContainer
	{
		[NotNull]
		IEnumerable<Item> GetInstanceConfigurationItems(
			[NotNull] IInstanceConfigurationContainerItem containerItem);

		DataQualityCategory Category { get; }
	}
}
