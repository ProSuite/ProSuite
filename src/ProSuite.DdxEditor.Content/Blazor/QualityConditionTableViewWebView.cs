using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;
using ProSuite.UI.QA.Controls;

namespace ProSuite.DdxEditor.Content.Blazor;

public class QualityConditionTableViewWebView : BlazorWebView, IQualityConditionTableViewControl
{
	public QualityConditionTableViewWebView(IServiceProvider serviceProvider, QualityConditionViewModel viewModel)
	{
		Dock = DockStyle.Fill;
		HostPage = "wwwroot/index.html";
		Services = serviceProvider;

		//QualityCondition qualityCondition = item.GetEntity();
		//var viewModel = new QualityConditionViewModel(qualityCondition);

		// todo use injection instead of parameter?
		// todo pass in as parameter?
		IDictionary<string, object> parameters = new Dictionary<string, object>();
		parameters.Add("Model", viewModel);

		RootComponents.Add<QualityConditionTableViewBlazor>("#app", parameters);
	}

	public void BindToParameterValues(BindingList<ParameterValueListItem> parameterValueItems) { }
}
