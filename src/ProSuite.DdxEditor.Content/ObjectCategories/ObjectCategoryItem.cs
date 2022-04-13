using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectCategoryItem<E> : SubclassedEntityItem<E, ObjectCategory>,
	                                     IObjectCategoryItem
		where E : ObjectCategory
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryItem{E}"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="category">The category.</param>
		/// <param name="repository">The repository.</param>
		public ObjectCategoryItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                          [NotNull] E category,
		                          [NotNull] IRepository<ObjectCategory> repository)
			: base(category, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			ObjectDataset = category.ObjectDataset;
			Model = ObjectDataset.Model;
			_modelBuilder = modelBuilder;
		}

		#region IObjectCategoryItem Members

		public DdxModel Model { get; }

		public ObjectDataset ObjectDataset { get; }

		#endregion

		protected override void AttachPresenter(
			ICompositeEntityControl<E, IViewObserver> control)
		{
			// if needed, override and use specific subclass
			new ObjectCategoryPresenter<E>(this, control);
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<E, IViewObserver> compositeControl)
		{
			var control =
				new ObjectCategoryControl<E>();

			new CategoryPresenter<E>(this, control);

			compositeControl.AddPanel(control);
		}
	}
}
