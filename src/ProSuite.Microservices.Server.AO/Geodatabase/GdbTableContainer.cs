using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Microservices.Server.AO.Geodatabase
{
	public class GdbTableContainer : BackingDataStore
	{
		private readonly IDictionary<string, GdbTable> _tablesByName =
			new Dictionary<string, GdbTable>();

		private readonly IDictionary<int, GdbTable> _tablesByClassId =
			new Dictionary<int, GdbTable>();

		public GdbTableContainer() { }

		public GdbTableContainer(IEnumerable<GdbTable> tables)
		{
			foreach (GdbTable gdbTable in tables)
			{
				TryAdd(gdbTable);
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

		public override void ExecuteSql(string sqlStatement)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<IDataset> GetDatasets(esriDatasetType datasetType)
		{
			return _tablesByName.Values.Where(t => t.Type == datasetType);
		}

		public override ITable OpenTable(string name)
		{
			return _tablesByName[name];
		}

		public IObjectClass GetByClassId(int classId)
		{
			return _tablesByClassId[classId];
		}
	}
}
