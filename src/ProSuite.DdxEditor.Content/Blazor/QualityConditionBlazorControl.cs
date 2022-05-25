using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;
using ProSuite.UI.QA.Controls;

namespace ProSuite.DdxEditor.Content.Blazor;

public class QualityConditionBlazorControl : BlazorWebView, IQualityConditionTableViewControl
{
	public QualityConditionBlazorControl() : this(DI.Provider) { }

	public QualityConditionBlazorControl(IServiceProvider provider)
	{
		Dock = DockStyle.Fill;
		HostPage = "wwwroot/index.html";
		Services = provider;
	}

	public void BindToParameterValues(BindingList<ParameterValueListItem> parameterValueItems) { }

	public void NotifySavedChanges(QualityCondition qualityCondition)
	{
		GetViewModel()?.NotifySavedChanges(qualityCondition);
	}

	protected override void OnCreateControl()
	{
		IDictionary<string, object> parameters = new Dictionary<string, object>();
		parameters.Add("Model", DI.Get<QualityConditionViewModel>());

		RootComponents.Add<QualityConditionTableViewBlazor>("#app", parameters);

		// Note: necessary!
		base.OnCreateControl();
	}

	[CanBeNull]
	private QualityConditionViewModel GetViewModel()
	{
		if (RootComponents.Count == 0)
		{
			return null;
		}

		IDictionary<string, object> parameters = RootComponents[0].Parameters;

		bool success = parameters.TryGetValue("Model", out object parameter);
		Assert.True(success, "No Model parameter");

		return (QualityConditionViewModel) parameter;
	}
}
