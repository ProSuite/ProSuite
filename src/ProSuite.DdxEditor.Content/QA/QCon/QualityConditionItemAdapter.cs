using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon;

public class QualityConditionItemAdapter : QualityConditionItem
{
	public QualityConditionItemAdapter([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
	                                   [NotNull] QualityCondition qualityCondition,
	                                   [CanBeNull] IQualityConditionContainerItem containerItem,
	                                   [NotNull] IRepository<QualityCondition> repository) : base(
		modelBuilder, qualityCondition, containerItem, repository) { }

	protected override Control CreateControlCore(IItemNavigation itemNavigation)
	{
		// https://andrewlock.net/exploring-dotnet-6-part-10-new-dependency-injection-features-in-dotnet-6/
		// https://www.stevejgordon.co.uk/aspnet-core-dependency-injection-what-is-the-iservicecollection
		// https://volosoft.com/Blog/ASP.NET-Core-Dependency-Injection
		// https://github.com/alirizaadiyahsi/aspnet-core-dependency-injection-training

		IServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddBlazorWebView();
		serviceCollection.AddScoped<IQualityConditionPresenterFactory, QualityConditionQualityConditionPresenterFactory>(CreateFactory(itemNavigation));

		return new BlazorWebViewImpl(serviceCollection.BuildServiceProvider());
	}

	private Func<IServiceProvider, QualityConditionQualityConditionPresenterFactory> CreateFactory(IItemNavigation itemNavigation)
	{
		return _ => new QualityConditionQualityConditionPresenterFactory
		            {
			            ItemNavigation = itemNavigation,
			            Item = this
		            };
	}
}
