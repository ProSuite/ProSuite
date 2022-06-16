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

public class QualityConditionBlazor : BlazorWebView, IQualityConditionTableViewControl
{
	[NotNull] private readonly QualityConditionViewModel _viewModel;

	public QualityConditionBlazor([NotNull] QualityConditionViewModel viewModel,
	                              [NotNull] IServiceProvider provider)
	{
		Assert.ArgumentNotNull(viewModel, nameof(viewModel));
		Assert.ArgumentNotNull(provider, nameof(provider));

		_viewModel = viewModel;

		Dock = DockStyle.Fill;
		HostPage = "wwwroot/index.html";
		Services = provider;
	}

	[Obsolete("not used anymore with .NET 6")]
	public void BindToParameterValues(BindingList<ParameterValueListItem> parameterValueItems) { }

	public void BindTo(QualityCondition qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		_viewModel.BindTo(qualityCondition);
	}

	protected override void OnCreateControl()
	{
		// use this OnCreateControl because constructor is to early

		IDictionary<string, object> parameters = new Dictionary<string, object>();
		parameters.Add("ViewModel", _viewModel);

		RootComponents.Add<QualityConditionTableViewBlazor>("#app", parameters);

		// Note: necessary!
		base.OnCreateControl();
	}
}
