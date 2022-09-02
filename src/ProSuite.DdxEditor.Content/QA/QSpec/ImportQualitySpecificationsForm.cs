using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public partial class ImportQualitySpecificationsForm :
		Form,
		IImportQualitySpecificationsView,
		IFormStateAware<ImportQualitySpecificationsFormState>
	{
		private IImportQualitySpecificationsObserver _observer;

		public ImportQualitySpecificationsForm([NotNull] string fileFilter,
		                                       [NotNull] string defaultExtension)
		{
			Assert.ArgumentNotNullOrEmpty(fileFilter, nameof(fileFilter));
			Assert.ArgumentNotNullOrEmpty(defaultExtension, nameof(defaultExtension));

			InitializeComponent();

			_toolStripStatusLabel.Text = null;
			_checkBoxUpdateDescriptorNames.Checked = false;
			_checkBoxUpdateDescriptorProperties.Checked = false;
			_checkBoxIgnoreQualityConditionsForUnknownDatasets.Checked = true;

			var formStateManager = new FormStateManager<ImportQualitySpecificationsFormState>(this);
			formStateManager.RestoreState();
			FormClosed += delegate { formStateManager.SaveState(); };

			_fileSystemPathControl.FileFilter = fileFilter;
			_fileSystemPathControl.FileDefaultExtension = defaultExtension;
			_fileSystemPathControl.FileCheckFileExists = true;
			_fileSystemPathControl.FileCheckPathExists = true;
			_fileSystemPathControl.ControlPathType = FileSystemPathType.ChooseFileName;
		}

		public string FilePath { get; private set; }

		public bool UpdateDescriptorNames => _checkBoxUpdateDescriptorNames.Checked;

		public bool UpdateDescriptorProperties => _checkBoxUpdateDescriptorProperties.Checked;

		public bool IgnoreQualityConditionsForUnknownDatasets =>
			_checkBoxIgnoreQualityConditionsForUnknownDatasets.Checked;

		#region Implementation of IImportQualitySpecificationsView

		string IImportQualitySpecificationsView.CurrentFilePath =>
			_fileSystemPathControl.TextBox.Text;

		public IImportQualitySpecificationsObserver Observer
		{
			set { _observer = value; }
		}

		public bool OKEnabled
		{
			get { return _buttonOK.Enabled; }
			set { _buttonOK.Enabled = value; }
		}

		void IImportQualitySpecificationsView.SetOKResult(string filePath)
		{
			DialogResult = DialogResult.OK;

			FilePath = filePath;
		}

		void IImportQualitySpecificationsView.SetCancelResult()
		{
			DialogResult = DialogResult.Cancel;

			FilePath = null;
		}

		string IImportQualitySpecificationsView.StatusText
		{
			get { return _toolStripStatusLabel.Text; }
			set { _toolStripStatusLabel.Text = value; }
		}

		#endregion

		#region Event handlers

		private void _fileSystemPathControl_ValueChanged(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.FilePathChanged();
			}
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.OKClicked();
			}
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.CancelClicked();
			}
		}

		#endregion

		#region Implementation of IFormStateAware<ImportQualitySpecificationsFormState>

		void IFormStateAware<ImportQualitySpecificationsFormState>.RestoreState(
			ImportQualitySpecificationsFormState formState)
		{
			_checkBoxUpdateDescriptorNames.Checked = formState.UpdateTestDescriptorNames;
			_checkBoxUpdateDescriptorProperties.Checked =
				formState.UpdateTestDescriptorProperties;
			_checkBoxIgnoreQualityConditionsForUnknownDatasets.Checked =
				formState.IgnoreQualityConditionsForUnknownDatasets;
		}

		void IFormStateAware<ImportQualitySpecificationsFormState>.GetState(
			ImportQualitySpecificationsFormState formState)
		{
			formState.UpdateTestDescriptorNames = _checkBoxUpdateDescriptorNames.Checked;
			formState.UpdateTestDescriptorProperties =
				_checkBoxUpdateDescriptorProperties.Checked;
			formState.IgnoreQualityConditionsForUnknownDatasets =
				_checkBoxIgnoreQualityConditionsForUnknownDatasets.Checked;
		}

		#endregion
	}
}
