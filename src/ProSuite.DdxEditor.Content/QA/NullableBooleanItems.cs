using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA
{
	public static class NullableBooleanItems
	{
		public static void UseFor([NotNull] DataGridViewComboBoxColumn comboBoxColumn,
		                          [NotNull] string defaultText = "Use Default",
		                          [NotNull] string trueText = "Yes",
		                          [NotNull] string falseText = "No")
		{
			Assert.ArgumentNotNull(comboBoxColumn, nameof(comboBoxColumn));

			comboBoxColumn.DataSource = GetNullableBooleanItems(defaultText,
			                                                    trueText,
			                                                    falseText);
			comboBoxColumn.ValueMember = "Value";
			comboBoxColumn.DisplayMember = "Name";
		}

		[NotNull]
		public static IList<NullableBooleanItem> GetNullableBooleanItems(
			[NotNull] string defaultText = "Use Default",
			[NotNull] string trueText = "Yes",
			[NotNull] string falseText = "No")
		{
			return new List<NullableBooleanItem>
			       {
				       new NullableBooleanItem(BooleanOverride.UseDefault, defaultText),
				       new NullableBooleanItem(BooleanOverride.Yes, trueText),
				       new NullableBooleanItem(BooleanOverride.No, falseText)
			       };
		}

		public static bool? GetNullableBoolean(BooleanOverride value)
		{
			switch (value)
			{
				case BooleanOverride.UseDefault:
					return null;

				case BooleanOverride.Yes:
					return true;

				case BooleanOverride.No:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(value));
			}
		}

		public static BooleanOverride GetBooleanOverride(bool? value)
		{
			if (! value.HasValue)
			{
				return BooleanOverride.UseDefault;
			}

			return value.Value
				       ? BooleanOverride.Yes
				       : BooleanOverride.No;
		}
	}
}
