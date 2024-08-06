using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.UI.QA.Controls;
using QualityConditionControl = ProSuite.DdxEditor.Content.QA.QCon.QualityConditionControl;
#if NET6_0_OR_GREATER
using ProSuite.DdxEditor.Content.Blazor;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;
#endif

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
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

#if NET6_0_OR_GREATER
			IInstanceConfigurationTableViewControl blazorControl =
				CreateBlazorControl(item, itemNavigation, modelBuilder);

			bool ignoreLastTab = item.IsNew;

			control = new QualityConditionControl(tableStateQSpec, tableStateIssueFilter,
			                                      blazorControl, ignoreLastTab);

			if (item.HideIssueFilters)
			{
				control.HideIssueFilterTab();
			}
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

#if NET6_0_OR_GREATER
			IInstanceConfigurationTableViewControl blazorControl =
				CreateBlazorControl(item, itemNavigation, modelBuilder);

			bool ignoreLastTab = item.IsNew;

			control = new InstanceConfigurationControl(tableState, blazorControl, ignoreLastTab);
#else
			control =
				new InstanceConfigurationControl(tableState,
				                                 new QualityConditionTableViewControl());
#endif
			new InstanceConfigurationPresenter(item, control, itemNavigation);

			return control;
		}

#if NET6_0_OR_GREATER
		private static IInstanceConfigurationTableViewControl CreateBlazorControl<T>(
			EntityItem<T, T> item, IItemNavigation itemNavigation,
			CoreDomainModelItemModelBuilder modelBuilder) where T : InstanceConfiguration
		{
			var viewModel =
				new InstanceConfigurationViewModel<T>(
					item, modelBuilder.GetTestParameterDatasetProvider(), itemNavigation);

			viewModel.SqlExpressionBuilder = modelBuilder.GetSqlExpressionBuilder();

			IInstanceConfigurationTableViewControl blazorControl =
				new QualityConditionBlazor(viewModel);

			return blazorControl;
		}
#endif
	}
}
