using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class ComboBoxHandler<T>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ComboBoxHandler&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="comboBox">The combo box.</param>
		public ComboBoxHandler([NotNull] ComboBox comboBox)
		{
			Assert.ArgumentNotNull(comboBox, nameof(comboBox));

			ComboBox = comboBox;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ComboBoxHandler&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="toolStripComboBox">The tool strip combo box.</param>
		public ComboBoxHandler([NotNull] ToolStripComboBox toolStripComboBox)
		{
			Assert.ArgumentNotNull(toolStripComboBox, nameof(toolStripComboBox));

			ComboBox = Assert.NotNull(toolStripComboBox.ComboBox);
		}

		#endregion

		[NotNull]
		public ComboBox ComboBox { get; }

		public int ItemCount => ComboBox.Items.Count;

		public T this[int index]
		{
			get
			{
				var item = (ComboItem) ComboBox.Items[index];

				return item.Value;
			}
		}

		[CanBeNull]
		public T GetSelectedValue([CanBeNull] T defaultValue = default(T))
		{
			var item = (ComboItem) ComboBox.SelectedItem;

			return (item != null)
				       ? item.Value
				       : defaultValue;
		}

		public void SetSelectedValue(T value)
		{
			foreach (ComboItem item in ComboBox.Items)
			{
				if (! Equals(value, item.Value))
				{
					continue;
				}

				ComboBox.SelectedItem = item;
				return;
			}

			// No such value => empty combo box (or throw an ArgumentException?)
			ComboBox.SelectedIndex = -1;
			ComboBox.SelectedItem = null;
		}

		public void Add(T value)
		{
			Add(value, value.ToString());
		}

		public void Add(T value, string label)
		{
			ComboBox.Items.Add(new ComboItem(value, label));
		}

		public void FillWithEnum([NotNull] Type enumType)
		{
			FillWithEnum(enumType, obj => obj.ToString());
		}

		public void FillWithEnum([NotNull] Type enumType,
		                         [NotNull] Func<object, string> toLabelFunction)
		{
			Assert.ArgumentNotNull(toLabelFunction, nameof(toLabelFunction));

			ComboBox.BeginUpdate();

			try
			{
				ComboBox.Items.Clear();

				foreach (object obj in Enum.GetValues(enumType))
				{
					var value = (T) obj;
					string label = toLabelFunction(obj);

					ComboBox.Items.Add(new ComboItem(value, label));
				}
			}
			finally
			{
				ComboBox.EndUpdate();
			}
		}

		public void FillWithList(params T[] list)
		{
			FillWithList((IEnumerable<T>) list);
		}

		public void FillWithList([NotNull] IEnumerable<T> list)
		{
			FillWithList(list, ToString);
		}

		public void FillWithList([NotNull] IEnumerable<T> list,
		                         [NotNull] Func<T, string> toLabelFunction)
		{
			Assert.ArgumentNotNull(toLabelFunction, nameof(toLabelFunction));

			ComboBox.BeginUpdate();

			try
			{
				ComboBox.Items.Clear();

				foreach (T value in list)
				{
					string label = toLabelFunction(value);
					ComboBox.Items.Add(new ComboItem(value, label));
				}
			}
			finally
			{
				ComboBox.EndUpdate();
			}
		}

		public void SortByLabel()
		{
			var items = new object[ComboBox.Items.Count];
			ComboBox.Items.CopyTo(items, 0);

			Array.Sort(items); // ComboItem is IComparable on its Label

			ComboBox.BeginUpdate();

			try
			{
				ComboBox.Items.Clear();
				ComboBox.Items.AddRange(items);
			}
			finally
			{
				ComboBox.EndUpdate();
			}
		}

		// TODO SortByLabel(Func<int,string,string> compareFunc);
		// TODO SortByValue(Func<int,T,T> compareFunc);

		public bool Contains(T value)
		{
			// ComboBox.Items.Contains() seems to use ReferenceEquals(),
			// cannot use _comboBox.Items.Contains(new ComboItem(value)).

			foreach (ComboItem item in ComboBox.Items)
			{
				if (Equals(item.Value, value))
				{
					return true;
				}
			}

			return false;
		}

		public void ClearItems()
		{
			ComboBox.Items.Clear();
		}

		#region Non-public

		private static string ToString(T o)
		{
			return o?.ToString() ?? string.Empty;
		}

		#endregion

		#region Nested type: ComboItem

		private class ComboItem : IComparable, IComparable<ComboItem>,
		                          IEquatable<ComboItem>
		{
			// public ComboItem(T value) : this(value, value.ToString()) {}

			public ComboItem(T value, [NotNull] string label)
			{
				// value may be null (sic)
				Assert.ArgumentNotNull(label, nameof(label));

				Value = value;
				Label = label;
			}

			public T Value { get; }

			private string Label { get; }

			#region Equality and collation

			#region IComparable Members

			public int CompareTo(object obj)
			{
				return CompareTo(obj as ComboItem);
			}

			#endregion

			#region IComparable<ComboBoxHandler<T>.ComboItem> Members

			public int CompareTo(ComboItem other)
			{
				if (other == null)
				{
					return -1;
				}

				return string.Compare(Label, other.Label,
				                      StringComparison.OrdinalIgnoreCase);
			}

			#endregion

			#region IEquatable<ComboBoxHandler<T>.ComboItem> Members

			public bool Equals(ComboItem other)
			{
				return other != null && Equals(Value, other.Value);
			}

			#endregion

			#endregion

			public override string ToString()
			{
				return Label;
			}
		}

		#endregion
	}

	//public class ComboItem<T> : IComparable, IComparable<ComboItem<T>>
	//{
	//    private readonly T _value;
	//    private readonly string _label;

	//    public ComboItem(T value, string label)
	//    {
	//        // value may be null (sic)
	//        Assert.ArgumentNotNull(label, "label");

	//        _value = value;
	//        _label = label;
	//    }

	//    public T Value
	//    {
	//        get { return _value; }
	//    }

	//    public string Label
	//    {
	//        get { return _label; }
	//    }

	//    public override string ToString()
	//    {
	//        return _label;
	//    }

	//    public int CompareTo(object obj)
	//    {
	//        return CompareTo(obj as ComboItem<T>);
	//    }

	//    public int CompareTo(ComboItem<T> other)
	//    {
	//        if (other == null)
	//        {
	//            return -1;
	//        }

	//        return string.Compare(Label, other.Label,
	//                              StringComparison.OrdinalIgnoreCase);
	//    }
	//}
}
