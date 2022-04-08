using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class DataQualityCategoryPresenter :
		SimpleEntityItemPresenter<DataQualityCategoryItem>, IDataQualityCategoryObserver
	{
		public delegate Model FindModel(
			IWin32Window owner, params ColumnDescriptor[] columns);

		public DataQualityCategoryPresenter([NotNull] IDataQualityCategoryView view,
		                                    [NotNull] DataQualityCategoryItem item,
		                                    [NotNull] FindModel findModel)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			view.Observer = this;
			view.FindDefaultModelDelegate = () => findModel(
				view,
				new ColumnDescriptor("Name"),
				new ColumnDescriptor("Description"),
				new ColumnDescriptor("UserConnectionProvider",
				                     "Master Database Connection Provider"),
				new ColumnDescriptor(
					"SpatialReferenceDescriptor",
					"Spatial Reference"));
		}
	}
}
