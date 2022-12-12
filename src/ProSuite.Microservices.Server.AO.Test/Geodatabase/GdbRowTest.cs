using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.Test.Geodatabase
{
	[TestFixture]
	public class GdbRowTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanCreateGdbRowFromRealData()
		{
			IWorkspace ws = TestUtils.OpenUserWorkspaceOracle();

			const string tlmStrasse = "TOPGIS_TLM.TLM_STRASSE";

			IFeatureClass realFeatureClass = DatasetUtils.OpenFeatureClass(ws, tlmStrasse);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(realFeatureClass, true);

			GdbTableContainer gdbTableContainer =
				ProtobufConversionUtils.CreateGdbTableContainer(
					new[] { objectClassMsg }, null, out GdbWorkspace _);

			var virtualFeatureClass = (IFeatureClass) gdbTableContainer.OpenTable(tlmStrasse);

			Assert.AreEqual(realFeatureClass.ObjectClassID, virtualFeatureClass.ObjectClassID);
			Assert.AreEqual(DatasetUtils.GetName(realFeatureClass),
			                DatasetUtils.GetName(virtualFeatureClass));
			Assert.AreEqual(realFeatureClass.AliasName, virtualFeatureClass.AliasName);
			Assert.AreEqual(realFeatureClass.ShapeFieldName, virtualFeatureClass.ShapeFieldName);
			Assert.AreEqual(realFeatureClass.OIDFieldName, virtualFeatureClass.OIDFieldName);
			Assert.AreEqual(realFeatureClass.FeatureClassID, virtualFeatureClass.FeatureClassID);
			Assert.AreEqual(realFeatureClass.FeatureType, virtualFeatureClass.FeatureType);
			Assert.AreEqual(realFeatureClass.HasOID, virtualFeatureClass.HasOID);
			Assert.AreEqual(realFeatureClass.ShapeType, virtualFeatureClass.ShapeType);
			Assert.AreEqual(realFeatureClass.ShapeFieldName, virtualFeatureClass.ShapeFieldName);

			Assert.IsTrue(SpatialReferenceUtils.AreEqual(
				              DatasetUtils.GetSpatialReference(realFeatureClass),
				              DatasetUtils.GetSpatialReference(virtualFeatureClass), true, true));

			Assert.AreEqual(realFeatureClass.Fields.FieldCount,
			                virtualFeatureClass.Fields.FieldCount);

			GdbFeatureClass wrappedFeatureClass = new GdbFeatureClass(realFeatureClass);
			int featureCount = 0;
			foreach (var feature in GdbQueryUtils.GetFeatures(realFeatureClass, true))
			{
				// TODO: Move all this to separate project referenced by both client and server
				GdbObjectMsg gdbObjectMsg =
					ProtobufGdbUtils.ToGdbObjectMsg(feature, false, true);

				GdbRow gdbRow =
					ProtobufConversionUtils.FromGdbObjectMsg(
						gdbObjectMsg, wrappedFeatureClass);

				for (int i = 0; i < feature.Fields.FieldCount; i++)
				{
					object expected = feature.get_Value(i);
					object actual = gdbRow.get_Value(i);

					if (expected is IGeometry shape)
					{
						Assert.IsTrue(
							GeometryUtils.AreEqual(shape, (IGeometry) actual));
					}
					else
					{
						Assert.AreEqual(expected, actual);
					}
				}

				featureCount++;

				if (featureCount > 250)
				{
					return;
				}
			}
		}

		[Test]
		public void CanCreateFeatureWithoutTableSchema()
		{
			ISpatialReference sr =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolygon shape = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2600050, 1200080, sr));

			GdbObjectMsg featureMsg = new GdbObjectMsg
			                          {
				                          ClassHandle = -1,
				                          ObjectId = 42,
				                          Shape = ProtobufGeometryUtils.ToShapeMsg(shape)
			                          };

			GdbFeatureClass fClass =
				new GdbFeatureClass(1, "Test", esriGeometryType.esriGeometryPolygon);

			GdbRow gdbRow = ProtobufConversionUtils.FromGdbObjectMsg(featureMsg, fClass);

			GdbFeature feature = (GdbFeature) gdbRow;

			Assert.AreEqual(42, feature.OID);
			Assert.True(GeometryUtils.AreEqual(shape, feature.Shape));
			Assert.AreEqual(1, feature.Class.ObjectClassID);
		}
	}
}
