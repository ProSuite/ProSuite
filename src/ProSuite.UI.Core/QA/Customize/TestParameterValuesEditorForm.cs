using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	public partial class TestParameterValuesEditorForm : Form
	{
		/// <summary>
		/// Optional creator instance. In AO contexts, this should be set to an
		/// instance of ProSuite.UI.QA.Controls.QualityConditionTestConfigurationCreator.
		/// </summary>
		public ITestConfigurationCreator TestConfigurationCreator;

		public TestParameterValuesEditorForm()
		{
			InitializeComponent();
		}

		public void SetQualityCondition(
			[NotNull] QualityCondition qualityCondition,
			[CanBeNull] ITestParameterDatasetProvider testParameterDatasetProvider)
		{
			_qualityConditionControl.QualityCondition = qualityCondition;
			_qualityConditionControl.ReadOnly = true;

			_testDescriptorControl.TestDescriptor = qualityCondition.TestDescriptor;

			_qualityConditionParams.TestParameterDatasetProvider =
				testParameterDatasetProvider;
			_qualityConditionParams.QualityCondition = qualityCondition;

			_qualityConditionParams.AutoSyncQualityCondition = true;

			var formStateManager = new BasicFormStateManager(this);
			FormClosed += delegate { formStateManager.SaveState(); };

			formStateManager.RestoreState(FormStateRestoreOption.KeepLocation);
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			_qualityConditionParams.SyncQualityCondition();
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
