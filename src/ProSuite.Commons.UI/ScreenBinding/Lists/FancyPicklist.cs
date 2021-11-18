using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public class FancyPicklist : Picklist<ListValue>
	{
		public FancyPicklist() : base(Array.Empty<ListValue>())
		{
			Configure();
		}

		public FancyPicklist([NotNull] IEnumerable<ListValue> items) : base(items)
		{
			Configure();
		}

		public FancyPicklist([NotNull] ListValue[] items) : base(items)
		{
			Configure();
		}

		private void Configure()
		{
			ValueMember = "Value";
			DisplayMember = "Display";
		}

		public object GetValueForDisplay(string display)
		{
			ListValue listValue =
				Items.Find(
					v => string.Equals(v.Display, display,
					                   StringComparison.OrdinalIgnoreCase));

			if (listValue == null)
			{
				throw new ArgumentException(@"Could not find a Column with heading " +
				                            display, nameof(display));
			}

			return listValue.Value;
		}

		public void AddValue(string display, string innerValue)
		{
			var listValue = new ListValue(display, innerValue);
			Items.Add(listValue);
		}

		public void SetDefault(string display, string innerValue)
		{
			var listValue = new ListValue(display, innerValue);
			Items.Insert(0, listValue);
		}
	}
}
