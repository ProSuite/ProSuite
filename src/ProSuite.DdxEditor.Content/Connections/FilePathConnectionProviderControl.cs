using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	// renamed because resource file path on build server was too long
	public partial class FilePathConnectionProviderControl<T> : UserControl,
	                                                            IEntityPanel<T>
		where T : FilePathConnectionProviderBase
	{
		public FilePathConnectionProviderControl(FilePathConnectionProviderBase entity)
		{
			InitializeComponent();

			Title = string.Format("{0} Connection Provider Properties",
			                      entity.TypeDescription);

			_fileSystemPathControlPath.FileDefaultExtension = entity.FileDefaultExtension;
			_fileSystemPathControlPath.FileFilter = entity.FileFilter;

			if (entity.FilePathIsFolder)
			{
				_fileSystemPathControlPath.ControlPathType = FileSystemPathType.ChooseFolder;
			}
		}

		#region IEntityPanel<T> Members

		public string Title { get; private set; }

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.Path)
			      .To(_fileSystemPathControlPath.TextBox)
			      .WithLabel(_labelPath);
		}

		public void OnBoundTo(T entity) { }

		#endregion
	}
}
