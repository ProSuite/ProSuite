using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class RowEventArgs : EventArgs
	{
		public RowEventArgs([NotNull] IReadOnlyRow row)
		{
			Row = row;
		}

		public RowEventArgs([NotNull] IReadOnlyRow row, Guid recycleUnique)
		{
			Row = row;
			Recycled = true;
			RecycleUnique = recycleUnique;
		}

		public bool Cancel { get; set; }

		[NotNull]
		public IReadOnlyRow Row { get; }

		public bool Recycled { get; }

		public Guid RecycleUnique { get; }

		public bool IgnoreTestArea { get; set; }
	}
}
