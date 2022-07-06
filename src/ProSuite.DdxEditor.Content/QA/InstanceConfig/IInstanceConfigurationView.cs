using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.Controls;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public interface IInstanceConfigurationView :
		IBoundView<InstanceConfiguration, IInstanceConfigurationObserver>
	{
		Func<object> FindInstanceDescriptorDelegate { get; set; }

		void BindToParameterValues(
			[NotNull] BindingList<ParameterValueListItem> parameterValues);

		void SetDescription(string description);

		void SetParameterDescriptions([CanBeNull] IList<TestParameter> paramList);

		// TODO: Remove specification stuff
		bool HasSelectedQualitySpecificationReferences { get; }

		bool RemoveFromQualitySpecificationsEnabled { get; set; }

		int FirstQualitySpecificationReferenceIndex { get; }

		bool InstanceDescriptorLinkEnabled { get; set; }

		string QualitySpecificationSummary { get; set; }

		IInstanceConfigurationTableViewControl TableViewControl { get; }

		void BindToQualitySpecificationReferences(
			[NotNull] IList<QualitySpecificationReferenceTableRow> tableRows);

		[NotNull]
		IList<QualitySpecificationReferenceTableRow>
			GetSelectedQualitySpecificationReferenceTableRows();

		bool Confirm([NotNull] string message, [NotNull] string title);

		void UpdateScreen();

		void RenderCategory([CanBeNull] string categoryText);

		void SaveState();

		void SelectQualitySpecifications(
			[NotNull] IEnumerable<QualitySpecification> specsToSelect);
	}
}
