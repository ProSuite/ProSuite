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

		public DatasetLookup(IList<BasicDataset> objectDatasets)
		{
			_objectDatasets = objectDatasets;
		}

		[CanBeNull]
		public BasicDataset GetDataset(Table table)
		{
			// TODO: Sophisticated logic

			string tableName = table.GetName();

			// TODO: If model has unqualified name, etc. see GlobalDatasetLookup

			string unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(tableName);

			return _objectDatasets.FirstOrDefault(
				d => d.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase) ||
				     d.Name.Equals(unqualifiedName, StringComparison.InvariantCultureIgnoreCase)
			);
		}
	}
}
