using ArcGIS.Core.CIM;
using NUnit.Framework;
using ProSuite.GIS.Geodatabase.AGP.DataConnections;
using esriDatasetType = ArcGIS.Core.CIM.esriDatasetType;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	[TestFixture]
	public class CIMBasedDataConnectionNameTest
	{
		[Test]
		public void CanCreateFromStandardConnectionWithSupportedDatasetType()
		{
			CIMStandardDataConnection connection =
				CreateConnection(esriDatasetType.esriDTMosaicDataset);

			bool created = CIMBasedDataConnectionName.TryCreateFromCIMDataConnection(
				connection, out CIMBasedDataConnectionName result);

			Assert.IsTrue(created);
			Assert.AreEqual("TLM_DTM_MOSAIC", result.NameString);
			Assert.AreEqual(API.esriDatasetType.esriDTMosaicDataset,
			                result.Type);
		}

		// Temporary layers (e.g. from Make Raster Layer) have esriDTAny; this must not throw.
		[Test]
		public void CannotCreateFromStandardConnectionWithUnmappedDatasetType()
		{
			CIMStandardDataConnection connection = CreateConnection(esriDatasetType.esriDTAny);

			bool created = CIMBasedDataConnectionName.TryCreateFromCIMDataConnection(
				connection, out CIMBasedDataConnectionName result);

			Assert.IsFalse(created);
			Assert.IsNull(result);
		}

		private static CIMStandardDataConnection CreateConnection(
			esriDatasetType datasetType)
		{
			return new CIMStandardDataConnection
			       {
				       WorkspaceConnectionString = @"DATABASE=C:\temp\test.gdb",
				       WorkspaceFactory = WorkspaceFactory.FileGDB,
				       Dataset = "TLM_DTM_MOSAIC",
				       DatasetType = datasetType
			       };
		}
	}
}
