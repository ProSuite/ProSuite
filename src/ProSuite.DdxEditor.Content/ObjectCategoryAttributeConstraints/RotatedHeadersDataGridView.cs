using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ProSuite.Commons;

namespace ProSuite.DdxEditor.Content.ObjectCategoryAttributeConstraints
{
	public class RotatedHeadersDataGridView : DataGridView
	{
		private double _rotationAngle;

		/// <summary>
		/// Initializes a new instance of the <see cref="RotatedHeadersDataGridView"/> class.
		/// </summary>
		public RotatedHeadersDataGridView()
		{
			_rotationAngle = 0d;
		}

		[Description("Rotation Angle")]
		[Category("Appearance")]
		public double RotationAngle
		{
			get { return _rotationAngle; }
			set
			{
				_rotationAngle = value;
				Invalidate();
			}
		}

		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			if (e.ColumnIndex < 0 || e.RowIndex != -1)
			{
				base.OnCellPainting(e);
				return;
			}

			string text = Columns[e.ColumnIndex].HeaderText;

			//Getting the width and height of the text, which we are going to write
			double columnTextWidth = e.Graphics.MeasureString(text, e.CellStyle.Font).Width;
			double columnTextHeight = e.Graphics.MeasureString(text, e.CellStyle.Font).Height;

			// Erase the cell.
			using (Brush backColorBrush = new SolidBrush(e.CellStyle.BackColor))
			{
				e.Graphics.FillRectangle(backColorBrush, e.CellBounds);
			}

			// only need 1 bottom line...
			e.Graphics.DrawLine(Pens.DarkGray,
			                    e.CellBounds.Left, e.CellBounds.Bottom - 1,
			                    e.CellBounds.Right, e.CellBounds.Bottom - 1);

			// two top lines...
			e.Graphics.DrawLine(Pens.DarkGray,
			                    e.CellBounds.Left, e.CellBounds.Top,
			                    e.CellBounds.Right, e.CellBounds.Top);

			e.Graphics.DrawLine(Pens.White,
			                    e.CellBounds.Left, e.CellBounds.Top + 1,
			                    e.CellBounds.Right, e.CellBounds.Top + 1);

			// right line...
			e.Graphics.DrawLine(Pens.DarkGray,
			                    e.CellBounds.Right - 1, e.CellBounds.Top,
			                    e.CellBounds.Right - 1, e.CellBounds.Bottom);
			// left line...
			e.Graphics.DrawLine(Pens.White,
			                    e.CellBounds.Left, e.CellBounds.Top,
			                    e.CellBounds.Left, e.CellBounds.Bottom);

			//For rotation
			double angle = MathUtils.ToRadians(_rotationAngle);

			double hSin = columnTextHeight * Math.Sin(angle);
			double wCos = columnTextWidth * Math.Cos(angle);
			double hCos = columnTextHeight * Math.Cos(angle);
			double wSin = columnTextWidth * Math.Sin(angle);

			double rotatedWidth = hSin - wCos;
			double rotatedHeight = hCos - wSin;

			double dx = (e.CellBounds.Width + hSin - wCos) / 2;
			//double dy = (e.CellBounds.Height - hCos - wSin) / 2;

			Columns[e.ColumnIndex].Width = (int) Math.Abs(rotatedWidth) + 10;
			int newColHeight = (int) Math.Abs(rotatedHeight) + 10;
			if (ColumnHeadersHeight < newColHeight)
			{
				ColumnHeadersHeight = newColHeight;
			}

			var mx = new Matrix();
			mx.Rotate((float) _rotationAngle, MatrixOrder.Append);
			float heightOffset = e.CellBounds.Y + 10;
			mx.Translate((float) (dx + e.CellBounds.X), heightOffset, MatrixOrder.Append);
			e.Graphics.Transform = mx;

			e.Graphics.DrawString(text, e.CellStyle.Font, Brushes.Black, 0, 0,
			                      StringFormat.GenericTypographic);

			e.Graphics.ResetTransform();
			e.Handled = true;
		}
	}
}
