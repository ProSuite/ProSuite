using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.Controls;

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

		bool GoToInstanceDescriptorEnabled { get; set; }

		string ReferenceingInstancesSummary { get; set; }

		IInstanceConfigurationTableViewControl TableViewControl { get; }

		void BindToInstanceConfigReferences(
			[NotNull] IList<InstanceConfigurationReferenceTableRow> tableRows);

		void RenderCategory([CanBeNull] string categoryText);

		void SaveState();
	}
}
