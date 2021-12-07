using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using System;
using System.Collections.Generic;

namespace ProSuite.QA.Tests.Transformers
{
	internal class TransformerWorkspace : BackingDataStore
	{
		public override void ExecuteSql(string sqlStatement)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<IDataset> GetDatasets(esriDatasetType datasetType)
		{
			throw new NotImplementedException();
		}

		public override ITable OpenQueryTable(string relationshipClassName)
		{
			throw new NotImplementedException();
		}

		public override ITable OpenTable(string name)
		{
			throw new NotImplementedException();
		}
	}

}
