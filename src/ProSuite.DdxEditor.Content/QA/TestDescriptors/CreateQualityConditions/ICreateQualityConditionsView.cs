using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal interface ICreateQualityConditionsView : IWin32Window
	{
		[CanBeNull]
		ICreateQualityConditionsObserver Observer { get; set; }

		[NotNull]
		string TestDescriptorName { get; set; }

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
