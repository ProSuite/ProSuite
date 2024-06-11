using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel
{
	public class DatasetLookup : IDatasetLookup
	{
		protected IList<IDdxDataset> ObjectDatasets { get; }

		private readonly IDictionary<long, IDdxDataset> _datasetByTableHandle =
			new Dictionary<long, IDdxDataset>();

		public DatasetLookup(IList<IDdxDataset> objectDatasets)
		{
			ObjectDatasets = objectDatasets;
		}

		public T GetDataset<T>(Table table) where T : IDdxDataset
		{
			var tableHandle = (long) table.Handle;

			if (_datasetByTableHandle.TryGetValue(tableHandle, out var dataset))
			{
				if (dataset is T value)
				{
					return value;
				}
			}

			// 1. Extract actual feature class from joined table:
			// Assumption: When providing a joined feature class the caller wants the dataset containing the shape field.
			// TODO: Deal with joined table (standalone table) by using OID field to determine the desired dataset.
			if (table is FeatureClass featureClass && table.IsJoinedTable())
			{
				table = DatasetUtils.GetDatabaseFeatureClass(featureClass);
			}

			// 2. TODO: Sophisticated logic with unqualified names, etc. (see GlobalDatasetLookup)

			string tableName = table.GetName();

			string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(tableName);

			T result = ObjectDatasets.OfType<T>().FirstOrDefault(
				d => d.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase) ||
				     d.Name.Equals(unqualifiedName, StringComparison.InvariantCultureIgnoreCase));

			// Todo daro: Hack GoTop-138 for tables from FGDB.
			if (result == null)
			{
				result = ObjectDatasets.OfType<T>().FirstOrDefault(
					d => UnqualifiedDatasetNameEquals(d, tableName) ||
					     DatasetNameEquals(d, unqualifiedName));
			}

			_datasetByTableHandle[tableHandle] = result;

			return result;
		}

		[CanBeNull]
		public IDdxDataset GetDataset(Table table)
		{
			return GetDataset<IDdxDataset>(table);
		}

		private static bool DatasetNameEquals<T>(T dataset, string name) where T : IDdxDataset
		{
			string datasetName = dataset.Name;

			return datasetName.Equals(name, StringComparison.InvariantCultureIgnoreCase);
		}

		private static bool UnqualifiedDatasetNameEquals<T>(T dataset, string name)
			where T : IDdxDataset
		{
			string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(dataset.Name);

			return unqualifiedName.Equals(name, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
