using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.DdxEditor.Content.Models
{
	public delegate ConnectionProvider FindSdeDirectConnectionProvider(
		IWin32Window owner, params ColumnDescriptor[] columns);
}