using System;
using System.Collections;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Lists;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class PicklistElement : BoundScreenElement<ComboBox, object>, IListElement
	{
		private IPicklist _list = new NulloPicklist();

		public PicklistElement([NotNull] IPropertyAccessor accessor,
		                       [NotNull] ComboBox control)
			: base(accessor, control)
		{
			Setup(control);

			// Have to be set in this order for combobox autofill
			//control.AutoCompleteMode = AutoCompleteMode.Suggest;
			//control.AutoCompleteSource = AutoCompleteSource.ListItems;
			control.DropDownStyle = ComboBoxStyle.DropDownList;
		}

		protected void Setup([NotNull] ComboBox control)
		{
			control.SelectedValueChanged += control_SelectedValueChanged;
		}

		[NotNull]
		public IList Items => BoundControl.Items;

		[NotNull]
		public string Display => _list.GetDisplay(BoundControl);

		#region IListElement Members

		public void FillWithList(IPicklist list)
		{
			ComboBox comboBox = BoundControl;

			comboBox.SelectedValueChanged -= control_SelectedValueChanged;
			try
			{
				comboBox.SelectedItem = null;

				list.Fill(comboBox);
				_list = list;

				if (IsBound)
				{
					Update();
				}
			}
			finally
			{
				comboBox.SelectedValueChanged += control_SelectedValueChanged;
			}
		}

		public void FillWithList(string[] strings)
		{
			var list = new Picklist<string>(strings);

			FillWithList(list);
		}

		public void FillWithEnum<T>() where T : struct, IComparable, IFormattable
		{
			var list = new Picklist<T>(EnumUtils.GetList<T>());
			FillWithList(list);
		}

		public void FillWith<T>(params T[] items) where T : IComparable
		{
			var list = new Picklist<T>(items);
			FillWithList(list);
		}

		public string DisplayValue => _list.GetDisplay(BoundControl);

		public IList GetListOfItems()
		{
			return BoundControl.Items;
		}

		public void SelectByDisplay(string display)
		{
			_list.SelectForDisplay(BoundControl, display);
		}

		#endregion

		protected override void TearDown()
		{
			BoundControl.SelectedValueChanged -= control_SelectedValueChanged;
		}

		private void control_SelectedValueChanged(object sender, EventArgs e)
		{
			ElementValueChanged();
		}

		protected override object GetValueFromControl()
		{
			return _list.GetValue(BoundControl.SelectedItem);
		}

		protected override void ResetControl(object originalValue)
		{
			_list.SetValue(BoundControl, originalValue);
		}

		public void SelectDisplay(string display)
		{
			_list.SelectForDisplay(BoundControl, display);
		}

		public bool HasSelection()
		{
			return BoundControl.SelectedItem != null;
		}

		public override void SetDefaults()
		{
			if (BoundControl.Items.Count == 1)
			{
				ResetControl(BoundControl.Items[0]);
			}
			else
			{
				base.SetDefaults();
			}
		}
	}
}
