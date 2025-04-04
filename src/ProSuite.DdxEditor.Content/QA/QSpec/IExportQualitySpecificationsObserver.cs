﻿namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IExportQualitySpecificationsObserver
	{
		void ExportTargetChanged();

		void FilePathChanged();

		void FilePathFocusLost();

		void DirectoryPathFocusLost();

		void DirectoryPathChanged();

		void OKClicked();

		void CancelClicked();

		void SelectAllClicked();

		void SelectNoneClicked();

		void SelectedItemsChanged();

		void ExportWorkspaceConnectionsChanged();
	}
}
