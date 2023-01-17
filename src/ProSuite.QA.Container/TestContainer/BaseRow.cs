using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	public abstract class BaseRow
	{
		[CanBeNull] private IList<long> _oidList;
		[CanBeNull] private Box _box;

		protected BaseRow([NotNull] IReadOnlyFeature feature,
		                  [CanBeNull] Box box = null,
		                  [CanBeNull] IList<long> oidList = null)
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

		public long OID { get; }

		[NotNull]
		public IList<long> OidList => _oidList ?? (_oidList = GetOidList());

		[NotNull]
		protected static IList<long> GetOidList([NotNull] IReadOnlyFeature feature)
		{
			IReadOnlyTable table = feature.Table;
			if (table.HasOID)
			{
				return new long[] { };
			}

			var fc = (IReadOnlyFeatureClass) table;
			IFields fields = fc.Fields;
			int fieldCount = fields.FieldCount;

			string shapeFieldName = fc.ShapeFieldName;
			int shapeFieldIndex = fields.FindField(shapeFieldName);

			var result = new List<long>();

			// TODO: This deserves some dedicated test coverage!
			// -> Unregistered table with no OID field
			// -> what's the difference in behaviour if there are vs are no int/long fields?
			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				if (fieldIndex == shapeFieldIndex)
				{
					continue;
				}

				// TODO check field type first instead of reading all values? 
				// TODO keep list of int fields in dictionary somewhere (per table?)
				object value = feature.get_Value(fieldIndex);
				if (value is int intValue)
				{
					result.Add(intValue);
				}
				else if (value is long longValue)
				{
					result.Add(longValue);
				}
			}

			return result;
		}

		[NotNull]
		public Box Extent => _box ?? (_box = GetExtent());

		public bool DisjointFromExecuteArea { get; set; }

		[CanBeNull]
		public UniqueId UniqueId { get; }

		[NotNull]
		protected abstract Box GetExtent();

		[NotNull]
		protected abstract IList<long> GetOidList();
	}
}
