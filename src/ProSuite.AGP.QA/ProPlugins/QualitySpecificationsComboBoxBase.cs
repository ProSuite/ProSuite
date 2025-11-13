using System;
using System.Linq;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class QualitySpecificationsComboBoxBase : ComboBox
	{
		protected QualitySpecificationsComboBoxBase()
		{
			FillCombo();

			WireEvent();
		}

		protected abstract IVerificationSessionContext SessionContext { get; }

		protected void QualitySpecificationsRefreshed(object sender, EventArgs e)
		{
			Clear();

			FillCombo();
		}

		private void WireEvent()
		{
			SessionContext.QualitySpecificationsRefreshed += QualitySpecificationsRefreshed;
		}

		private void FillCombo()
		{
			IQualityVerificationEnvironment verificationEnvironment =
				SessionContext.VerificationEnvironment;

			if (verificationEnvironment == null)
			{
				Clear();
				return;
			}

			foreach (var qaSpec in verificationEnvironment.QualitySpecificationReferences.Select(
				         s => s.Name))
			{
				Add(new ComboBoxItem(qaSpec));
			}

			IQualitySpecificationReference currentSpecification =
				verificationEnvironment.CurrentQualitySpecificationReference;

			if (currentSpecification != null)
			{
				SelectedItem =
					ItemCollection.FirstOrDefault(
						i => Equals(((ComboBoxItem) i).Text, currentSpecification.Name));
			}
			else
			{
				// Select first item
				SelectedItem = ItemCollection.FirstOrDefault();
			}
		}

		protected override void OnSelectionChange(ComboBoxItem item)
		{
			// TODO: Binding
			IQualityVerificationEnvironment verificationEnvironment =
				SessionContext.VerificationEnvironment;

			if (verificationEnvironment == null)
			{
				Clear();
				return;
			}

			verificationEnvironment.CurrentQualitySpecificationReference =
				verificationEnvironment.QualitySpecificationReferences.FirstOrDefault(
					s => s.Name == item.Text);
		}
	}
}
