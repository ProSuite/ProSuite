using System;
using System.Collections.Generic;
using System.IO;
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

			if (gdbTable.ObjectClassID >= 0)
			{
				// Do not add views (query layers) with ClassId -1
				_tablesByClassId.Add(gdbTable.ObjectClassID, gdbTable);
			}

			if (! _tablesByName.ContainsKey(gdbTable.Name))
			{
				// Do not add views (query layers) with the same name twice
				_tablesByName.Add(gdbTable.Name, gdbTable);
			}

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

		public override IEnumerable<VirtualTable> GetDatasets(esriDatasetType datasetType)
		{
			return _tablesByName.Values.Where(
				t => t.DatasetType == datasetType ||
				     datasetType == esriDatasetType.esriDTAny);
		}

		public override VirtualTable OpenTable(string name)
		{
			if (_tablesByName.TryGetValue(name, out GdbTable table))
			{
				return table;
			}

			// It could be a m:n or attributed relationship class table
			if (_relClassesByName.TryGetValue(name, out table))
			{
				return table;
			}

			throw new IOException($"Table {name} does not exist in this data store.");
		}

		public override VirtualTable OpenQueryTable(string relationshipClassName)
		{
			return _relClassesByName[relationshipClassName];
		}

		public IObjectClass GetByClassId(int classId)
		{
			return _tablesByClassId[classId];
		}
	}
}
