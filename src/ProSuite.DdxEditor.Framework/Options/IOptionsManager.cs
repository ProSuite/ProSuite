using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Options
{
	public interface IOptionsManager
	{
		void RestoreOptions();

		void SaveOptions();

		void ShowOptionsDialog([NotNull] IApplicationController applicationController,
		                       [NotNull] IWin32Window owner);
	}
}
