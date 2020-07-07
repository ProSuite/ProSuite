namespace ProSuite.Commons.UI.Finder
{
	public interface IFinderObserver
	{
		void CancelClicked();

		void OKClicked();

		void SelectionChanged();

		void ListDoubleClicked();

		void ViewLoaded();
	}
}
