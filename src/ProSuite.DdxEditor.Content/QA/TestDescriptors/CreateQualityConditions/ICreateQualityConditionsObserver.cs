using System.Collections.Generic;
using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	public interface ICreateQualityConditionsObserver
	{
		void ViewLoaded();

		void OKClicked();

		void CancelClicked();

		void AddClicked();

		void RemoveClicked();

		void QualitySpecificationNamingChanged();

		void QualityConditionParametersSelectionChanged();

		void CollectContextCommands([NotNull] IList<ICommand> commands);

		void SelectAllClicked();

		void SelectNoneClicked();

		void ApplyToSelectionClicked();

		void CellValidated([NotNull] DataRow dataRow, [NotNull] string columnName);

		/// <summary>
		/// A button cell of the parameters grid was clicked (the "…" cells that open a
		/// builder or picker dialog). Such columns hold no value of their own, so they
		/// are identified by the grid column's Name rather than by a data column name.
		/// The classic presenter adds no button cells and ignores this.
		/// </summary>
		void CellButtonClicked([NotNull] DataRow dataRow, [NotNull] string columnName);

		void AssignToQualitySpecificationsClicked();

		void RemoveFromQualitySpecificationsClicked();

		void QualitySpecificationSelectionChanged();

		bool CanFindCategory { get; }

		object FindCategory();

		string FormatCategoryText(object category);
	}
}
