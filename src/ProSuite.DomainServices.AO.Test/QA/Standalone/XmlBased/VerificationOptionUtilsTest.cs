using System;
using ESRI.ArcGIS.esriSystem;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	[TestFixture]
	public class VerificationOptionUtilsTest
	{
		[Test]
		public void CanParseValidDocumentVersions()
		{
			Expect(esriArcGISVersion.esriArcGISVersion83, "8.3");
			Expect(esriArcGISVersion.esriArcGISVersion90, "9");
			Expect(esriArcGISVersion.esriArcGISVersion90, "9.0");
			Expect(esriArcGISVersion.esriArcGISVersion90, "9.1"); // 9.1 -> 9.0
			Expect(esriArcGISVersion.esriArcGISVersion92, "9.2");
			Expect(esriArcGISVersion.esriArcGISVersion93, "9.3");

			Expect(esriArcGISVersion.esriArcGISVersion10, "10");
			Expect(esriArcGISVersion.esriArcGISVersion10, "10.0");
			Expect(esriArcGISVersion.esriArcGISVersion10, "10.00");

			Expect("esriArcGISVersion101", "10.1");
			Expect("esriArcGISVersion101", "10.2"); // 10.2 -> 10.1
			Expect("esriArcGISVersion103", "10.3");
			Expect("esriArcGISVersion104", "10.4");

			Expect("esriArcGISVersion104", " 10.4 ");
			Expect("esriArcGISVersion104", " 0010.400 ");

			Expect(esriArcGISVersion.esriArcGISVersionCurrent, null);
			Expect(esriArcGISVersion.esriArcGISVersionCurrent, string.Empty);
			Expect(esriArcGISVersion.esriArcGISVersionCurrent, " ");
			Expect(esriArcGISVersion.esriArcGISVersionCurrent, "current");
		}

		[Test]
		public void CantParseInvalidDocumentVersions()
		{
			Expect<ArgumentException>("7");
			Expect<ArgumentException>("9.4");
			Expect<ArgumentException>("11.2");
			Expect<InvalidConfigurationException>("A");
			Expect<InvalidConfigurationException>("10.3.1");
			Expect<InvalidConfigurationException>("10.*");
		}

		private static void Expect<T>([NotNull] string version) where T : Exception
		{
			Assert.Throws<T>(() => VerificationOptionUtils.ParseDocumentVersion(version));
		}

		private static void Expect(esriArcGISVersion expectedVersion,
		                           [CanBeNull] string version)
		{
			Assert.AreEqual(expectedVersion,
			                VerificationOptionUtils.ParseDocumentVersion(version));
		}

		private static void Expect([NotNull] string expectedArcGISVersion,
		                           [CanBeNull] string version)
		{
			Assert.AreEqual(expectedArcGISVersion,
			                VerificationOptionUtils.ParseDocumentVersion(version).ToString());
		}
	}
}
