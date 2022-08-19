using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.UI.QA.Controls;
#if NET6_0
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using ProSuite.DdxEditor.Content.Blazor;
using ProSuite.DomainModel.Core.QA;
#endif

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public static class InstanceConfigurationControlFactory
	{
		public static Control CreateControl([NotNull] QualityConditionItem item,
		                                    [NotNull] IItemNavigation itemNavigation,
		                                    [NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                    [NotNull] TableState tableStateQSpec,
		                                    TableState tableStateIssueFilter)
		{
			// ReSharper disable once JoinDeclarationAndInitializer
			QualityConditionControl control;

#if NET6_0
			IServiceProvider provider = CreateIoCContainer();

			var viewModel =
				new InstanceConfigurationViewModel<QualityCondition>(
					item, modelBuilder.GetTestParameterDatasetProvider(),
					modelBuilder.GetRowFilterConfigurationProvider());

			IInstanceConfigurationTableViewControl blazorControl =
				new QualityConditionBlazor(viewModel, provider);

			control = new QualityConditionControl(tableStateQSpec, tableStateIssueFilter, blazorControl);
#else
			control =
				new QualityConditionControl(tableStateQSpec, tableStateIssueFilter,
				                            new QualityConditionTableViewControl());
#endif
			new QualityConditionPresenter(item, control, itemNavigation);

			return control;
		}

		public static Control CreateControl([NotNull] InstanceConfigurationItem item,
		                                    [NotNull] IItemNavigation itemNavigation,
		                                    [NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                    [NotNull] TableState tableState)
		{
			// ReSharper disable once JoinDeclarationAndInitializer
			InstanceConfigurationControl control;

#if NET6_0
			IServiceProvider provider = CreateIoCContainer();

			var viewModel =
				new InstanceConfigurationViewModel<InstanceConfiguration>(
					item, modelBuilder.GetTestParameterDatasetProvider(),
					modelBuilder.GetRowFilterConfigurationProvider());

			IInstanceConfigurationTableViewControl blazorControl =
				new QualityConditionBlazor(viewModel, provider);

			control = new InstanceConfigurationControl(tableState, blazorControl);
#else
			control =
				new InstanceConfigurationControl(tableState, new QualityConditionTableViewControl());
#endif
			new InstanceConfigurationPresenter(item, control, itemNavigation);

			return control;
		}

		private static IServiceProvider CreateIoCContainer()
		{
#if NET6_0
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddWindowsFormsBlazorWebView();
			serviceCollection.AddSingleton<IEventAggregator>(_ => new EventAggregator());

			IServiceProvider provider = serviceCollection.BuildServiceProvider();
			return provider;
#endif
			throw new NotImplementedException();
		}
	}
}
