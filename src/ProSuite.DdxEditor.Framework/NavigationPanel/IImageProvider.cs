using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public interface IImageProvider
	{
		[CanBeNull]
		string GetImageKey([NotNull] Item item,
		                   [CanBeNull] Image image);

		[CanBeNull]
		string GetImageKey([NotNull] Item item,
		                   [CanBeNull] Image image,
		                   bool selected);
	}
}
