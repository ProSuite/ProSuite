using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal interface ITestDescriptorView :
		IBoundView<TestDescriptor, ITestDescriptorObserver>
	{
		Func<object> FindTestFactoryDelegate { get; set; }

		Func<object> FindTestClassDelegate { get; set; }

		Func<object> FindTestConfiguratorDelegate { get; set; }

		void RefreshFactoryElements();

		void RenderTestDescription(string description);

		void RenderTestCategories([NotNull] string[] categories);

		void RenderTestParameters([NotNull] IEnumerable<TestParameter> testParameters);

		void BindToQualityConditions(
			[NotNull] IList<ReferencingQualityConditionTableRow> tableRows);

		IList<ReferencingQualityConditionTableRow> GetSelectedQualityConditionTableRows();

		void SaveState();
	}
}
