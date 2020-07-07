using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Finder
{
	public interface IFinderView<T> where T : class
	{
		IFinderObserver Observer { get; set; }

		DialogResult DialogResult { get; set; }

		bool OKEnabled { get; set; }

		bool HasSelection { get; }

		IList<T> Selection { get; set; }

		[CanBeNull]
		string StatusMessage { get; set; }

		int SelectionCount { get; }

		int TotalCount { get; }

		IEnumerable<T> GetSelection();
	}
}
