using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	public interface ICreateQualityConditionsView : IWin32Window
	{
		[CanBeNull]
		ICreateQualityConditionsObserver Observer { get; set; }

		[NotNull]
		string TestDescriptorName { get; set; }

		/// <summary>
		/// Caption of the label left of <see cref="TestDescriptorName"/>. Defaults to
		/// "Test Descriptor:"; a presenter that drives the dialog from something else
		/// (an algorithm, say) sets its own caption here.
		/// </summary>
		[NotNull]
		string SubjectCaption { get; set; }

		/// <summary>
		/// Caption of the checkbox that excludes datasets already covered. Defaults to
		/// the classic "Exclude datasets for which this test descriptor is already used".
		/// </summary>
		[NotNull]
		string ExcludeDatasetsCaption { get; set; }

		/// <summary>
		/// A short note shown next to the checkbox above, saying how many datasets it
		/// left out of the dataset selection window — the ones that appear greyed out
		/// there. The empty string (the default) shows nothing; the classic presenter
		/// never sets it.
		/// </summary>
		[NotNull]
		string ExcludeDatasetsNote { get; set; }

		[NotNull]
		string QualityConditionNames { get; set; }

		[CanBeNull]
		IList<QualityConditionParameters> QualityConditionParameters { get; set; }

		DialogResult DialogResult { get; set; }

		bool OKEnabled { get; set; }

		void Close();

		string SupportedVariablesText { get; set; }

		bool SelectAllParametersRowsEnabled { get; set; }

		bool ClearParametersRowSelectionEnabled { get; set; }

		bool ApplyToParametersRowSelectionEnabled { get; set; }

		int TotalParametersRowCount { get; }

		int SelectedParametersRowCount { get; }

		bool RemoveSelectedParametersRowsEnabled { get; set; }

		[NotNull]
		IList<DataRow> SelectedParametersRows { get; }

		void BindParameters([NotNull] DataTable parametersDataTable);

		void AddParametersColumn([NotNull] DataGridViewColumn gridColumn);

		/// <summary>
		/// Removes all parameter columns and unbinds the grid, so a presenter that
		/// rebuilds its columns (for instance after a flavor change) can start over.
		/// The classic presenter adds its columns once and never calls this.
		/// </summary>
		void ClearParametersColumns();

		/// <summary>
		/// Places an additional control in the header area of the dialog, below the
		/// supported-variables box. Used for the flavor chooser; the classic presenter
		/// adds nothing and the header keeps its original layout.
		/// </summary>
		void AddOptionsControl([NotNull] Control control);

		void SelectAllParametersRows();

		void ClearParametersRowSelection();

		[NotNull]
		CellSelection GetParametersCellSelection();

		bool Confirm([NotNull] string message, bool defaultIsCancel);

		void Warn([NotNull] string message);

		void BindToQualitySpecifications(
			[NotNull] IList<QualitySpecificationTableRow> selectedQualitySpecifications);

		[NotNull]
		IList<QualitySpecificationTableRow> GetSelectedQualitySpecifications();

		bool RemoveFromQualitySpecificationsEnabled { get; set; }

		bool HasSelectedQualitySpecifications { get; }

		bool ExcludeDatasetsUsingThisTest { get; set; }

		[CanBeNull]
		DataQualityCategory TargetCategory { get; set; }
	}
}
