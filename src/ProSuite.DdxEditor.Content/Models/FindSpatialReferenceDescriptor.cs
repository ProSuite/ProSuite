using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public delegate SpatialReferenceDescriptor FindSpatialReferenceDescriptor(
		IWin32Window owner, params ColumnDescriptor[] columns);
}
