using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	internal interface IExportQualitySpecificationsView : IWin32Window
	{
		void Close();

		[CanBeNull]
		string CurrentFilePath { get; set; }

		[CanBeNull]
		string CurrentDirectoryPath { get; set; }

		void BindTo([NotNull] IEnumerable<QualitySpecificationListItem> items);

		void Select([NotNull] IEnumerable<QualitySpecificationListItem> items);

		[NotNull]
		IList<QualitySpecificationListItem> SelectedItems { get; }

		IExportQualitySpecificationsObserver Observer { set; }

		bool OKEnabled { get; set; }

		bool SelectNoneEnabled { get; set; }

		bool SelectAllEnabled { get; set; }

		bool ExportSdeConnectionFilePathsEnabled { get; set; }

		int ItemCount { get; }

		void SetOKResult(
			[NotNull] IDictionary<string, ICollection<QualitySpecification>>
				qualitySpecificationsByFileName,
			[NotNull] ICollection<string> deletableFiles);

		void SetCancelResult();

		void SelectAll();

		void SelectNone();

		[CanBeNull]
		string StatusText { get; set; }

		ExportTarget CurrentExportTarget { get; set; }

		bool FilePathEnabled { get; set; }

		bool DirectoryPathEnabled { get; set; }

		bool ExportWorkspaceConnections { get; }

		void SetCurrentFilePathError([CanBeNull] string message);

		void SetCurrentDirectoryPathError([CanBeNull] string message);

		bool Confirm(string message);
	}
}
