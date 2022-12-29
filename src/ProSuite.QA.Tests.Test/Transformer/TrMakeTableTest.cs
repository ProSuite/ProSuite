using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Transformers;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrMakeTableTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanOpenAssociationTable()
		{
			IFeatureWorkspace workspace = OpenTestWorkspaceSde();

			const string baseTableName = "TOPGIS_TLM.TLM_STRASSE";
			const string associationTableName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			IFeatureClass realFeatureClass =
				DatasetUtils.OpenFeatureClass(workspace, baseTableName);
			var baseTable = ReadOnlyTableFactory.Create(realFeatureClass);

			var tr = new TrMakeTable(baseTable, associationTableName);

			const string transformerName = "route_association";
			((ITableTransformer) tr).TransformerName = transformerName;
			IReadOnlyTable associationTable = tr.GetTransformed();

			Assert.AreEqual(associationTable.Name, transformerName);

			long rowCount = associationTable.RowCount(null);

			long checkCount =
				DatasetUtils.OpenTable(workspace, associationTableName).RowCount(null);

			Assert.AreEqual(checkCount, rowCount);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanOpenQueryClass()
		{
			IFeatureWorkspace workspace = OpenTestWorkspaceSde();

			const string baseTableName = "TOPGIS_TLM.TLM_FLIESSGEWAESSER";

			IFeatureClass realFeatureClass =
				DatasetUtils.OpenFeatureClass(workspace, baseTableName);
			var baseTable = ReadOnlyTableFactory.Create(realFeatureClass);

			var tr = new TrMakeTable(baseTable,
			                         "SELECT * FROM TOPGIS_TLM.GEWISS_REGION WHERE REGION LIKE 'V%'",
			                         null);

			const string transformerName = "kantone_mit_V";

			((ITableTransformer) tr).TransformerName = transformerName;
			IReadOnlyTable transformedTable = tr.GetTransformed();

			Assert.AreEqual(transformedTable.Name, transformerName);

			long rowCount = transformedTable.RowCount(null);

			// VD, VS
			Assert.AreEqual(2, rowCount);
		}

		[NotNull]
		[Category(TestCategory.Sde)]
		private static IFeatureWorkspace OpenTestWorkspaceSde()
		{
			string versionName = "TG_SERVICE.RC_TLM_2022-6-30";

			IFeatureWorkspace defaultVersion =
				(IFeatureWorkspace) TestUtils.OpenUserWorkspaceOracle();

			return WorkspaceUtils.OpenFeatureWorkspaceVersion(defaultVersion, versionName);
			return defaultVersion;
		}
	}
}
