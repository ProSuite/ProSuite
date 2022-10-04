using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DdxEditor.Framework.Search
{
	/// <summary>
	/// 
	/// </summary>
	public class NopSearchProvider : ISearchProvider
	{
		public string Text => string.Empty;

		public Image Image => null;

		public Entity SearchEntity(IWin32Window owner)
		{
			return null;
		}
	}
}
