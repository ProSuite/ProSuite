using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public interface IAssociationEndView : IWrappedEntityControl<AssociationEnd>, IWin32Window
	{
		[CanBeNull]
		IAssociationEndObserver Observer { get; set; }
	}
}
