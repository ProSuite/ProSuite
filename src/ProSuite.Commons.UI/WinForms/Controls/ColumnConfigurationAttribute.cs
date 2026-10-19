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
			FillWeight = -1;
			WrapMode = TriState.NotSet;
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.NotSet;
			Alignment = DataGridViewContentAlignment.NotSet;
		}

		public TriState WrapMode { get; set; }

		public int Width { get; set; }

		public int MinimumWidth { get; set; }

		/// <summary>
		/// The share of the remaining table width this column gets, relative to the other
		/// columns that fill the remaining width (only relevant for columns whose
		/// <see cref="AutoSizeColumnMode"/> is
		/// <see cref="DataGridViewAutoSizeColumnMode.Fill"/>). Leave unset to give the
		/// column the same share as all others.
		/// </summary>
		public int FillWeight { get; set; }

		public DataGridViewAutoSizeColumnMode AutoSizeColumnMode { get; set; }

		public DataGridViewContentAlignment Alignment { get; set; }

		public Padding Padding => new Padding(PaddingLeft, PaddingTop, PaddingRight, PaddingBottom);

		public int PaddingLeft { get; set; }

		public int PaddingRight { get; set; }

		public int PaddingTop { get; set; }

		public int PaddingBottom { get; set; }
	}
}
