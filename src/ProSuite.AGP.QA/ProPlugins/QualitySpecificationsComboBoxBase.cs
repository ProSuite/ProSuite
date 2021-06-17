using System;
using System.Linq;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class QualitySpecificationsComboBoxBase : ComboBox
	{
		protected QualitySpecificationsComboBoxBase()
		{
			FillCombo();
			//Enabled = false;

			// TODO: To ensure that this cannot be created before the QualityVerificationEnvironment exists
			// -> Create static session context with the QualitySpecificationsRefreshed event
			// -> Here, subscribe only to that event
			QualityVerificationEnvironment.QualitySpecificationsRefreshed +=
				QualitySpecificationsRefreshed;
		}

		protected abstract IQualityVerificationEnvironment QualityVerificationEnvironment { get; }

		private void QualitySpecificationsRefreshed(object sender, EventArgs e)
		{
			Clear();

			FillCombo();
		}

		private void FillCombo()
		{
			foreach (var qaSpec in QualityVerificationEnvironment.QualitySpecifications.Select(
				s => s.Name))
			{
				Add(new ComboBoxItem(qaSpec));
			}

			// Select first item
			SelectedItem = ItemCollection.FirstOrDefault();
		}

		protected override void OnSelectionChange(ComboBoxItem item)
		{
			// TODO: Binding
			QualityVerificationEnvironment.CurrentQualitySpecification =
				QualityVerificationEnvironment.QualitySpecifications.FirstOrDefault(
					s => s.Name == item.Text);
		}
	}
}
