using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IImportQualitySpecificationsView
	{
		void Close();

		[CanBeNull]
		string CurrentFilePath { get; }

		IImportQualitySpecificationsObserver Observer { set; }

		bool OKEnabled { get; set; }

		void SetOKResult([NotNull] string filePath);

		void SetCancelResult();

		[CanBeNull]
		string StatusText { get; set; }
	}
}