using System;
using System.Collections;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public class NulloPicklist : IPicklist
	{
		#region IPicklist Members

		public void Fill(ComboBox comboBox)
		{
			throw new NotImplementedException();
		}

		public void SelectForDisplay(ComboBox comboBox, string display)
		{
			comboBox.SelectedItem = display;
		}

		public string GetDisplay(ComboBox comboBox)
		{
			return comboBox.SelectedItem == null
				       ? string.Empty
				       : comboBox.SelectedItem.ToString();
		}

		public object GetValue(object selectedItem)
		{
			return selectedItem;
		}

		public string DisplayMember
		{
			get { return null; }
			set { }
		}

		public string GetDisplay(object item)
		{
			return item == null
				       ? string.Empty
				       : item.ToString();
		}

		//public void Fill(System.Windows.Controls.ComboBox control)
		//{

		//}

		//public void SelectForDisplay(System.Windows.Controls.ComboBox comboBox, string display)
		//{
		//}

		//public void SetValue(System.Windows.Controls.ComboBox comboBox, object originalValue)
		//{
		//    comboBox.SelectedItem = originalValue;
		//}

		public void SetValue(ComboBox comboBox, object originalValue)
		{
			comboBox.SelectedItem = originalValue;
		}

		public IList Values => Array.Empty<string>();

		public IEnumerator GetEnumerator()
		{
			return Array.Empty<string>().GetEnumerator();
		}

		#endregion
	}
}
