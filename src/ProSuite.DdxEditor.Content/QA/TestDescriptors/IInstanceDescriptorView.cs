using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal interface IInstanceDescriptorView :
		IBoundView<InstanceDescriptor, IInstanceDescriptorObserver>
	{
		Func<object> FindClassDelegate { get; set; }

		void RefreshFactoryElements();

		void RenderInstanceDescription(string description);

		void RenderInstanceCategories([NotNull] string[] categories);

		void RenderTestParameters([NotNull] IEnumerable<TestParameter> testParameters);

		// TODO: Replace Table Row with instance-config-tableRow
		void BindToInstanceConfigurations(
			[NotNull] IList<ReferencingQualityConditionTableRow> tableRows);

		IList<ReferencingQualityConditionTableRow> GetSelectedQualityConditionTableRows();

		void SaveState();
	}
}
