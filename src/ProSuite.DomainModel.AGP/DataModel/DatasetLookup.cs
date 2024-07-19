using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel
{
	public class DatasetLookup : IDatasetLookup
	{
		private readonly IList<IDdxDataset> _objectDatasets;

		private readonly IDictionary<long, IDdxDataset> _datasetByTableHandle =
			new Dictionary<long, IDdxDataset>();

		public DatasetLookup(IList<IDdxDataset> objectDatasets)
		{
			_objectDatasets = objectDatasets;
		}

		public T GetDataset<T>(Table table) where T : IDdxDataset
		{
			// TODO: Sophisticated logic with unqualified names, etc.

			var tableHandle = (long) table.Handle;

			if (_datasetByTableHandle.TryGetValue(tableHandle, out var dataset))
			{
				if (dataset is T value)
				{
					return value;
				}
			}

			string tableName = table.GetName();

			// TODO: If model has unqualified name, etc. see GlobalDatasetLookup

			string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(tableName);

			T result = _objectDatasets.OfType<T>().FirstOrDefault(
				d => d.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase) ||
				     d.Name.Equals(unqualifiedName, StringComparison.InvariantCultureIgnoreCase));

			// Todo daro: Hack GoTop-138 for tables from FGDB.
			if (result == null)
			{
				result = _objectDatasets.OfType<T>().FirstOrDefault(
					d => ModelElementNameUtils.GetUnqualifiedName(d.Name).Equals(tableName, StringComparison.InvariantCultureIgnoreCase) ||
					     ModelElementNameUtils.GetUnqualifiedName(d.Name).Equals(unqualifiedName, StringComparison.InvariantCultureIgnoreCase));
			}

			_datasetByTableHandle[tableHandle] = result;

			return result;
		}

		[CanBeNull]
		public IDdxDataset GetDataset(Table table)
		{
			return GetDataset<IDdxDataset>(table);
		}
	}
}
