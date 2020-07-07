using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Logging
{
	internal static class LogWindowUtils
	{
		private const int _indentPixelPerCharacter = 6;

		public static void SetBackColor([NotNull] DataGridViewBand band,
		                                [NotNull] LogEventItem item)
		{
			Assert.ArgumentNotNull(band, nameof(band));
			Assert.ArgumentNotNull(item, nameof(item));

			Color backColor = GetBackColor(item);
			if (backColor.IsEmpty)
			{
				return;
			}

			SetBackColor(band, backColor);
		}

		public static void SetIndentation([NotNull] DataGridViewCell cell,
		                                  [NotNull] LogEventItem item,
		                                  Padding defaultPadding)
		{
			Assert.ArgumentNotNull(cell, nameof(cell));
			Assert.ArgumentNotNull(item, nameof(item));

			if (item.Indentation <= 0)
			{
				return;
			}

			DataGridViewCellStyle style = cell.Style;
			style.Padding = GetCellPadding(item, defaultPadding);
		}

		public static void HandleCellFormattingEvent(
			[NotNull] DataGridViewCellFormattingEventArgs e,
			[NotNull] DataGridViewRow row,
			[CanBeNull] LogEventItem item,
			int messageColumnIndex,
			params int[] textColumns)
		{
			if (item == null)
			{
				return;
			}

			var formatted = false;
			if (e.ColumnIndex == messageColumnIndex)
			{
				if (ApplyPadding(e, item))
				{
					formatted = true;
				}
			}

			if (ApplyBackColor(e, item, textColumns))
			{
				formatted = true;
			}

			e.FormattingApplied = formatted;
		}

		private static Color GetBackColor([NotNull] LogEventItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			switch (item.Level)
			{
				case LogLevel.Warn:
					return LogWindowColors.Warning;

				case LogLevel.Error:
					return LogWindowColors.Error;

				case LogLevel.Fatal:
					return LogWindowColors.Fatal;

				default:
					return Color.Empty;
			}
		}

		private static void SetBackColor(DataGridViewBand band, Color color)
		{
			band.DefaultCellStyle.BackColor = color;
		}

		private static Padding GetCellPadding([NotNull] LogEventItem logEventItem,
		                                      Padding defaultPadding)
		{
			return new Padding(logEventItem.Indentation * _indentPixelPerCharacter,
			                   defaultPadding.Top, defaultPadding.Right,
			                   defaultPadding.Bottom);
		}

		private static bool ApplyPadding([NotNull] DataGridViewCellFormattingEventArgs e,
		                                 [NotNull] LogEventItem item)
		{
			Assert.ArgumentNotNull(e, nameof(e));
			Assert.ArgumentNotNull(item, nameof(item));

			if (item.Indentation <= 0)
			{
				return false;
			}

			Padding padding = e.CellStyle.Padding;

			e.CellStyle.Padding = new Padding(item.Indentation * _indentPixelPerCharacter,
			                                  padding.Top,
			                                  padding.Right,
			                                  padding.Bottom);

			return true;
		}

		private static bool ApplyBackColor(DataGridViewCellFormattingEventArgs e,
		                                   LogEventItem item, params int[] textColumns)
		{
			Assert.ArgumentNotNull(e, nameof(e));
			Assert.ArgumentNotNull(item, nameof(item));

			Color backColor = GetBackColor(item);
			if (backColor.IsEmpty)
			{
				return false;
			}

			// assigning back color requires correct type of cell value
			if (IsContained(textColumns, e.ColumnIndex))
			{
				if (e.Value is string)
				{
					// do nothing
				}
				else if (e.Value is DateTime)
				{
					// TODO cleaner solution needed
					e.Value = ((DateTime) e.Value).ToLongTimeString();
				}
				else
				{
					e.Value = $"{e.Value}";
				}
			}

			e.CellStyle.BackColor = backColor;
			return true;
		}

		private static bool IsContained([NotNull] IEnumerable<int> array, int value)
		{
			return array.Any(t => t == value);
		}
	}
}
