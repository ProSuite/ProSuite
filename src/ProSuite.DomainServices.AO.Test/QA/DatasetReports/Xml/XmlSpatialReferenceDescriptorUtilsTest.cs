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

#if NET6_0_OR_GREATER
			// Extra digits were added to improve 'round-trippability', i.e. parsing the formatted string
			// returning to the same number.
			// See https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/

			// See ProSuite.Commons.Test.StringUtilsTest.cs

			Assert.True(xmlSpatialReference.XyToleranceFormatted.StartsWith("1.0"));
			Assert.True(xmlSpatialReference.ZToleranceFormatted.StartsWith("0.1"));
			Assert.True(xmlSpatialReference.MToleranceFormatted.StartsWith("0.01"));
			Assert.True(xmlSpatialReference.XyResolutionFormatted.StartsWith("0.001"));
			Assert.True(xmlSpatialReference.ZResolutionFormatted.StartsWith("0.0001"));
			Assert.True(xmlSpatialReference.MResolutionFormatted.StartsWith("0.00001"));
#else
			Assert.AreEqual("1.0", xmlSpatialReference.XyToleranceFormatted);
			Assert.AreEqual("0.1", xmlSpatialReference.ZToleranceFormatted);
			Assert.AreEqual("0.01", xmlSpatialReference.MToleranceFormatted);
			Assert.AreEqual("0.001", xmlSpatialReference.XyResolutionFormatted);
			Assert.AreEqual("0.0001", xmlSpatialReference.ZResolutionFormatted);
			Assert.AreEqual("0.00001", xmlSpatialReference.MResolutionFormatted);
#endif

			Assert.AreEqual(1, xmlSpatialReference.XyTolerance);
			Assert.AreEqual(0.1, xmlSpatialReference.ZTolerance);
			Assert.AreEqual(0.01, xmlSpatialReference.MTolerance);
			Assert.AreEqual(0.001, xmlSpatialReference.XyResolution);
			Assert.AreEqual(0.0001, xmlSpatialReference.ZResolution);
			Assert.AreEqual(0.00001, xmlSpatialReference.MResolution);
		}
	}
}
