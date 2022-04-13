using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class ObjectAttributePresenter
	{
		public delegate ObjectAttributeType FindObjectAttributeType(
			IWin32Window owner, params ColumnDescriptor[] columns);

		public ObjectAttributePresenter() { }

		public ObjectAttributePresenter(IObjectAttributeView view,
		                                FindObjectAttributeType
			                                findObjectAttributeType)
		{
			view.FindObjectAttributeTypeDelegate =
				delegate
				{
					return findObjectAttributeType(view,
					                               new ColumnDescriptor("Name"));
				};
		}
	}
}
