using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationPresenter :
		EntityItemPresenter
		<QualitySpecification, IQualitySpecificationObserver, QualitySpecification>,
		IQualitySpecificationObserver
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull] private readonly SortableBindingList<QualitySpecificationElementTableRow>
			_elementTableRows =
				new SortableBindingList<QualitySpecificationElementTableRow>();

		[NotNull] private readonly QualitySpecificationItem _item;
		[NotNull] private readonly IItemNavigation _itemNavigation;

		[NotNull] private readonly Func<ICollection<QualityCondition>, bool>
			_assignToCategory;

		[NotNull] private readonly IQualitySpecificationView _view;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationPresenter" /> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		/// <param name="itemNavigation">The item navigation.</param>
		/// <param name="assignToCategory">The function to assign selected quality conditions to a category.</param>
		public QualitySpecificationPresenter(
			[NotNull] QualitySpecificationItem item,
			[NotNull] IQualitySpecificationView view,
			[NotNull] IItemNavigation itemNavigation,
			[NotNull] Func<ICollection<QualityCondition>, bool> assignToCategory)
			: base(item, view)
		{
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_item = item;
			_view = view;
			_itemNavigation = itemNavigation;
			_assignToCategory = assignToCategory;

			_view.Observer = this;
		}

		#endregion

		#region IQualitySpecificationObserver Members

		void IQualitySpecificationObserver.ElementDoubleClicked(
			QualitySpecificationElementTableRow qualitySpecificationElementTableRow)
		{
			_itemNavigation.GoToItem(
				qualitySpecificationElementTableRow.Element.QualityCondition);
		}

		void IQualitySpecificationObserver.AddQualityConditionsClicked()
		{
			QualitySpecification qualitySpecification = Assert.NotNull(Item.GetEntity());

			IList<QualityConditionWithTestParametersTableRow> selectedConditions =
				_item.GetQualityConditionsToAdd(qualitySpecification, _view);

			if (selectedConditions == null)
			{
				return;
			}

			var newElements = new List<QualitySpecificationElement>();

			foreach (
				QualityConditionWithTestParametersTableRow selectedItem in selectedConditions)
			{
				QualityCondition qualityCondition = selectedItem.QualityCondition;

				if (qualitySpecification.Contains(qualityCondition))
				{
					_msg.WarnFormat(
						"The selected quality condition is already contained in the quality specification");
				}
				else
				{
					newElements.Add(qualitySpecification.AddElement(qualityCondition));
				}
			}

			if (newElements.Count <= 0)
			{
				return;
			}

			AddElements(newElements);

			Item.NotifyChanged();
			UpdateAppearance();
		}

		void IQualitySpecificationObserver.RemoveElementsClicked()
		{
			// get selected targets
			IList<QualitySpecificationElementTableRow> selected =
				_view.GetSelectedElementTableRows();

			int remainingCount = _item.RemoveElements(selected);

			if (remainingCount == 0)
			{
				// no remaining elements, clear the binding list
				_elementTableRows.Clear();
			}
			else
			{
				if (selected.Count > 20 && _view.ElementCount > 80)
				{
					// suspend list changed events
					_elementTableRows.WithSuspendedListChangedEvents(
						() => RemoveElementRows(selected));
				}
				else
				{
					RemoveElementRows(selected);
				}
			}

			Item.NotifyChanged();
			UpdateAppearance();
		}

		void IQualitySpecificationObserver.AssignToCategoryClicked()
		{
			Assert.False(Item.IsDirty, "Item has changes");

			bool assigned = _assignToCategory(_view.GetSelectedElements()
			                                       .Select(element => element.QualityCondition)
			                                       .ToList());

			if (! assigned)
			{
				return;
			}

			foreach (QualitySpecificationElementTableRow row in
			         _view.GetSelectedElementTableRows())
			{
				row.UpdateCategory();
			}

			_view.RefreshElements();
		}

		void IQualitySpecificationObserver.BinderChanged()
		{
			UpdateAppearance();
		}

		void IQualitySpecificationObserver.ElementSelectionChanged()
		{
			UpdateAppearance();
		}

		void IQualitySpecificationObserver.CreateCopyOfQualitySpecification()
		{
			_item.CreateCopy();
			OnBoundTo(Assert.NotNull(_item.GetEntity()));
		}

		void IQualitySpecificationObserver.OnElementsChanged()
		{
			OnBoundTo(Assert.NotNull(_item.GetEntity()));
		}

		void IQualitySpecificationObserver.OpenUrlClicked()
		{
			_item.OpenUrl();
		}

		#endregion

		protected override void OnBoundTo(QualitySpecification qualitySpecification)
		{
			base.OnBoundTo(qualitySpecification);

			RenderElements(qualitySpecification);

			_view.RenderCategory(qualitySpecification.Category == null
				                     ? string.Empty
				                     : qualitySpecification.Category.GetQualifiedName());
		}

		protected override void OnUnloaded()
		{
			_view.SaveState();

			base.OnUnloaded();
		}

		private void UpdateAppearance()
		{
			_view.RemoveElementsEnabled = _view.HasSelectedElements;
			_view.AssignToCategoryEnabled = ! Item.IsDirty && _view.HasSelectedElements;
		}

		private void AddElements([NotNull] ICollection<QualitySpecificationElement> elements)
		{
			foreach (QualitySpecificationElement element in elements)
			{
				_elementTableRows.Add(new QualitySpecificationElementTableRow(element));
			}

			// or refresh binding?
			_view.BindToElements(_elementTableRows);
			_view.SelectElements(elements);
		}

		private void RenderElements(
			[NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			_elementTableRows.Clear();

			HashSet<int> qualityConditionIdsInvolvingDeletedDatasets =
				_item.GetQualityConditionIdsInvolvingDeletedDatasets();

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				bool involvesDeletedDatasets =
					qualityConditionIdsInvolvingDeletedDatasets.Contains(
						element.QualityCondition.Id);

				_elementTableRows.Add(new QualitySpecificationElementTableRow(
					                      element, involvesDeletedDatasets));
			}

			_view.BindToElements(_elementTableRows);
		}

		private void RemoveElementRows(
			[NotNull] IEnumerable<QualitySpecificationElementTableRow> rows)
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			foreach (QualitySpecificationElementTableRow row in rows)
			{
				_elementTableRows.Remove(row);
			}
		}
	}
}
