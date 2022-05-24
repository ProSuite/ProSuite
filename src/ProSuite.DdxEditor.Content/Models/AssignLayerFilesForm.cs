using System;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Content.Models
{
	public partial class AssignLayerFilesForm : Form,
	                                            IFormStateAware<AssignLayerFileFormState>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AssignLayerFilesForm"/> class.
		/// </summary>
		public AssignLayerFilesForm()
		{
			InitializeComponent();

			DialogResult = DialogResult.Cancel;

			var formStateManager = new FormStateManager<AssignLayerFileFormState>(this);
			formStateManager.RestoreState();
			FormClosed += delegate { formStateManager.SaveState(); };

			UpdateOkEnabled();
		}

		#endregion

		public string FolderPath { get; private set; }

		private void UpdateOkEnabled()
		{
			_buttonOK.Enabled = IsValidFolder(_fileSystemPathControl.TextBox.Text);
		}

		private static bool IsValidFolder([CanBeNull] string folderPath)
		{
			return ! string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
		}

		#region Event handlers

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			string folderPath = _fileSystemPathControl.TextBox.Text;

			if (! IsValidFolder(folderPath))
			{
				Dialog.Warning(this, Text, "Invalid folder");
				return;
			}

			FolderPath = folderPath;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void _fileSystemPathControl_ValueChanged(object sender, EventArgs e)
		{
			UpdateOkEnabled();
		}

		#endregion

		#region Implementation of IFormStateAware<AssignLayerFileFormState>

		void IFormStateAware<AssignLayerFileFormState>.RestoreState(
			AssignLayerFileFormState formState)
		{
			if (IsValidFolder(formState.FolderPath))
			{
				_fileSystemPathControl.TextBox.Text = FolderPath;
			}
		}

		void IFormStateAware<AssignLayerFileFormState>.GetState(
			AssignLayerFileFormState formState)
		{
			string folderPath = _fileSystemPathControl.TextBox.Text;

			if (IsValidFolder(folderPath))
			{
				formState.FolderPath = folderPath;
			}
		}

		#endregion
	}
}