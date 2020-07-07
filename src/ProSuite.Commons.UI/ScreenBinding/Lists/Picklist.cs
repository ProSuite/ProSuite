using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public class Picklist<T> : IPicklist
	{
		private string _displayMember;
		[NotNull] private List<T> _items;
		private string _valueMember;

		public Picklist([NotNull] IEnumerable<T> items,
		                bool noSort = false) : this(items, noSort, null) { }

		public Picklist([NotNull] IEnumerable<T> items,
		                [CanBeNull] IComparer<T> comparer) : this(items, false, comparer) { }

		private Picklist([NotNull] IEnumerable<T> items,
		                 bool noSort,
		                 [CanBeNull] IComparer<T> comparer)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			_items = new List<T>(items);

			if (! noSort)
			{
				if (comparer != null)
				{
					_items.Sort(comparer);
				}
				else if (typeof(IComparable).IsAssignableFrom(typeof(T)) ||
				         typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
				{
					// sort using IComparable
					_items.Sort();
				}

				// else: can't sort since neither a comparer is specified nor does the
				// type implement IComparable
			}
		}

		public Picklist(params T[] items) : this(new List<T>(items)) { }

		public string ValueMember
		{
			get { return _valueMember; }
			set { _valueMember = value; }
		}

		[NotNull]
		public List<T> Items
		{
			get { return _items; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				_items = value;
			}
		}

		public int Count => _items.Count;

		public T this[int i] => _items[i];

		#region IPicklist Members

		public string DisplayMember
		{
			get { return _displayMember; }
			set { _displayMember = value; }
		}

		public IList Values => _items;

		public void Fill(ComboBox comboBox)
		{
			comboBox.BeginUpdate();

			try
			{
				comboBox.Items.Clear();

				foreach (T item in _items)
				{
					comboBox.Items.Add(item);
				}

				if (! string.IsNullOrEmpty(_displayMember))
				{
					comboBox.DisplayMember = _displayMember;
				}

				if (! string.IsNullOrEmpty(_valueMember))
				{
					comboBox.ValueMember = _valueMember;
				}
			}
			finally
			{
				comboBox.EndUpdate();
			}
		}

		// TODO:  return a bool, and blow up in test if it isn't found
		public void SelectForDisplay(ComboBox comboBox, string display)
		{
			if (string.IsNullOrEmpty(_displayMember))
			{
				foreach (T item in _items)
				{
					if (item.ToString().Equals(display))
					{
						comboBox.SelectedItem = item;
						return;
					}
				}
			}

			if (_displayMember != null)
			{
				PropertyInfo displayProperty = typeof(T).GetProperty(_displayMember);

				for (var i = 0; i < comboBox.Items.Count; i++)
				{
					var item = (T) comboBox.Items[i];

					string itemDisplay = displayProperty.GetValue(item, null).ToString();

					if (itemDisplay == display)
					{
						comboBox.SelectedIndex = i;
						return;
					}
				}
			}

			var itemText = new StringBuilder();
			var firstItem = true;
			foreach (T item in comboBox.Items)
			{
				var itemFormat = "'{0}'";
				if (firstItem)
				{
					firstItem = false;
				}
				else
				{
					itemFormat = ", " + itemFormat;
				}

				itemText.AppendFormat(itemFormat, comboBox.GetItemText(item));
			}

			throw new InvalidOperationException(
				string.Format(
					"The Display Value of '{0}' was not found in the List. Valid values are: ({1}).",
					display, itemText));
		}

		public virtual void SetValue(ComboBox comboBox, object originalValue)
		{
			if (originalValue == null)
			{
				comboBox.SelectedItem = null;
				return;
			}

			if (string.IsNullOrEmpty(_valueMember))
			{
				comboBox.SelectedItem = originalValue;
			}
			else
			{
				PropertyInfo property = typeof(T).GetProperty(_valueMember);

				foreach (object item in comboBox.Items)
				{
					object itemValue = property.GetValue(item, null);
					if (originalValue.Equals(itemValue))
					{
						comboBox.SelectedItem = item;
						return;
					}
				}

				comboBox.SelectedIndex = -1;
			}
		}

		public string GetDisplay(ComboBox comboBox)
		{
			object item = comboBox.SelectedItem;
			return GetDisplay(item);
		}

		public string GetDisplay(object item)
		{
			if (item == null)
			{
				return string.Empty;
			}

			if (string.IsNullOrEmpty(_displayMember))
			{
				return item.ToString();
			}

			PropertyInfo displayProperty = typeof(T).GetProperty(_displayMember);
			return displayProperty.GetValue(item, null).ToString();
		}

		//public void Fill(System.Windows.Controls.ComboBox control)
		//{
		//    control.Items.Clear();

		//    foreach (T t in _items)
		//    {
		//        control.Items.Add(t);
		//    }

		//    if (!string.IsNullOrEmpty(_displayMember))
		//    {
		//        control.DisplayMemberPath = _displayMember;
		//    }
		//}

		//public void SelectForDisplay(System.Windows.Controls.ComboBox comboBox, string display)
		//{
		//    if (string.IsNullOrEmpty(_displayMember))
		//    {
		//        foreach (T item in _items)
		//        {
		//            if (item.ToString().Equals(display))
		//            {
		//                comboBox.SelectedValue = item;
		//                return;
		//            }
		//        }
		//    }

		//    PropertyInfo displayProperty = typeof(T).GetProperty(_displayMember);
		//    for (int i = 0; i < comboBox.Items.Count; i++)
		//    {
		//        var item = (T)comboBox.Items[i];

		//        string itemDisplay = displayProperty.GetValue(item, null).ToString();
		//        if (itemDisplay == display)
		//        {
		//            comboBox.SelectedValue = item;
		//            return;
		//        }
		//    }

		//    StringBuilder itemText = new StringBuilder();
		//    bool firstItem = true;
		//    foreach (T item in comboBox.Items)
		//    {
		//        string itemFormat = "'{0}'";
		//        if (firstItem)
		//        {
		//            firstItem = false;
		//        }
		//        else
		//        {
		//            itemFormat = ", " + itemFormat;
		//        }

		//        itemText.AppendFormat(itemFormat, item.ToString());
		//    }

		//    string message = string.Format("The Display Value of '{0}' was not found in the List. Valid values are: ({1}).", display, itemText);
		//    throw new ApplicationException(message);
		//}

		//public void SetValue(System.Windows.Controls.ComboBox comboBox, object originalValue)
		//{
		//    if (originalValue == null)
		//    {
		//        comboBox.SelectedItem = null;
		//        return;
		//    }

		//    if (string.IsNullOrEmpty(_valueMember))
		//    {
		//        comboBox.SelectedItem = originalValue;
		//    }
		//    else
		//    {
		//        PropertyInfo property = typeof(T).GetProperty(_valueMember);
		//        foreach (object item in comboBox.Items)
		//        {
		//            object itemValue = property.GetValue(item, null);
		//            if (originalValue.Equals(itemValue))
		//            {
		//                comboBox.SelectedItem = item;
		//                return;
		//            }
		//        }

		//        comboBox.SelectedIndex = -1;
		//    }
		//}

		public object GetValue(object selectedItem)
		{
			if (selectedItem == null)
			{
				return null;
			}

			if (string.IsNullOrEmpty(_valueMember))
			{
				return selectedItem;
			}

			PropertyInfo property = selectedItem.GetType().GetProperty(_valueMember);
			return property.GetValue(selectedItem, null);
		}

		public IEnumerator GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		#endregion

		[NotNull]
		public object[] GetSelectedItems([NotNull] string[] value)
		{
			Assert.ArgumentNotNull(value, nameof(value));

			if (string.IsNullOrEmpty(_valueMember))
			{
				return value.Cast<object>().ToArray();
			}

			var list = new List<object>();
			PropertyInfo property = typeof(T).GetProperty(_valueMember);
			foreach (T item in _items)
			{
				string itemValue = property.GetValue(item, null).ToString();
				if (Array.IndexOf(value, itemValue) > -1)
				{
					list.Add(item);
				}
			}

			return list.ToArray();
		}

		public void MoveItemsToTopOfList([NotNull] object[] itemsToMoveToTop)
		{
			_items.Sort();
			var tempList = new List<T>();

			foreach (T item in itemsToMoveToTop)
			{
				_items.Remove(item);
				tempList.Add(item);
			}

			_items.InsertRange(0, tempList);
		}

		public void SelectAll([NotNull] ListBox listBox)
		{
			listBox.SelectedItems.Clear();
			foreach (T item in _items)
			{
				listBox.SelectedItems.Add(item);
			}
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var picklist = obj as Picklist<T>;
			if (picklist == null)
			{
				return false;
			}

			return
				Equals(_items, picklist._items) &&
				Equals(_displayMember, picklist._displayMember);
		}

		public override int GetHashCode()
		{
			return
				_items.GetHashCode() +
				29 * (_displayMember != null
					      ? _displayMember.GetHashCode()
					      : 0);
		}

		public void Prepend(T item)
		{
			_items.Insert(0, item);
		}

		public bool Contains(T itemValue)
		{
			return _items.Contains(itemValue);
		}
	}
}
