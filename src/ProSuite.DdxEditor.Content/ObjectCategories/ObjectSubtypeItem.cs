using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Attributes;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectSubtypeItem : ObjectCategoryItem<ObjectSubtype>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtypeItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="objectSubtype">The objectSubtype.</param>
		/// <param name="repository">The repository.</param>
		public ObjectSubtypeItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                         [NotNull] ObjectSubtype objectSubtype,
		                         [NotNull] IRepository<ObjectCategory> repository)
			: base(modelBuilder, objectSubtype, repository)
		{
			_modelBuilder = modelBuilder;
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(Assert.NotNull(GetEntity()));
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<ObjectSubtype, IViewObserver> compositeControl)
		{
			base.AddEntityPanels(compositeControl);

			var control =
				new ObjectSubtypeControl<ObjectSubtype>();

			control.Observer =
				new ObjectSubtypePresenter(this, control, GetCriteriumToAdd);

			compositeControl.AddPanel(control);
		}

		private IList<ObjectAttributeTableRow> GetCriteriumToAdd(
			ObjectSubtype objectSubtype, IWin32Window owner,
			params ColumnDescriptor[] columns)
		{
			Assert.ArgumentNotNull(objectSubtype, nameof(objectSubtype));

			IFinder<ObjectAttributeTableRow> finder =
				new Finder<ObjectAttributeTableRow>();
			var items = new List<ObjectAttributeTableRow>();

			ObjectDataset dataset = objectSubtype.ObjectDataset;

			Assert.NotNull(dataset, "Dataset is null");

			_modelBuilder.ReadOnlyTransaction(
				delegate
				{
					_modelBuilder.Reattach(objectSubtype.ObjectDataset);

					foreach (ObjectAttribute attribute in dataset.GetAttributes())
					{
						items.Add(new ObjectAttributeTableRow(attribute)
						          {
							          Selectable = ! objectSubtype.ContainsCriterion(attribute)
						          });
					}
				});

			const bool allowMultiSelection = true;
			return finder.ShowDialog(owner, items, allowMultiSelection, columns);
		}

		protected override bool AllowDelete => true;

		protected override void IsValidForPersistenceCore(ObjectSubtype entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			if (entity.Name == null)
			{
				// already reported by standard validation
				return;
			}

			_modelBuilder.Reattach(entity.ObjectDataset);

			foreach (ObjectType existingObjectType in entity.ObjectDataset.ObjectTypes)
			{
				if (Equals(existingObjectType.Name, entity.Name))
				{
					notification.RegisterMessage("Name",
					                             "An object type with the same name already exists",
					                             Severity.Error);
				}

				foreach (ObjectSubtype existingSubtype in
					existingObjectType.ObjectSubtypes)
				{
					if (! Equals(existingSubtype.Name, entity.Name))
					{
						continue;
					}

					if (entity.Id == existingSubtype.Id)
					{
						// same instance, ok
					}
					else
					{
						notification.RegisterMessage("Name",
						                             "An object subtype with the same name already exists",
						                             Severity.Error);
					}
				}
			}
		}
	}
}
