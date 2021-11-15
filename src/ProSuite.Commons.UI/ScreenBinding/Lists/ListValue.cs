using System;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public class ListValue : IComparable
	{
		private string _display;
		private object _value;

		public ListValue(string display, object value)
		{
			_display = display;
			_value = value;
		}

		public string Display
		{
			get { return _display; }
			set { _display = value; }
		}

		public object Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			var peer = (ListValue) obj;
			return string.Compare(Display, peer.Display, StringComparison.CurrentCulture);
		}

		#endregion
	}
}
