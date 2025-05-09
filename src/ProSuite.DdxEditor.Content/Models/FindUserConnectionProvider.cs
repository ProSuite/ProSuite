using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Models
{
	public delegate ConnectionProvider FindUserConnectionProvider(
		IWin32Window owner, params ColumnDescriptor[] columns);
}
