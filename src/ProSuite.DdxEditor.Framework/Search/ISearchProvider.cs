using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Search
{
	public interface ISearchProvider
	{
		[NotNull]
		string Text { get; }

		[CanBeNull]
		Image Image { get; }

		[CanBeNull]
		Entity SearchEntity([NotNull] IWin32Window owner);
	}
}
