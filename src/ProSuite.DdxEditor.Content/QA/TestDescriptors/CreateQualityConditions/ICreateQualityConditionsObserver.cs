using System.Collections.Generic;
using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal interface ICreateQualityConditionsObserver
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

		void AssignToQualitySpecificationsClicked();

		void RemoveFromQualitySpecificationsClicked();

		void QualitySpecificationSelectionChanged();

		bool CanFindCategory { get; }

		object FindCategory();

		string FormatCategoryText(object category);
	}
}
