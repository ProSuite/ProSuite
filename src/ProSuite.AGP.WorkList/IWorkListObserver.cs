using ArcGIS.Desktop.Framework.Controls;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListObserver
	{
		void Show([CanBeNull] string title = null);

		void Close();

		[CanBeNull]
		ProWindow View { get; }
	}
}
