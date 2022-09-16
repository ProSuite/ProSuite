using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public partial class ObjectSubtypeControl<T> : UserControl, IEntityPanel<T>,
	                                               IObjectSubtypeView
		where T : ObjectSubtype
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private IObjectSubtypeObserver _observer;

		private readonly SortableBindingList<ObjectSubtypeCriterionTableRow>
			_criteriaItems =
				new SortableBindingList<ObjectSubtypeCriterionTableRow>();

		public ObjectSubtypeControl()
		{
			InitializeComponent();
		}

		public string Title => "Object Subtype Properties";

		public IObjectSubtypeObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		public void BindTo(ObjectSubtype target) { }

		public void BindToCriteria(
			SortableBindingList<ObjectSubtypeCriterionTableRow> criteriumItems)
		{
			_bindingSourceCriteriaListItem.DataSource = criteriumItems;
		}

		public IList<ObjectSubtypeCriterionTableRow> GetSelectedCriteria()
		{
			var list =
				new List<ObjectSubtypeCriterionTableRow>();
			foreach (DataGridViewRow row in _dataGridViewCriteria.SelectedRows)
			{
				list.Add((ObjectSubtypeCriterionTableRow) row.DataBoundItem);
			}

			return list;
		}

		public bool HasSelectedCriteria => _dataGridViewCriteria.SelectedRows.Count > 0;

		public bool RemoveCriteriaEnabled
		{
			get { return _toolStripButtonRemoveCriteria.Enabled; }
			set { _toolStripButtonRemoveCriteria.Enabled = value; }
		}

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			_dataGridViewCriteria.AutoGenerateColumns = false;
		}

		public void OnBoundTo(T entity)
		{
			_criteriaItems.Clear();

			foreach (ObjectSubtypeCriterion criterium in
			         entity.Criteria)
			{
				_criteriaItems.Add(new ObjectSubtypeCriterionTableRow(criterium));
			}

			_bindingSourceCriteriaListItem.DataSource = _criteriaItems;
		}

		private void _toolStripButtonAddCriterium_Click(object sender, EventArgs e)
		{
			Try(delegate
			{
				if (_observer != null)
				{
					IList<ObjectSubtypeCriterionTableRow> addedItems =
						_observer.AddTargetClicked();

					foreach (ObjectSubtypeCriterionTableRow item in addedItems)
					{
						_criteriaItems.Add(item);
					}
				}
			});
		}

		private void _toolStripButtonRemoveCriterium_Click(object sender, EventArgs e)
		{
			Try(delegate
			{
				if (_observer != null)
				{
					IList<ObjectAttribute> removedAttributes =
						_observer.RemoveTargetClicked();

					foreach (ObjectAttribute attribute in removedAttributes)
					{
						foreach (
							ObjectSubtypeCriterionTableRow criteriaItem in
							_criteriaItems)
						{
							if (Equals(criteriaItem.Attribute, attribute))
							{
								_criteriaItems.Remove(criteriaItem);
								break;
							}
						}
					}
				}
			});
		}

		/// <summary>
		/// Handles the SelectionChanged event of the _dataGridViewCriteria control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void _dataGridViewCriteria_SelectionChanged(object sender, EventArgs e)
		{
			Try(delegate
			{
				if (_observer != null)
				{
					_observer.TargetSelectionChanged();
				}
			});
		}

		private static void Try(Action proc)
		{
			try
			{
				proc();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		private void _dataGridViewCriteria_CellValueChanged(object sender,
		                                                    DataGridViewCellEventArgs e)
		{
			Try(delegate
			{
				if (e.RowIndex < 0 || e.ColumnIndex < 0)
				{
					return;
				}

				if (_observer != null)
				{
					_observer.NotifyChanged(true);
				}
			});
		}

		private void _dataGridViewCriteria_DataError(object sender,
		                                             DataGridViewDataErrorEventArgs e)
		{
			ErrorHandler.HandleError(e.Exception, _msg);
		}
	}
}
