using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	public abstract class BaseRow
	{
		[CanBeNull] private IList<int> _oidList;
		[CanBeNull] private Box _box;

		protected BaseRow([NotNull] IReadOnlyFeature feature,
		                  [CanBeNull] Box box = null,
		                  [CanBeNull] IList<int> oidList = null)
		{
			Table = feature.Table;
			OID = feature.OID;

			var uniqueIdObject = feature as IUniqueIdObject;
			UniqueId = uniqueIdObject?.UniqueId;

			_oidList = oidList;
			_box = box;
		}

		[CanBeNull]
		public IReadOnlyTable Table { get; }

		public int OID { get; }

		[NotNull]
		public IList<int> OidList => _oidList ?? (_oidList = GetOidList());

		[NotNull]
		protected static IList<int> GetOidList([NotNull] IReadOnlyFeature feature)
		{
			IReadOnlyTable table = feature.Table;
			if (table.HasOID)
			{
				return new int[] { };
			}

			var fc = (IReadOnlyFeatureClass) table;
			IFields fields = fc.Fields;
			int fieldCount = fields.FieldCount;

			string shapeFieldName = fc.ShapeFieldName;
			int shapeFieldIndex = fields.FindField(shapeFieldName);

			var result = new List<int>();

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				if (fieldIndex == shapeFieldIndex)
				{
					continue;
				}

				// TODO check field type first instead of reading all values? 
				// TODO keep list of int fields in dictionary somewhere (per table?)
				object value = feature.get_Value(fieldIndex);
				if (value is int)
				{
					result.Add((int) value);
				}
			}

			return result;
		}

		[NotNull]
		public Box Extent => _box ?? (_box = GetExtent());

		public bool IsFirstOccurrenceX { get; set; } = true;

		public bool IsFirstOccurrenceY { get; set; } = true;

		public bool DisjointFromExecuteArea { get; set; }

		[CanBeNull]
		public UniqueId UniqueId { get; }

		[NotNull]
		protected abstract Box GetExtent();

		[NotNull]
		protected abstract IList<int> GetOidList();
	}
}
