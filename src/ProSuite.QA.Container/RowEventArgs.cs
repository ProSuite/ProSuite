using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class RowEventArgs : EventArgs
	{
		[CLSCompliant(false)]
		public RowEventArgs([NotNull] IRow row)
		{
			Row = row;
		}

		[CLSCompliant(false)]
		public RowEventArgs([NotNull] IRow row, Guid recycleUnique)
		{
			Row = row;
			Recycled = true;
			RecycleUnique = recycleUnique;
		}

		public bool Cancel { get; set; }

		[NotNull]
		[CLSCompliant(false)]
		public IRow Row { get; }

		public bool Recycled { get; }

		public Guid RecycleUnique { get; }

		public bool IgnoreTestArea { get; set; }
	}
}
