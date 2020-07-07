namespace ProSuite.Commons.UI.Finder
{
	internal interface ITypeFinderObserver
	{
		void AssemblyPathChanged();

		void TypeSelectionChanged();

		void OKClicked();

		void RowDoubleClicked();

		void ViewLoaded();
	}
}
