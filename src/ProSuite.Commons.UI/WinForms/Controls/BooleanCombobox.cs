using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class BooleanCombobox : UserControl
	{
		private bool _value;
		private readonly ListItem _trueItem = new ListItem(true, "Yes");
		private readonly ListItem _falseItem = new ListItem(false, "No");

		#region Constructors

		public BooleanCombobox()
		{
			InitializeComponent();

			Height = _comboBox.Height;

			_comboBox.Items.Add(_trueItem);
			_comboBox.Items.Add(_falseItem);

			RenderValue();
		}

		#endregion

		public event EventHandler ValueChanged;

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

		public bool Value
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

		[NotNull]
		public string TrueText
		{
			get { return _trueItem.Text; }
			set { _trueItem.Text = value; }
		}

		[NotNull]
		public string FalseText
		{
			get { return _falseItem.Text; }
			set { _falseItem.Text = value; }
		}

		#region Non-public members

		private void RenderValue()
		{
			foreach (ListItem listItem in _comboBox.Items)
			{
				if (listItem.Value != _value)
				{
					continue;
				}

				_comboBox.SelectedItem = listItem;
				return;
			}

			throw new InvalidOperationException(
				string.Format("No list item found for value {0}", _value));
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
			private readonly bool _value;
			private string _text;

			public ListItem(bool value, [NotNull] string text)
			{
				Assert.ArgumentNotNullOrEmpty(text, nameof(text));

				_value = value;
				_text = text;
			}

			public bool Value => _value;

			[NotNull]
			public string Text
			{
				get { return _text; }
				set
				{
					Assert.ArgumentNotNullOrEmpty(value, nameof(value));

					_text = value;
				}
			}

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
