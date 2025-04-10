using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using TestCategory = ProSuite.Commons.Test.TestCategory;

namespace ProSuite.Microservices.Server.AO.Test
{
	[TestFixture]
	public class ProtobufConversionUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanConvertPolygonToFromShapeMsg()
		{
			string xmlFile = TestData.GetDensifiedWorkUnitPerimeterPath();

			var polygon = (IPolygon) GeometryUtils.FromXmlFile(xmlFile);

			var shapeMsg = ProtobufGeometryUtils.ToShapeMsg(
				polygon, ShapeMsg.FormatOneofCase.EsriShape,
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);

			IGeometry rehydrated = ProtobufGeometryUtils.FromShapeMsg(shapeMsg);

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

			IGeometry rehydratedFromWkb = ProtobufGeometryUtils.FromShapeMsg(wkbShapeMsg);

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
				(IEnvelope) ProtobufGeometryUtils.FromShapeMsg(envShapeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(envelope, rehydratedEnvelope));
		}

		[Test]
		[Category(TestCategory.Performance)]
		public void CanConvertPolygonToFromShapeMsgFastEnough()
		{
			string xmlFile = TestData.GetHugeLockergesteinPolygonPath();

			var polygon = (IPolygon) GeometryUtils.FromXmlFile(xmlFile);

			var watch = Stopwatch.StartNew();

			var runCount = 100;

			ShapeMsg shapeMsg = null;

			for (int i = 0; i < runCount; i++)
			{
				shapeMsg = ProtobufGeometryUtils.ToShapeMsg(polygon);
			}

			long dehydrationAvg = watch.ElapsedMilliseconds / runCount;

			Console.WriteLine("Dehydration: {0}ms", dehydrationAvg);

			watch.Restart();

			IPoint point = new PointClass();

			IGeometry rehydrated = null;
			for (int i = 0; i < runCount; i++)
			{
				rehydrated = ProtobufGeometryUtils.FromShapeMsg(shapeMsg);

				// This is almost free:
				((IPointCollection) rehydrated).QueryPoint(23, point);

				// This results in an extra 45ms on average:
				//point = ((IPointCollection) rehydrated).Point[23];
			}

			long rehydrationAvg = watch.ElapsedMilliseconds / runCount;

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, rehydrated));

			Console.WriteLine("Rehydration: {0}ms", rehydrationAvg);

			Assert.Less(dehydrationAvg, 60);
			Assert.Less(rehydrationAvg, 30);

			// Typical output on a reasonable laptop:
			//Dehydration: 45ms
			//Rehydration: 20ms

			// x86, ArcGIS 10.8, Ryzen 9 5900HS:
			//Dehydration: 30ms
			//Rehydration: 22ms

			//
			// x64, ArcGIS 10.9.1, Ryzen 9 5900HS:
			//Dehydration: 24ms
			//Rehydration: 6ms
		}

		[Test]
		public void CanConvertEnvelopeToFromShapeMsg()
		{
			IEnvelope env =
				GeometryFactory.CreateEnvelope(2600000.1234, 1200000.987654, 2601000.12,
				                               1201000.98);

			EnvelopeMsg envelopeMsg = ProtobufGeometryUtils.ToEnvelopeMsg(env);

			IEnvelope rehydrated = ProtobufGeometryUtils.FromEnvelopeMsg(envelopeMsg);

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

			GdbFeature featureWithNoShape = GdbFeature.Create(41, fClass);

			AssertCanConvertToDtoAndBack(featureWithNoShape);

			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon.SpatialReference = sr;

			GdbFeature featureWithShape = GdbFeature.Create(42, fClass);
			featureWithShape.Shape = polygon;

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

			GdbFeature featureWithNoShape = GdbFeature.Create(41, fClass1);

			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon.SpatialReference = sr;

			GdbFeature featureWithShape = GdbFeature.Create(42, fClass1);
			featureWithShape.Shape = polygon;

			var fClass2 =
				new GdbFeatureClass(124, "TestClass2", esriGeometryType.esriGeometryMultipoint);

			fClass2.SpatialReference = sr;

			GdbFeature multipointFeature = GdbFeature.Create(41, fClass2);

			AssertCanConvertToDtoAndBack(new List<IFeature>
			                             {
				                             featureWithNoShape,
				                             multipointFeature,
				                             featureWithShape
			                             });
		}

		[Test]
		public void CanCreateXmlConditionElement()
		{
			const string categoryName = "Cat";
			const string conditionName = "someCondition";
			const string conditionDescription = "someDescription";
			const string testDescriptorName = "descriptor";
			const string url = "http://www.someUrl.com";

			QualitySpecificationElementMsg elementMsg =
				new QualitySpecificationElementMsg()
				{
					CategoryName = categoryName,
					Condition = new QualityConditionMsg()
					            {
						            Name = conditionName,
						            Description = conditionDescription,
						            TestDescriptorName = testDescriptorName,
						            Url = url
					            }
				};

			SpecificationElement xmlConditionElement =
				ProtobufConversionUtils.CreateXmlConditionElement(elementMsg);

			Assert.AreEqual(xmlConditionElement.CategoryName, categoryName);
			Assert.AreEqual(xmlConditionElement.XmlCondition.Name, conditionName);
			Assert.AreEqual(xmlConditionElement.XmlCondition.Description, conditionDescription);
			Assert.AreEqual(xmlConditionElement.XmlCondition.TestDescriptorName,
			                testDescriptorName);
			Assert.AreEqual(xmlConditionElement.XmlCondition.Url, url);
		}

		private static void AssertCanConvertToDtoAndBack(IList<IFeature> features)
		{
			ICollection<GdbObjectMsg> dehydrated = new List<GdbObjectMsg>();
			HashSet<ObjectClassMsg> dehydratedClasses = new HashSet<ObjectClassMsg>();

			ProtobufGdbUtils.ToGdbObjectMsgList(features, dehydrated, dehydratedClasses);

			IList<IFeature> rehydrated =
				ProtobufConversionUtils.FromGdbObjectMsgList(dehydrated, dehydratedClasses);

			Assert.AreEqual(features.Count, rehydrated.Count);

			foreach (IFeature original in features)
			{
				IFeature rehydratedFeature = rehydrated.Single(
					f => original.OID == f.OID &&
					     original.Class.ObjectClassID == f.Class.ObjectClassID);

				Assert.AreEqual(original.Class.ObjectClassID,
				                rehydratedFeature.Class.ObjectClassID);

				Assert.AreEqual(DatasetUtils.GetName(original.Class),
				                DatasetUtils.GetName(rehydratedFeature.Class));

				AssertSameFeature(original, rehydratedFeature);

				// NOTE: For consistent dictionary usage, we need to stick to AO-equality
				// and GetHashCode implementation (reference-equals only). For actual equality
				// of different instances we need a different concept:
				// - TableIdentity?
				// - Implement IEquatable.Equals<T> (which is a bit of an abuse tbh)
				// - Separate interface(s): ITableEquality(RO...)
				// - The client code remains in charge to determine what's equal
				bool sameObject = GdbObjectUtils.IsSameObject(
					original, rehydratedFeature, ObjectClassEquality.SameTableSameVersion);

				// Counter-intuitive, but equality so far means reference equality for ITable
				// implementations
				Assert.IsFalse(sameObject);
			}
		}

		private static void AssertCanConvertToDtoAndBack(GdbFeature feature)
		{
			GdbObjectMsg dehydrated =
				ProtobufGdbUtils.ToGdbObjectMsg(feature);

			var featureClass = (GdbFeatureClass) feature.Class;

			GdbFeature rehydrated = ProtobufConversionUtils.FromGdbFeatureMsg(
				dehydrated, () => featureClass);

			AssertSameFeature(feature, rehydrated);
		}

		private static void AssertSameFeature(IFeature feature, IFeature rehydrated)
		{
			Assert.AreEqual(feature.OID, rehydrated.OID);
			Assert.AreEqual(feature.FeatureType, rehydrated.FeatureType);

			Assert.AreEqual(GdbObjectUtils.GetSubtypeCode(feature),
			                GdbObjectUtils.GetSubtypeCode(rehydrated));

			int newFieldCount = rehydrated.Fields.FieldCount;
			// NOTE: We add the OID field explicitly to prevent functionality degradation
			//       e.g. in expression filters.
			Assert.IsTrue(newFieldCount >= feature.Fields.FieldCount);

			Assert.AreEqual(feature.Class.ObjectClassID, rehydrated.Class.ObjectClassID);

			Assert.IsTrue(GeometryUtils.AreEqual(feature.Shape, rehydrated.Shape));
			Assert.IsTrue(GeometryUtils.AreEqual(feature.Extent, rehydrated.Extent));

			if (feature.Shape != null)
			{
				Assert.IsTrue(SpatialReferenceUtils.AreEqual(
					              feature.Shape.SpatialReference, rehydrated.Shape.SpatialReference,
					              true, true));
			}
		}
	}
}
