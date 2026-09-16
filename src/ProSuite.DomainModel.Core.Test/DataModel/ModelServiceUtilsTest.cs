using System;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Test.DataModel
{
	[TestFixture]
	public class ModelServiceUtilsTest
	{
		[Test]
		public void ReturnsNoUrlsForNullOrEmptyDescription()
		{
			Assert.IsEmpty(ModelServiceUtils.GetServiceUrlsFromDescription(null).ToList());
			Assert.IsEmpty(ModelServiceUtils.GetServiceUrlsFromDescription("").ToList());
			Assert.IsEmpty(
				ModelServiceUtils.GetServiceUrlsFromDescription("just a description").ToList());
		}

		[Test]
		public void CanParseSingleServiceUrl()
		{
			const string url =
				"https://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/FeatureServer";

			string description =
				$"Some notes about the model.\n{ModelServiceUtils.ServiceUrlMarker}{url}";

			var urls = ModelServiceUtils.GetServiceUrlsFromDescription(description).ToList();

			Assert.AreEqual(1, urls.Count);
			Assert.AreEqual(url, urls[0]);
		}

		[Test]
		public void CanParseMultipleServiceUrls()
		{
			const string url1 =
				"https://host/arcgis/rest/services/A/FeatureServer";
			const string url2 =
				"https://host/arcgis/rest/services/B/FeatureServer";

			string description =
				$"Notes\n{ModelServiceUtils.ServiceUrlMarker}{url1}\n" +
				$"more notes\n  {ModelServiceUtils.ServiceUrlMarker}{url2}  ";

			var urls = ModelServiceUtils.GetServiceUrlsFromDescription(description).ToList();

			CollectionAssert.AreEquivalent(new[] { url1, url2 }, urls);
		}

		[Test]
		public void IgnoresTrailingTextAfterUrlOnMarkerLine()
		{
			const string url = "https://host/arcgis/rest/services/A/FeatureServer";

			string description = $"{ModelServiceUtils.ServiceUrlMarker}{url} (production)";

			var urls = ModelServiceUtils.GetServiceUrlsFromDescription(description).ToList();

			Assert.AreEqual(1, urls.Count);
			Assert.AreEqual(url, urls[0]);
		}

		[Test]
		public void ServiceUrlEqualsIgnoresTrailingSlashAndCase()
		{
			Assert.IsTrue(ModelServiceUtils.ServiceUrlEquals(
				              "https://host/services/A/FeatureServer",
				              "https://host/services/A/FeatureServer/"));

			Assert.IsTrue(ModelServiceUtils.ServiceUrlEquals(
				              "https://HOST/services/A/FeatureServer",
				              "https://host/services/A/featureserver"));

			Assert.IsFalse(ModelServiceUtils.ServiceUrlEquals(
				               "https://host/services/A/FeatureServer",
				               "https://host/services/B/FeatureServer"));
		}

		[Test]
		public void StripsLayerPrefixFromFeatureServiceTableName()
		{
			// Pro SDK: layer 0 "Wildfire Response Points" -> "L0Wildfire_Response_Points"
			Assert.AreEqual(
				"Wildfire_Response_Points",
				ModelServiceUtils.StripFeatureServiceLayerPrefix("L0Wildfire_Response_Points"));

			Assert.AreEqual(
				"Streets",
				ModelServiceUtils.StripFeatureServiceLayerPrefix("L12Streets"));
		}

		[Test]
		public void StripsOnlyTheLeadingLayerPrefix()
		{
			// A layer whose (sanitized) name itself starts with "L2_..." must keep that part.
			Assert.AreEqual(
				"L2_Cache",
				ModelServiceUtils.StripFeatureServiceLayerPrefix("L5L2_Cache"));
		}

		[Test]
		public void LeavesNonPrefixedNameUnchanged()
		{
			Assert.AreEqual(
				"Roads",
				ModelServiceUtils.StripFeatureServiceLayerPrefix("Roads"));

			// "Lake" starts with 'L' but no digits follow -> no prefix.
			Assert.AreEqual(
				"Lake",
				ModelServiceUtils.StripFeatureServiceLayerPrefix("Lake"));
		}

		[Test]
		public void MasterDatabaseNameForUnqualifiedModelStripsPrefixOnly()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false);

			Assert.AreEqual(
				"Roads",
				ModelServiceUtils.GetMasterDatabaseModelElementName(model, "L0Roads"));

			Assert.AreEqual(
				"Roads",
				ModelServiceUtils.GetMasterDatabaseModelElementName(model, "Roads"));
		}

		[Test]
		public void MasterDatabaseNameForQualifiedModelUsesSchemaOwner()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: true,
			                             schemaOwner: "TOPGIS");

			Assert.AreEqual(
				"TOPGIS.Roads",
				ModelServiceUtils.GetMasterDatabaseModelElementName(model, "L0Roads"));
		}

		[Test]
		public void MasterDatabaseNameForQualifiedModelUsesDatabaseAndSchemaOwner()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: true,
			                             schemaOwner: "TOPGIS",
			                             databaseName: "PROD");

			Assert.AreEqual(
				"PROD.TOPGIS.Roads",
				ModelServiceUtils.GetMasterDatabaseModelElementName(model, "L0Roads"));
		}

		[Test]
		public void MasterDatabaseNameForQualifiedModelWithoutSchemaOwnerIsNull()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: true);

			Assert.IsNull(
				ModelServiceUtils.GetMasterDatabaseModelElementName(model, "L0Roads"));
		}

		[Test]
		public void MasterDatabaseNameForQualifiedModelKeepsAlreadyQualifiedName()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: true,
			                             schemaOwner: "TOPGIS");

			// After stripping the "L0" prefix the name is already qualified -> used as-is,
			// the model schema owner is not prepended again.
			Assert.AreEqual(
				"OTHER.Roads",
				ModelServiceUtils.GetMasterDatabaseModelElementName(model, "L0OTHER.Roads"));
		}

		[Test]
		public void FindsDatasetOfUnqualifiedModelByServiceTableNameWithSchemaOwner()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC");
			Dataset roads = AddDataset(model, "TLM_STRASSE");
			AddDataset(model, "TLM_GEBAEUDE");

			// The service was published from "TOPGIS_RC.TLM_STRASSE"
			Assert.AreSame(
				roads,
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE"));
		}

		[Test]
		public void FindsDatasetOfUnqualifiedModelByServiceTableNameWithDatabaseAndSchemaOwner()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC",
			                             databaseName: "TOPGISRAW");
			Dataset roads = AddDataset(model, "TLM_STRASSE");

			// The service was published from "TOPGISRAW.TOPGIS_RC.TLM_STRASSE"
			Assert.AreSame(
				roads,
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGISRAW_TOPGIS_RC_TLM_STRASSE"));

			// ... or from the name without the database part
			Assert.AreSame(
				roads,
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE"));
		}

		[Test]
		public void FindsDatasetOfQualifiedModelByServiceTableName()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: true,
			                             schemaOwner: "TOPGIS_RC",
			                             databaseName: "TOPGISRAW");
			Dataset roads = AddDataset(model, "TOPGISRAW.TOPGIS_RC.TLM_STRASSE");

			Assert.AreSame(
				roads,
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGISRAW_TOPGIS_RC_TLM_STRASSE"));

			// the layer may have been published without the database part
			Assert.AreSame(
				roads,
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE"));
		}

		[Test]
		public void ServiceTableNameSearchIgnoresCase()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC");
			Dataset roads = AddDataset(model, "TLM_STRASSE");

			Assert.AreSame(
				roads,
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5topgis_rc_tlm_strasse"));
		}

		[Test]
		public void ServiceTableNameSearchSkipsIgnoredDatasets()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC");
			Dataset roads = AddDataset(model, "TLM_STRASSE");

			Assert.IsNull(
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE", dataset => dataset == roads));
		}

		[Test]
		public void ServiceTableNameSearchReturnsNothingForAmbiguousName()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC");

			// both datasets reduce to the same letters and digits as the service table name:
			// the first one once qualified with the schema owner, the second one as it is
			AddDataset(model, "TLM_STRASSE");
			AddDataset(model, "TOPGIS_RC_TLM_STRASSE");

			Assert.IsNull(
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE"));
		}

		[Test]
		public void ServiceTableNameSearchDoesNotMatchOtherSchemaOwner()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC");
			AddDataset(model, "TLM_STRASSE");

			Assert.IsNull(
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5OTHER_OWNER_TLM_STRASSE"));
		}

		[Test]
		public void ServiceTableNameSearchDoesNotMatchOnNamePartAlone()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false,
			                             schemaOwner: "TOPGIS_RC");
			AddDataset(model, "STRASSE");

			// "TLM_STRASSE" ends with the dataset name, but is not that dataset
			Assert.IsNull(
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE"));
		}

		[Test]
		public void ServiceTableNameSearchNeedsSchemaOwnerForUnqualifiedModel()
		{
			DdxModel model = CreateModel(elementNamesAreQualified: false);
			AddDataset(model, "TLM_STRASSE");

			Assert.IsNull(
				ModelServiceUtils.FindDatasetForServiceTableName(
					model, "L5TOPGIS_RC_TLM_STRASSE"));
		}

		[NotNull]
		private static Dataset AddDataset([NotNull] DdxModel model, [NotNull] string name)
		{
			return model.AddDataset(new TestDataset(name));
		}

		[NotNull]
		private static DdxModel CreateModel(bool elementNamesAreQualified,
		                                    [CanBeNull] string schemaOwner = null,
		                                    [CanBeNull] string databaseName = null)
		{
			return new TestModel("test")
			       {
				       ElementNamesAreQualified = elementNamesAreQualified,
				       DefaultDatabaseSchemaOwner = schemaOwner,
				       DefaultDatabaseName = databaseName
			       };
		}

		private class TestModel : DdxModel
		{
			public TestModel([NotNull] string name) : base(name) { }

			public override string QualifyModelElementName(string modelElementName)
			{
				throw new NotImplementedException();
			}

			public override string TranslateToModelElementName(
				string masterDatabaseDatasetName)
			{
				throw new NotImplementedException();
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }
		}

		private class TestDataset : Dataset
		{
			public TestDataset([NotNull] string name) : base(name) { }

			public override DatasetType DatasetType => DatasetType.FeatureClass;
		}
	}
}
