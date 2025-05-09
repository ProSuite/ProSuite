using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Models
{
	public delegate ConnectionProvider
		FindSdeRepositoryOwnerConnectionProvider(
			IWin32Window owner, params ColumnDescriptor[] columns);
}
