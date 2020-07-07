using System;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class FileSystemPathControl : UserControl
	{
		private enum FileDialogType
		{
			Open,
			Save
		}

		#region Constructors

		public FileSystemPathControl()
		{
			InitializeComponent();
		}

		#endregion

		public event EventHandler ValueChanged;
		public event EventHandler LeaveTextBox;

		[Browsable(false)]
		public TextBox TextBox { get; private set; }

		public FileSystemPathType ControlPathType { get; set; }

		[UsedImplicitly]
		public bool FileCheckPathExists { get; set; } = true;

		[UsedImplicitly]
		public bool FileCheckFileExists { get; set; } = true;

		[UsedImplicitly]
		public string FileDefaultExtension { get; set; }

		[UsedImplicitly]
		public string FileFilter { get; set; }

		[UsedImplicitly]
		public bool FolderShowNewFolderButton { get; set; } = true;

		[UsedImplicitly]
		public string FolderGroupTitle { get; set; } = "Choose Folder";

		#region Private functions

		private string GetSelectedFolderName([NotNull] IWin32Window owner)
		{
			// TODO set initial folder 
			// - from current text box value
			// - if that is undefined: value of new property?

			using (var dlg = new FolderBrowserDialog())
			{
				dlg.Description = FolderGroupTitle;
				dlg.ShowNewFolderButton = FolderShowNewFolderButton;

				DialogResult result = dlg.ShowDialog(owner);

				return result == DialogResult.OK
					       ? dlg.SelectedPath
					       : string.Empty;
			}
		}

		private string GetSelectedFileName([NotNull] IWin32Window owner,
		                                   FileDialogType dialogType)
		{
			// TODO set initial folder 
			// - from current text box value
			// - if that is undefined: value of new property?

			using (FileDialog dlg = CreateFileDialog(dialogType))
			{
				dlg.AddExtension = true;
				dlg.DereferenceLinks = true;
				dlg.RestoreDirectory = true;
				dlg.SupportMultiDottedExtensions = true;
				dlg.ValidateNames = true;

				dlg.CheckPathExists = FileCheckPathExists;
				dlg.CheckFileExists = FileCheckFileExists;

				if (! string.IsNullOrEmpty(FileDefaultExtension))
				{
					dlg.DefaultExt = FileDefaultExtension;
				}

				if (! string.IsNullOrEmpty(FileFilter))
				{
					dlg.Filter = FileFilter;
				}

				dlg.FilterIndex = 0;

				DialogResult result = dlg.ShowDialog(owner);

				return result == DialogResult.OK
					       ? dlg.FileName
					       : string.Empty;
			}
		}

		[NotNull]
		private static FileDialog CreateFileDialog(FileDialogType dialogType)
		{
			switch (dialogType)
			{
				case FileDialogType.Open:
					return new OpenFileDialog();

				case FileDialogType.Save:
					return new SaveFileDialog();

				default:
					throw new ArgumentOutOfRangeException(nameof(dialogType), dialogType,
					                                      @"Unsupported dialog type");
			}
		}

		private void OnValueChanged(EventArgs e)
		{
			EventHandler handler = ValueChanged;
			handler?.Invoke(this, e);
		}

		protected virtual void OnLeaveTextBox(EventArgs e)
		{
			EventHandler handler = LeaveTextBox;
			handler?.Invoke(this, e);
		}

		#region EventHandlers

		private void _buttonBrowse_Click(object sender, EventArgs e)
		{
			string selected = SelectPath(this);

			if (! string.IsNullOrEmpty(selected))
			{
				TextBox.Text = selected;
			}
		}

		[CanBeNull]
		private string SelectPath([NotNull] IWin32Window owner)
		{
			switch (ControlPathType)
			{
				case FileSystemPathType.ChooseFileName:
					return GetSelectedFileName(owner, FileDialogType.Open);

				case FileSystemPathType.ChooseFolder:
					return GetSelectedFolderName(owner);

				case FileSystemPathType.SaveFileName:
					return GetSelectedFileName(owner, FileDialogType.Save);

				default:
					throw new InvalidOperationException(
						string.Format("Invalid pathType {0}", ControlPathType));
			}
		}

		private void _textBox_TextChanged(object sender, EventArgs e)
		{
			OnValueChanged(e);
		}

		private void _textBox_Leave(object sender, EventArgs e)
		{
			OnLeaveTextBox(e);
		}

		#endregion EventHandlers

		#endregion Private functions
	}
}
