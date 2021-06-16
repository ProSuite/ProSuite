using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class GdbTableContainer : BackingDataStore
	{
		private readonly IDictionary<string, GdbTable> _tablesByName =
			new Dictionary<string, GdbTable>();

		private readonly IDictionary<int, GdbTable> _tablesByClassId =
			new Dictionary<int, GdbTable>();

		private readonly IDictionary<string, GdbTable> _relClassesByName =
			new Dictionary<string, GdbTable>();

		public GdbTableContainer() { }

		public GdbTableContainer(
			IEnumerable<GdbTable> tables,
			[CanBeNull] IEnumerable<GdbTable> relClassQueryClasses = null)
		{
			foreach (GdbTable gdbTable in tables)
			{
				TryAdd(gdbTable);
			}

			if (relClassQueryClasses != null)
			{
				foreach (GdbTable relClassQueryTable in relClassQueryClasses)
				{
					_relClassesByName.Add(relClassQueryTable.Name, relClassQueryTable);
				}
			}
		}

		public bool TryAdd(GdbTable gdbTable)
		{
			if (_tablesByClassId.ContainsKey(gdbTable.ObjectClassID))
			{
				return false;
			}

			_tablesByName.Add(gdbTable.Name, gdbTable);
			_tablesByClassId.Add(gdbTable.ObjectClassID, gdbTable);

			return true;
		}

		public bool TryAddRelationshipClass(GdbTable gdbTable)
		{
			if (_relClassesByName.ContainsKey(gdbTable.Name))
			{
				return false;
			}

			_relClassesByName.Add(gdbTable.Name, gdbTable);

			return true;
		}

		public override void ExecuteSql(string sqlStatement)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<IDataset> GetDatasets(esriDatasetType datasetType)
		{
			return _tablesByName.Values.Where(
				t => t.Type == datasetType ||
				     datasetType == esriDatasetType.esriDTAny);
		}

		public override ITable OpenTable(string name)
		{
			return _tablesByName[name];
		}

		public override ITable OpenQueryTable(string relationshipClassName)
		{
			return _relClassesByName[relationshipClassName];
		}

		public IObjectClass GetByClassId(int classId)
		{
			return _tablesByClassId[classId];
		}
	}
}
