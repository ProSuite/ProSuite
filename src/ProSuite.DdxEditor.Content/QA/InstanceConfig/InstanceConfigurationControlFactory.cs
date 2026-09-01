using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.AlgorithmUI;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.UI.Core.QA.Controls;
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
			// Every control creation starts from a clean item; the algorithm branch
			// below re-installs whatever callbacks it needs. Without this reset, stale
			// callbacks from a previous editor (e.g. algorithm mode) survive a switch
			// to the classic editor and corrupt persistence/validation.
			item.PreparePersistenceCallback = null;
			item.PrepareValidationCallback = null;
			item.WebHelpDescriptorProvider = null;

			if (modelBuilder.UseAlgorithmUi)
			{
				return modelBuilder.AlgorithmEditorUi.CreateQualityConditionControl(
					item, itemNavigation, modelBuilder, tableStateQSpec, tableStateIssueFilter);
			}

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
			// See the QualityConditionItem overload above: reset before branching so
			// callbacks from a previous editor never leak into the new control.
			item.PreparePersistenceCallback = null;
			item.PrepareValidationCallback = null;
			item.WebHelpDescriptorProvider = null;

			if (modelBuilder.UseAlgorithmUi)
			{
				return modelBuilder.AlgorithmEditorUi.CreateInstanceConfigurationControl(
					item, itemNavigation, modelBuilder, tableState);
			}

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
