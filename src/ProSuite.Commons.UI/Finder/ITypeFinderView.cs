using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Lists;

namespace ProSuite.Commons.UI.Finder
{
	internal interface ITypeFinderView
	{
		ITypeFinderObserver Observer { get; set; }

		IList<TypeTableRow> GetSelectedTypeRows();

		string AssemblyPath { get; set; }

		int SelectedTypeCount { get; }

		bool OKEnabled { get; set; }

		DialogResult DialogResult { get; set; }

		[CanBeNull]
		IList<Type> SelectedTypes { get; set; }

		bool SelectAllTypesEnabled { get; set; }

		bool SelectNoTypesEnabled { get; set; }

		int TypeCount { get; }

		string StatusText { get; set; }

		string Text { get; set; }

		string LastUsedAssemblyPath { get; }

		void Close();

		void SetAssemblyError(string format, params string[] args);

		void ClearAssemblyError();

		void ClearTypeRows();

		void SetTypeRows([NotNull] SortableBindingList<TypeTableRow> rows);

		bool TrySelectRow([NotNull] TypeTableRow tableRow);
	}
}
