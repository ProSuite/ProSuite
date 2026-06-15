using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.Controls;
using Radzen;

namespace ProSuite.DdxEditor.Content.Blazor;

public class QualityConditionBlazor : BlazorWebView, IInstanceConfigurationTableViewControl
{
	[NotNull] private readonly IInstanceConfigurationViewModel _viewModel;

	public QualityConditionBlazor([NotNull] IInstanceConfigurationViewModel viewModel)
	{
		Assert.ArgumentNotNull(viewModel, nameof(viewModel));

		_viewModel = viewModel;

		HostPage = "wwwroot/index.html";
		Services = CreateIoCContainer();
	}

	[Obsolete("not used anymore with .NET 6")]
	public void BindToParameterValues(BindingList<ParameterValueListItem> parameterValueItems) { }

	public void BindTo(InstanceConfiguration qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		_viewModel.BindTo(qualityCondition);
	}

	private static IServiceProvider CreateIoCContainer()
	{
#if NET6_0_OR_GREATER 
		IServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddWindowsFormsBlazorWebView();
		serviceCollection.AddSingleton<IEventAggregator>(_ => new EventAggregator());
		serviceCollection.AddScoped<TooltipService>();
#if DEBUG
		// Allows opening browser developer tools (keyboard shortcut: CTRL + SHIFT + I)
		serviceCollection.AddBlazorWebViewDeveloperTools();
#endif
		return serviceCollection.BuildServiceProvider();
#else
		throw new NotImplementedException();
#endif
	}

	protected override void OnCreateControl()
	{
		// use this OnCreateControl because constructor is to early

		IDictionary<string, object> parameters = new Dictionary<string, object>();
		parameters.Add("ViewModel", _viewModel);

		RootComponents.Add<QualityConditionTableViewBlazor>("#app", parameters);

		Dock = DockStyle.Fill;

		// Note: necessary!
		base.OnCreateControl();
	}

	protected override void Dispose(bool disposing)
	{
		_viewModel.Dispose();

		base.Dispose(disposing);
	}
}
