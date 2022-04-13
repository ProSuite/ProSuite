using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public interface IAssociationEndView :
		IWrappedEntityControl<AssociationEnd>, IWin32Window
	{
		[CanBeNull]
		IAssociationEndObserver Observer { get; set; }
	}
}
