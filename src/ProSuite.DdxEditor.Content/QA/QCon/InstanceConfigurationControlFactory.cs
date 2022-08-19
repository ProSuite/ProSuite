using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.UI.QA.Controls;
#if NET6_0
using ProSuite.DdxEditor.Content.Blazor;
using ProSuite.DomainModel.Core.QA;
#endif

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public static class InstanceConfigurationControlFactory
	{
		[NotNull]
		public static Control CreateControl([NotNull] QualityConditionItem item,
		                                    [NotNull] IItemNavigation itemNavigation,
		                                    [NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                    [NotNull] TableState tableStateQSpec,
		                                    [NotNull] TableState tableStateIssueFilter)
		{
			// ReSharper disable once JoinDeclarationAndInitializer
			QualityConditionControl control;

#if NET6_0

			var viewModel =
				new InstanceConfigurationViewModel<QualityCondition>(
					item, modelBuilder.GetTestParameterDatasetProvider());

			IInstanceConfigurationTableViewControl blazorControl =
				new QualityConditionBlazor(viewModel);

			control = new QualityConditionControl(tableStateQSpec, tableStateIssueFilter,
			                                      blazorControl);
#else
			control =
				new QualityConditionControl(tableStateQSpec, tableStateIssueFilter,
				                            new QualityConditionTableViewControl());
#endif
			new QualityConditionPresenter(item, control, itemNavigation);

			return control;
		}

		[NotNull]
		public static Control CreateControl([NotNull] InstanceConfigurationItem item,
		                                    [NotNull] IItemNavigation itemNavigation,
		                                    [NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                    [NotNull] TableState tableState)
		{
			// ReSharper disable once JoinDeclarationAndInitializer
			InstanceConfigurationControl control;

#if NET6_0

			var viewModel =
				new InstanceConfigurationViewModel<InstanceConfiguration>(
					item, modelBuilder.GetTestParameterDatasetProvider());

			IInstanceConfigurationTableViewControl blazorControl =
				new QualityConditionBlazor(viewModel);

			control = new InstanceConfigurationControl(tableState, blazorControl);
#else
			control =
				new InstanceConfigurationControl(tableState, new QualityConditionTableViewControl());
#endif
			new InstanceConfigurationPresenter(item, control, itemNavigation);

			return control;
		}
	}
}
