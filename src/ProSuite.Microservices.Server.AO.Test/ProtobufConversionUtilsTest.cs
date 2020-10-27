using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
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

			// TODO: WKB
		}

		[Test]
		public void CanConvertEnvelopeToFromShapeMsg()
		{
			IEnvelope env =
				GeometryFactory.CreateEnvelope(2600000.1234, 1200000.987654, 2601000.12,
				                               1201000.98);

			EnvelopeMsg envelopeMsg = ProtobufConversionUtils.ToEnvelopeMsg(env);

			IEnvelope rehydrated = ProtobufConversionUtils.FromEnvelopoeMsg(envelopeMsg);

			Assert.IsTrue(GeometryUtils.AreEqual(env, rehydrated));
		}
	}
}
