using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class ObjectAttributeItem<T> : AttributeItem<T> where T : ObjectAttribute
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public ObjectAttributeItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                           [NotNull] T attribute,
		                           [NotNull] IRepository<Attribute> repository)
			: base(attribute, repository)
		{
			_modelBuilder = modelBuilder;
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			base.AddEntityPanels(compositeControl, itemNavigation);

			var control = new ObjectAttributeControl<T>();
			new ObjectAttributePresenter(control, FindObjectAttributeCategory);

			compositeControl.AddPanel(control);
		}

		private ObjectAttributeType FindObjectAttributeCategory(
			IWin32Window owner, params ColumnDescriptor[] columns)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IList<ObjectAttributeType> all = null;

			_modelBuilder.ReadOnlyTransaction(
				delegate { all = _modelBuilder.AttributeTypes.GetAll<ObjectAttributeType>(); });

			IFinder<ObjectAttributeType> finder = new Finder<ObjectAttributeType>();

			return finder.ShowDialog(owner, all, columns);
		}
	}
}
