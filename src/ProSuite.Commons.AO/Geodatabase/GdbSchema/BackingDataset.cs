using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Abstraction for the data behind a <see cref="GdbTable"/> in case
	/// the table or feature class is used for actual data access rather
	/// than just metadata, such as name or geometry type.
	/// </summary>
	public abstract class BackingDataset
	{
		public abstract IEnvelope Extent { get; }

		public abstract IReadOnlyRow GetRow(int id);

		public abstract int GetRowCount(IQueryFilter queryFilter);

		public abstract IEnumerable<IReadOnlyRow> Search(IQueryFilter filter, bool recycling);
	}
}
