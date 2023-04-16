using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public abstract class ExchangeCommand<T> : ItemCommandBase<T> where T : Item
	{
		[NotNull] private readonly string _defaultExtension;
		[NotNull] private readonly string _fileFilter;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeCommand&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="defaultExtension">The default extension for exchange files.</param>
		/// <param name="fileFilter">The file filter for the exchange files.</param>
		protected ExchangeCommand([NotNull] T item,
		                          [NotNull] IApplicationController applicationController,
		                          [NotNull] string defaultExtension,
		                          [NotNull] string fileFilter)
			: base(item, applicationController)
		{
			_defaultExtension = defaultExtension;
			_fileFilter = fileFilter;
		}

		protected override bool EnabledCore =>
			base.EnabledCore && ! ApplicationController.HasPendingChanges;

		[CanBeNull]
		protected string GetSelectedFileName([NotNull] FileDialog dlg,
		                                     [CanBeNull] string initialFileName = null)
		{
			dlg.CheckPathExists = true;
			dlg.AddExtension = true;
			dlg.DereferenceLinks = true;
			dlg.RestoreDirectory = true;
			dlg.SupportMultiDottedExtensions = true;
			dlg.ValidateNames = true;
			dlg.DefaultExt = _defaultExtension;
			dlg.Filter = _fileFilter;
			dlg.FilterIndex = 0;

			if (initialFileName != null)
			{
				//dlg.ShowHelp = true; // workaround for incomplete display of file name - however the dialog uses less usable layout
				dlg.InitialDirectory = Path.GetDirectoryName(initialFileName);
				dlg.FileName = Path.GetFileName(initialFileName);
			}

			// TODO restore directory?

			DialogResult result = dlg.ShowDialog(ApplicationController.Window);

			if (result != DialogResult.OK)
			{
				return string.Empty;
			}

			string fileName = dlg.FileName;

			// TODO remember directory?

			// caller can validate file as needed
			return fileName;
		}
	}
}
