using System;
using System.Diagnostics;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test;
using ProSuite.Microservices.Definitions.Shared;

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

			var shapeMsg = ProtobufConversionUtils.ToShapeMsg(polygon);

			IGeometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, rehydrated));

			// ... and WKB
			var wkbWriter = new WkbGeometryWriter();
			byte[] wkb = wkbWriter.WriteGeometry(polygon);

			var wkbShapeMsg = new ShapeMsg()
			                  {
				                  Wkb = ByteString.CopyFrom(wkb)
			                  };

			IGeometry rehydratedFromWkb = ProtobufConversionUtils.FromShapeMsg(wkbShapeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, rehydratedFromWkb));

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
				                  SpatialReferenceWkid = (int) WellKnownHorizontalCS.LV95
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

			IGeometry rehydrated = null;
			for (int i = 0; i < runCount; i++)
			{
				rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);
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
	}
}
