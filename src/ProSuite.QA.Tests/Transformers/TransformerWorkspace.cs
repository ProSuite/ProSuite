using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;

namespace ProSuite.QA.Tests.Transformers
{
	internal class TransformerWorkspace : BackingDataStore
	{
		public override void ExecuteSql(string sqlStatement)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<VirtualTable> GetDatasets(esriDatasetType datasetType)
		{
			throw new NotImplementedException();
		}

		public override VirtualTable OpenQueryTable(string relationshipClassName)
		{
			throw new NotImplementedException();
		}

		public override VirtualTable OpenTable(string name)
		{
			throw new NotImplementedException();
		}
	}
}
