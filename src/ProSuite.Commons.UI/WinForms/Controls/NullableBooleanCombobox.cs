using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class NullableBooleanCombobox : UserControl
	{
		private bool? _value;
		private string _trueText = "Yes";
		private string _falseText = "No";
		private string _defaultText = "<Null>";
		private bool _itemsDirty = true;

		#region Constructors

		public NullableBooleanCombobox()
		{
			InitializeComponent();

			Height = _comboBox.Height;

			RenderValue();
		}

		#endregion

		public event EventHandler ValueChanged;

		[NotNull]
		public string TrueText
		{
			get { return _trueText; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				if (Equals(_trueText, value))
				{
					return;
				}

				_trueText = value;
				_itemsDirty = true;
			}
		}

		[NotNull]
		public string FalseText
		{
			get { return _falseText; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				if (Equals(_falseText, value))
				{
					return;
				}

				_falseText = value;
				_itemsDirty = true;
			}
		}

		[NotNull]
		public string DefaultText
		{
			get { return _defaultText; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				if (Equals(_defaultText, value))
				{
					return;
				}

				_defaultText = value;
				_itemsDirty = true;
			}
		}

		public override Color BackColor
		{
			get { return _comboBox.BackColor; }
			set { _comboBox.BackColor = value; }
		}

		public FlatStyle FlatStyle
		{
			get { return _comboBox.FlatStyle; }
			set { _comboBox.FlatStyle = value; }
		}

		public bool? Value
		{
			get { return _value; }
			set
			{
				if (_value == value)
				{
					return;
				}

				_value = value;

				RenderValue();

				OnValueChanged(EventArgs.Empty);
			}
		}

		#region Non-public members

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			EnsureItemsClean();
		}

		private void EnsureItemsClean()
		{
			if (! _itemsDirty)
			{
				return;
			}

			_comboBox.BeginUpdate();

			try
			{
				_comboBox.Items.Clear();

				_comboBox.Items.Add(new ListItem(true, TrueText));
				_comboBox.Items.Add(new ListItem(false, FalseText));
				_comboBox.Items.Add(new ListItem(null, DefaultText));
			}
			finally
			{
				_comboBox.EndUpdate();
			}

			_itemsDirty = false;

			RenderValue();
		}

		private void RenderValue()
		{
			EnsureItemsClean();

			foreach (ListItem listItem in _comboBox.Items)
			{
				if (listItem.Value == _value)
				{
					_comboBox.SelectedItem = listItem;
					return;
				}
			}

			Assert.CantReach("No list item found for value {0}", _value);
		}

		protected virtual void OnValueChanged(EventArgs e)
		{
			if (ValueChanged != null)
			{
				ValueChanged(this, e);
			}
		}

		#region Event handlers

		private void _comboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var selectedItem = (ListItem) _comboBox.SelectedItem;

			Value = selectedItem.Value;
		}

		#endregion

		#endregion

		#region Nested types

		private class ListItem : IEquatable<ListItem>
		{
			private readonly bool? _value;
			private readonly string _text;

			public ListItem(bool? value, [NotNull] string text)
			{
				Assert.ArgumentNotNullOrEmpty(text, nameof(text));

				_value = value;
				_text = text;
			}

			public bool? Value => _value;

			public override string ToString()
			{
				return _text;
			}

			public bool Equals(ListItem listItem)
			{
				if (listItem == null)
				{
					return false;
				}

				return Equals(_value, listItem._value);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				return Equals(obj as ListItem);
			}

			public override int GetHashCode()
			{
				return _value.GetHashCode();
			}
		}

		#endregion
	}
}
