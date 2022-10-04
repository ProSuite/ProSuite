using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public interface IInstanceConfigurationContainerItem
	{
		void AddNewInstanceConfigurationItem();

		// Consider removing this once the QualityConditionItem has been absorbed by the InstanceConfigurationItem
		void CreateCopy([NotNull] QualityConditionItem item);

		void CreateCopy([NotNull] InstanceConfigurationItem item);

		// Consider removing this once the QualityConditionItem has been absorbed by the InstanceConfigurationItem
		bool AssignToCategory([NotNull] ICollection<QualityConditionItem> items,
		                      [NotNull] IWin32Window owner,
		                      [CanBeNull] out DataQualityCategory category);

		bool AssignToCategory([NotNull] ICollection<InstanceConfigurationItem> items,
		                      [NotNull] IWin32Window owner,
		                      [CanBeNull] out DataQualityCategory category);

		[CanBeNull]
		InstanceConfiguration GetInstanceConfiguration<T>(
			[NotNull] EntityItem<T, T> instanceConfigurationItem) where T : InstanceConfiguration;
	}
}
