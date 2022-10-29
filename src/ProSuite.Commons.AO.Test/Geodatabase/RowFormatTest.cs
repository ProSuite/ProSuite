using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class RowFormatTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanFormatRow()
		{
			const string name = "TESTCLASS";
			const string aliasName = "Test FeatureClass";

			var featureClassMock = new FeatureClassMock(
				1, name, aliasName, esriGeometryType.esriGeometryPolyline);

			featureClassMock.AddField("NAME", esriFieldType.esriFieldTypeString);
			featureClassMock.AddField("PLZ", esriFieldType.esriFieldTypeInteger);

			IFeature feature = featureClassMock.CreateFeature();

			Assert.AreEqual($"{feature.OID}", RowFormat.Format(feature));
			Assert.AreEqual($"{aliasName} - {feature.OID}", RowFormat.Format(feature, true));

			var plz = 5023;
			feature.Value[feature.Fields.FindField("PLZ")] = plz;

			Assert.AreEqual($": {plz}", RowFormat.Format(@"{NAME}: {PLZ}", feature));
			Assert.AreEqual($"<null>: {plz}",
			                RowFormat.Format(@"{NAME}: {PLZ}", feature, "<null>"));
		}

		[Test]
		public void CanFormatRowWithFieldNameContainedInOtherField()
		{
			// TOP-5056: Field PLZ_ZZ and PLZ are both present in the feature class:
			const string name = "TESTCLASS";
			const string aliasName = "Test FeatureClass";

			var featureClassMock = new FeatureClassMock(
				1, name, aliasName, esriGeometryType.esriGeometryPolyline);

			featureClassMock.AddField("NAME", esriFieldType.esriFieldTypeString);
			featureClassMock.AddField("PLZ", esriFieldType.esriFieldTypeInteger);
			featureClassMock.AddField("PLZ_ZZ", esriFieldType.esriFieldTypeInteger);

			IFeature feature = featureClassMock.CreateFeature();

			var plz = 5023;
			var zz = 2;
			feature.Value[feature.Fields.FindField("PLZ")] = plz;
			feature.Value[feature.Fields.FindField("PLZ_ZZ")] = zz;

			Assert.AreEqual($"{plz}_{zz}", RowFormat.Format(@"{PLZ}_{PLZ_ZZ}", feature));
		}
	}
}
