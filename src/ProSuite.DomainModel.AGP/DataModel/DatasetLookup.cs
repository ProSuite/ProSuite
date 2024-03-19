using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AGP.DataModel
{
	public class DatasetLookup
	{
		private readonly IList<BasicDataset> _objectDatasets;

		private readonly IDictionary<long, BasicDataset> _datasetByTableHandle =
			new Dictionary<long, BasicDataset>();

		public DatasetLookup(IList<BasicDataset> objectDatasets)
		{
			_objectDatasets = objectDatasets;
		}

		[CanBeNull]
		public BasicDataset GetDataset([NotNull] Table table)
		{
			// TODO: Sophisticated logic with unqualified names, etc.

			var tableHandle = (long) table.Handle;

			if (_datasetByTableHandle.TryGetValue(tableHandle, out var dataset))
			{
				return dataset;
			}

			string tableName = table.GetName();

			// TODO: If model has unqualified name, etc. see GlobalDatasetLookup

			string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(tableName);

			BasicDataset result = _objectDatasets.FirstOrDefault(
				d => d.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase) ||
				     d.Name.Equals(unqualifiedName, StringComparison.InvariantCultureIgnoreCase));

			if (result != null)
			{
				_datasetByTableHandle[tableHandle] = result;
			}

			return result;
		}
	}
}
