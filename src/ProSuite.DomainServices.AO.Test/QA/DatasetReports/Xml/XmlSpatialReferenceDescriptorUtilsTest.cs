using System;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Xml;
using ProSuite.DomainServices.AO.QA.DatasetReports.Xml;

namespace ProSuite.DomainServices.AO.Test.QA.DatasetReports.Xml
{
	[TestFixture]
	public class XmlSpatialReferenceDescriptorUtilsTest
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
		public void CanCreateXmlSpatialReferenceDescriptor()
		{
			const bool defaultXyDomain = true;
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             defaultXyDomain);

			((ISpatialReferenceTolerance) spatialReference).XYTolerance = 1.0;
			((ISpatialReferenceTolerance) spatialReference).ZTolerance = 0.1;
			((ISpatialReferenceTolerance) spatialReference).MTolerance = 0.01;
			((ISpatialReferenceResolution) spatialReference).set_XYResolution(true, 0.001);
			((ISpatialReferenceResolution) spatialReference).set_ZResolution(true, 0.0001);
			((ISpatialReferenceResolution) spatialReference).MResolution = 0.00001;

			XmlSpatialReferenceDescriptor xmlSpatialReference =
				XmlSpatialReferenceDescriptorUtils.CreateXmlSpatialReferenceDescriptor(
					spatialReference);

			Console.WriteLine(XmlUtils.Serialize(xmlSpatialReference));

			Assert.AreEqual("1.0", xmlSpatialReference.XyToleranceFormatted);
			Assert.AreEqual("0.1", xmlSpatialReference.ZToleranceFormatted);
			Assert.AreEqual("0.01", xmlSpatialReference.MToleranceFormatted);
			Assert.AreEqual("0.001", xmlSpatialReference.XyResolutionFormatted);
			Assert.AreEqual("0.0001", xmlSpatialReference.ZResolutionFormatted);
			Assert.AreEqual("0.00001", xmlSpatialReference.MResolutionFormatted);

			Assert.AreEqual(1, xmlSpatialReference.XyTolerance);
			Assert.AreEqual(0.1, xmlSpatialReference.ZTolerance);
			Assert.AreEqual(0.01, xmlSpatialReference.MTolerance);
			Assert.AreEqual(0.001, xmlSpatialReference.XyResolution);
			Assert.AreEqual(0.0001, xmlSpatialReference.ZResolution);
			Assert.AreEqual(0.00001, xmlSpatialReference.MResolution);
		}
	}
}
