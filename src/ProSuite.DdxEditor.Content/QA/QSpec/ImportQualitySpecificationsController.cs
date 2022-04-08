using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ImportQualitySpecificationsController :
		IImportQualitySpecificationsObserver
	{
		private readonly IImportQualitySpecificationsView _view;

		/// <summary>
		/// Initializes a new instance of the <see cref="ImportQualitySpecificationsController"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		public ImportQualitySpecificationsController(
			[NotNull] IImportQualitySpecificationsView view)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			_view = view;
			_view.Observer = this;

			UpdateAppearance();
		}

		#region Implementation of IImportQualitySpecificationsObserver

		public void FilePathChanged()
		{
			UpdateAppearance();
		}

		public void OKClicked()
		{
			string filePath = _view.CurrentFilePath;
			Assert.NotNull(filePath, "filePath");

			_view.SetOKResult(filePath);

			_view.Close();
		}

		public void CancelClicked()
		{
			_view.SetCancelResult();

			_view.Close();
		}

		#endregion

		private void UpdateAppearance()
		{
			var ok = false;

			_view.StatusText = null;

			if (StringUtils.IsNotEmpty(_view.CurrentFilePath))
			{
				if (File.Exists(_view.CurrentFilePath))
				{
					ok = true;
				}
				else
				{
					_view.StatusText = string.Format("File does not exist: {0}",
					                                 _view.CurrentFilePath);
				}
			}

			_view.OKEnabled = ok;
		}
	}
}