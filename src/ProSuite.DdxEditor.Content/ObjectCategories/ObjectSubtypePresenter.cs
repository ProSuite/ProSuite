using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Attributes;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectSubtypePresenter : IObjectSubtypeObserver
	{
		#region Delegates

		public delegate IList<ObjectAttributeTableRow> GetCriteriumToAdd(
			ObjectSubtype objectSubtype, IWin32Window owner,
			params ColumnDescriptor[] columns);

		#endregion

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly GetCriteriumToAdd _getCriteriumToAdd;
		private readonly IObjectSubtypeView _view;
		private readonly ObjectSubtypeItem _item;

		public ObjectSubtypePresenter([NotNull] ObjectSubtypeItem item,
		                              [NotNull] IObjectSubtypeView view,
		                              [NotNull] GetCriteriumToAdd getCriteriumToAdd)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(getCriteriumToAdd, nameof(getCriteriumToAdd));

			_item = item;
			_view = view;
			_getCriteriumToAdd = getCriteriumToAdd;

			UpdateAppearance();
		}

		public IList<ObjectSubtypeCriterionTableRow> AddTargetClicked()
		{
			ObjectSubtype subType = _item.GetEntity();
			Assert.NotNull(subType, "subType");

			IList<ObjectAttributeTableRow> selectedItems =
				_getCriteriumToAdd(subType, _view,
				                   new ColumnDescriptor("Name", "Name"));

			var addedItems = new List<ObjectSubtypeCriterionTableRow>();

			if (selectedItems == null)
			{
				return addedItems;
			}

			var anyAdded = false;

			foreach (ObjectAttributeTableRow selectedItem in selectedItems)
			{
				ObjectAttribute selectedAttribute = selectedItem.ObjectAttribute;

				if (subType.ContainsCriterion(selectedAttribute))
				{
					_msg.WarnFormat("A criterion for the attribute already exists");
				}
				else
				{
					ObjectSubtypeCriterion criterion =
						subType.AddCriterion(selectedAttribute.Name, null);

					addedItems.Add(new ObjectSubtypeCriterionTableRow(criterion));

					anyAdded = true;
				}
			}

			if (anyAdded)
			{
				_item.NotifyChanged();
			}

			return addedItems;
		}

		public IList<ObjectAttribute> RemoveTargetClicked()
		{
			// get selected targets
			IList<ObjectSubtypeCriterionTableRow> selected = _view.GetSelectedCriteria();

			ObjectSubtype entity = _item.GetEntity();
			Assert.NotNull(entity, "entity is null");

			var removedAttributes = new List<ObjectAttribute>();

			// remove them from the entity
			foreach (ObjectSubtypeCriterionTableRow item in selected)
			{
				removedAttributes.Add(item.Attribute);

				entity.RemoveCriterion(item.Attribute);
			}

			_item.NotifyChanged();

			return removedAttributes;
		}

		public void TargetSelectionChanged()
		{
			UpdateAppearance();
		}

		private void UpdateAppearance()
		{
			bool hasCriterionSelection = _view.HasSelectedCriteria;
			_view.RemoveCriteriaEnabled = hasCriterionSelection;
		}

		#region IViewObserver Members

		public void NotifyChanged(bool dirty)
		{
			if (dirty)
			{
				_item.NotifyChanged();
			}
		}

		#endregion
	}
}
