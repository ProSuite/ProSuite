using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcSet : ISet
	{
		private readonly List<Row> _proRowList;
		private int _currentIndex;

		public ArcSet(IEnumerable<Row> rows)
		{
			_proRowList = rows.ToList();
		}

		public ICollection<Row> ProRows => _proRowList;

		#region Implementation of ISet

		public void Add(object unk)
		{
			_proRowList.Add((Row) unk);
		}

		public void Remove(object unk)
		{
			Row rowToRemove = (Row) unk;
			long rowToRemoveOid = rowToRemove.GetObjectID();
			long rowToRemoveTableId = rowToRemove.GetTable().GetID();

			for (int i = 0; i < _proRowList.Count; i++)
			{
				Row item = _proRowList[i];

				if (! IsSameRow(item, rowToRemoveOid, rowToRemoveTableId))
				{
					continue;
				}

				_proRowList.Remove(item);
			}
		}

		private static bool IsSameRow(Row rowToTest, long rowOid, long rowTableId)
		{
			if (rowOid != rowToTest.GetObjectID())
			{
				return false;
			}

			if (rowTableId != rowToTest.GetTable().GetID())
			{
				return false;
			}

			return true;
		}

		public void RemoveAll()
		{
			_proRowList.Clear();
		}

		public object Find(object unk)
		{
			Row rowToFind = (Row) unk;

			long objectIdToFind = rowToFind.GetObjectID();
			long objectTableId = rowToFind.GetTable().GetID();

			Row found = _proRowList.Find(row => IsSameRow(row, objectIdToFind, objectTableId));

			if (found == null)
			{
				return null;
			}

			return ArcGeodatabaseUtils.ToArcRow(found);
		}

		public object Next()
		{
			return _currentIndex >= _proRowList.Count
				       ? null
				       : ArcGeodatabaseUtils.ToArcRow(_proRowList[_currentIndex]);
		}

		public void Reset()
		{
			_currentIndex = 0;
		}

		public int Count => _proRowList.Count;

		#endregion
	}
}
