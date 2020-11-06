using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.Microservices.Server.AO.Geodatabase;

namespace ProSuite.Microservices.Server.AO.Test
{
	[TestFixture]
	public class ProtobufConversionUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanConvertPolygonToFromShapeMsg()
		{
			string xmlFile = TestData.GetDensifiedWorkUnitPerimeterPath();

			var polygon = (IPolygon) GeometryUtils.FromXmlFile(xmlFile);

			var shapeMsg = ProtobufConversionUtils.ToShapeMsg(
				polygon, ShapeMsg.FormatOneofCase.EsriShape,
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);

			IGeometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.NotNull(rehydrated);
			Assert.IsTrue(GeometryUtils.AreEqual(polygon, rehydrated));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(polygon.SpatialReference,
			                                             rehydrated.SpatialReference, true, true));

			// ... and WKB
			var wkbWriter = new WkbGeometryWriter();
			byte[] wkb = wkbWriter.WriteGeometry(polygon);

			var wkbShapeMsg = new ShapeMsg()
			                  {
				                  Wkb = ByteString.CopyFrom(wkb)
			                  };

			IGeometry rehydratedFromWkb = ProtobufConversionUtils.FromShapeMsg(wkbShapeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, rehydratedFromWkb));
			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(polygon.SpatialReference,
				                               rehydrated.SpatialReference));

			// ... and envelope
			IEnvelope envelope = polygon.Envelope;

			var envShapeMsg = new ShapeMsg()
			                  {
				                  Envelope = new EnvelopeMsg()
				                             {
					                             XMin = envelope.XMin,
					                             YMin = envelope.YMin,
					                             XMax = envelope.XMax,
					                             YMax = envelope.YMax
				                             },
				                  SpatialReference = new SpatialReferenceMsg()
				                                     {
					                                     SpatialReferenceWkid =
						                                     (int) WellKnownHorizontalCS.LV95
				                                     }
			                  };

			IEnvelope rehydratedEnvelope =
				(IEnvelope) ProtobufConversionUtils.FromShapeMsg(envShapeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(envelope, rehydratedEnvelope));
		}

		[Test]
		public void CanConvertPolygonToFromShapeMsgFastEnough()
		{
			string xmlFile = TestData.GetHugeLockergesteinPolygonPath();

			var polygon = (IPolygon) GeometryUtils.FromXmlFile(xmlFile);

			var watch = Stopwatch.StartNew();

			var runCount = 100;

			ShapeMsg shapeMsg = null;

			for (int i = 0; i < runCount; i++)
			{
				shapeMsg = ProtobufConversionUtils.ToShapeMsg(polygon);
			}

			long dehydrationAvg = watch.ElapsedMilliseconds / runCount;

			Console.WriteLine("Dehydration: {0}ms", dehydrationAvg);

			watch.Restart();

			IPoint point = new PointClass();

			IGeometry rehydrated = null;
			for (int i = 0; i < runCount; i++)
			{
				rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

				// This is almost free:
				((IPointCollection) rehydrated).QueryPoint(23, point);

				// This results in an extra 45ms on average:
				//point = ((IPointCollection) rehydrated).Point[23];
			}

			long rehydrationAvg = watch.ElapsedMilliseconds / runCount;

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, rehydrated));

			Console.WriteLine("Rehydration: {0}ms", rehydrationAvg);

			Assert.Less(dehydrationAvg, 60);
			Assert.Less(rehydrationAvg, 6);
		}

		[Test]
		public void CanConvertEnvelopeToFromShapeMsg()
		{
			IEnvelope env =
				GeometryFactory.CreateEnvelope(2600000.1234, 1200000.987654, 2601000.12,
				                               1201000.98);

			EnvelopeMsg envelopeMsg = ProtobufConversionUtils.ToEnvelopeMsg(env);

			IEnvelope rehydrated = ProtobufConversionUtils.FromEnvelopeMsg(envelopeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(env, rehydrated));
		}

		[Test]
		public void CanConvertToFromFeature()
		{
			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			var fClass = new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon);

			fClass.SpatialReference = sr;

			GdbFeature featureWithNoShape = new GdbFeature(41, fClass);

			AssertCanConvertToDtoAndBack(featureWithNoShape);

			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon.SpatialReference = sr;

			GdbFeature featureWithShape = new GdbFeature(42, fClass)
			                              {
				                              Shape = polygon
			                              };

			AssertCanConvertToDtoAndBack(featureWithShape);
		}

		[Test]
		public void CanConvertToFromFeatureList()
		{
			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			var fClass1 = new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon);

			fClass1.SpatialReference = sr;

			GdbFeature featureWithNoShape = new GdbFeature(41, fClass1);

			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon.SpatialReference = sr;

			GdbFeature featureWithShape = new GdbFeature(42, fClass1)
			                              {
				                              Shape = polygon
			                              };

			var fClass2 =
				new GdbFeatureClass(124, "TestClass2", esriGeometryType.esriGeometryMultipoint);

			fClass2.SpatialReference = sr;

			GdbFeature multipointFeature = new GdbFeature(41, fClass2);

			AssertCanConvertToDtoAndBack(new List<IFeature>
			                             {
				                             featureWithNoShape,
				                             multipointFeature,
				                             featureWithShape
			                             });
		}

		private static void AssertCanConvertToDtoAndBack(IList<IFeature> features)
		{
			ICollection<GdbObjectMsg> dehydrated = new List<GdbObjectMsg>();
			HashSet<ObjectClassMsg> dehydratedClasses = new HashSet<ObjectClassMsg>();

			ProtobufConversionUtils.ToGdbObjectMsgList(features, dehydrated, dehydratedClasses);

			IList<IFeature> rehydrated =
				ProtobufConversionUtils.FromGdbObjectMsgList(dehydrated, dehydratedClasses);

			Assert.AreEqual(features.Count, rehydrated.Count);

			foreach (IFeature original in features)
			{
				IFeature rehydratedFeature = rehydrated.Single(
					f => GdbObjectUtils.IsSameObject(original, f,
					                                 ObjectClassEquality.SameTableSameVersion));

				Assert.AreEqual(original.Class.ObjectClassID,
				                rehydratedFeature.Class.ObjectClassID);

				Assert.AreEqual(DatasetUtils.GetName(original.Class),
				                DatasetUtils.GetName(rehydratedFeature.Class));

				AssertSameFeature(original, rehydratedFeature);
			}
		}

		private static void AssertCanConvertToDtoAndBack(GdbFeature feature)
		{
			GdbObjectMsg dehydrated =
				ProtobufConversionUtils.ToGdbObjectMsg(feature);

			var featureClass = (GdbFeatureClass) feature.Class;

			GdbFeature rehydrated = ProtobufConversionUtils.FromGdbFeatureMsg(
				dehydrated, new GdbTableContainer(new[] {featureClass}));

			AssertSameFeature(feature, rehydrated);
		}

		private static void AssertSameFeature(IFeature feature, IFeature rehydrated)
		{
			Assert.AreEqual(feature.OID, rehydrated.OID);
			Assert.AreEqual(feature.FeatureType, rehydrated.FeatureType);

			Assert.AreEqual(GdbObjectUtils.GetSubtypeCode(feature),
			                GdbObjectUtils.GetSubtypeCode(rehydrated));

			Assert.AreEqual(feature.Fields.FieldCount, rehydrated.Fields.FieldCount);
			Assert.AreEqual(feature.Class.ObjectClassID, rehydrated.Class.ObjectClassID);

			Assert.IsTrue(GeometryUtils.AreEqual(feature.Shape, rehydrated.Shape));
			Assert.IsTrue(GeometryUtils.AreEqual(feature.Extent, rehydrated.Extent));

			if (feature.Shape != null)
			{
				Assert.IsTrue(SpatialReferenceUtils.AreEqual(feature.Shape.SpatialReference,
				                                             rehydrated.Shape.SpatialReference,
				                                             true,
				                                             true));
			}
		}
	}
}
