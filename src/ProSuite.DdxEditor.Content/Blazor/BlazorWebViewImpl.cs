using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.Blazor;

public class BlazorWebViewImpl : BlazorWebView
{
	public BlazorWebViewImpl(IServiceProvider serviceProvider)
	{
		Dock = DockStyle.Fill;
		HostPage = "wwwroot/index.html";
		Services = serviceProvider;

		// todo pass in as parameter?
		IDictionary<string, object> parameters = new Dictionary<string, object>();
		parameters.Add("Factory", serviceProvider.GetService<IQualityConditionPresenterFactory>());

		RootComponents.Add<BlazorHost>("#app", parameters);
	}
}
