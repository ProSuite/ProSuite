using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.UI.QA.Controls;
#if NET6_0
using Microsoft.Extensions.DependencyInjection;
using ProSuite.DdxEditor.Content.Blazor;
#endif

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public static class QualityConditionControlFactory
	{
		public static Control CreateControl([NotNull] QualityConditionItem item,
		                                    [NotNull] IItemNavigation itemNavigation,
		                                    [NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                    [NotNull] TableState tableState)
		{
			// ReSharper disable once JoinDeclarationAndInitializer
			QualityConditionControl control;

#if NET6_0
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddBlazorWebView();

			ServiceProvider provider = serviceCollection.BuildServiceProvider();

			var viewModel =
				new QualityConditionViewModel(item, modelBuilder.GetTestParameterDatasetProvider());

			IQualityConditionTableViewControl blazorControl =
				new QualityConditionBlazor(viewModel, provider);

			control = new QualityConditionControl(tableState, blazorControl);
#else
			control =
				new QualityConditionControl(tableState, new QualityConditionTableViewControl());
#endif
			new QualityConditionPresenter(item, control, itemNavigation);

			return control;
		}
	}
}
