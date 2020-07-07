using System;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ColumnConfigurationAttribute : Attribute
	{
		public ColumnConfigurationAttribute()
		{
			MinimumWidth = -1;
			Width = -1;
			WrapMode = TriState.NotSet;
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.NotSet;
			Alignment = DataGridViewContentAlignment.NotSet;
		}

		public TriState WrapMode { get; set; }

		public int Width { get; set; }

		public int MinimumWidth { get; set; }

		public DataGridViewAutoSizeColumnMode AutoSizeColumnMode { get; set; }

		public DataGridViewContentAlignment Alignment { get; set; }

		public Padding Padding => new Padding(PaddingLeft, PaddingTop, PaddingRight, PaddingBottom);

		public int PaddingLeft { get; set; }

		public int PaddingRight { get; set; }

		public int PaddingTop { get; set; }

		public int PaddingBottom { get; set; }
	}
}
