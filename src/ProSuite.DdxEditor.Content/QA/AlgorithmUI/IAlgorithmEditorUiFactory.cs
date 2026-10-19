using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.AlgorithmUI
{
	/// <summary>
	/// Seam implemented by the algorithm-first WinForms UI project
	/// (<c>ProSuite.Shared.DdxEditor.Content</c>) and registered on
	/// <see cref="CoreDomainModelItemModelBuilder.AlgorithmEditorUi"/>.
	/// </summary>
	/// <remarks>
	/// When <see cref="CoreDomainModelItemModelBuilder.UseAlgorithmUi"/> is <c>false</c>
	/// (no factory registered, or "Use classic condition specification" is switched on),
	/// none of these methods are called and the classic Finder/constructor-combo/Blazor
	/// flow is used, unchanged.
	/// </remarks>
	public interface IAlgorithmEditorUiFactory
	{
		/// <summary>
		/// Creates the algorithm-first content control for a quality condition, replacing
		/// the classic <see cref="QualityConditionControl"/> (Finder + constructor combo +
		/// Blazor parameter grid) built by
		/// <see cref="InstanceConfigurationControlFactory.CreateControl(QualityConditionItem,IItemNavigation,CoreDomainModelItemModelBuilder,TableState,TableState)"/>.
		/// </summary>
		/// <param name="itemNavigation">The item navigation service (e.g. to route
		/// parameter documentation to the help pane).</param>
		/// <param name="tableStateQSpec">Persisted table state for the quality
		/// specifications reference grid.</param>
		/// <param name="tableStateIssueFilter">Persisted table state for the issue
		/// filters reference grid.</param>
		[NotNull]
		Control CreateQualityConditionControl(
			[NotNull] QualityConditionItem item,
			[NotNull] IItemNavigation itemNavigation,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] TableState tableStateQSpec,
			[NotNull] TableState tableStateIssueFilter);

		/// <summary>
		/// Creates the algorithm-first content control for a transformer or issue filter
		/// configuration, replacing the classic <c>InstanceConfigurationControl</c> built
		/// by
		/// <see cref="InstanceConfigurationControlFactory.CreateControl(InstanceConfigurationItem,IItemNavigation,CoreDomainModelItemModelBuilder,TableState)"/>.
		/// </summary>
		[NotNull]
		Control CreateInstanceConfigurationControl(
			[NotNull] InstanceConfigurationItem item,
			[NotNull] IItemNavigation itemNavigation,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] TableState tableState);

		/// <summary>
		/// Creates the algorithm tree items shown under "Algorithm Descriptors" for
		/// <paramref name="configurationType"/>, grouped by test category. Replaces the
		/// classic, flat <c>TestDescriptorsItem</c>/<c>TransformerDescriptorsItem</c>/
		/// <c>IssueFilterDescriptorsItem</c> group item for that configuration type.
		/// </summary>
		/// <param name="modelBuilder">The model builder, providing the instance descriptor
		/// repository the algorithm catalog is built on.</param>
		/// <param name="configurationType">The <see cref="InstanceConfiguration"/> subtype
		/// the items are created for: <see cref="QualityCondition"/>,
		/// <see cref="TransformerConfiguration"/> or <see cref="IssueFilterConfiguration"/>.</param>
		[NotNull]
		IEnumerable<Item> CreateAlgorithmItems(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] Type configurationType);
	}
}
