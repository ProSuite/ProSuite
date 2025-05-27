using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class DataQualityCategoryPresenter :
		SimpleEntityItemPresenter<DataQualityCategoryItem>, IDataQualityCategoryObserver
	{
		public delegate DdxModel FindModelDelegate(
			IWin32Window owner, params ColumnDescriptor[] columns);

		public DataQualityCategoryPresenter([NotNull] DataQualityCategoryItem item,
		                                    [NotNull] IDataQualityCategoryView view,
		                                    [NotNull] FindModelDelegate findModelDelegate)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(findModelDelegate, nameof(findModelDelegate));

			view.Observer = this;
			view.FindDefaultModelDelegate = () => findModelDelegate(
				view,
				new ColumnDescriptor(nameof(DdxModel.Name)),
				new ColumnDescriptor(nameof(DdxModel.Description)),
				new ColumnDescriptor(nameof(DdxModel.UserConnectionProvider),
				                     "Master Database Connection Provider"),
				new ColumnDescriptor(nameof(DdxModel.SpatialReferenceDescriptor),
				                     "Spatial Reference"));
		}
	}
}
