using System.Reflection;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Com;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class AreaPredictionCoverageTest
	{
		[OneTimeSetUp]
		public void SetupFixture() => TestUtils.InitializeLicense();

		[OneTimeTearDown]
		public void TeardownFixture() => TestUtils.ReleaseLicense();

		[TestCase(0, 5, false)]
		[TestCase(2, 7, false)]
		[TestCase(4, 9, true)]
		[TestCase(5, 10, true)]
		[TestCase(10, 15, false)]
		public void CountsCoveredAreaOnlyOnce(double secondMin, double secondMax,
		                                      bool expected)
		{
			IPolygon target = CreateRectangle(0, 10);
			IPolygon first = CreateRectangle(0, 5);
			IPolygon second = CreateRectangle(secondMin, secondMax);
			try
			{
				Assert.AreEqual(expected, IsCovered(
					target, 100, new IGeometry[] { first, second }, 0.9));
				Assert.AreEqual(expected, IsCovered(
					target, 100, new IGeometry[] { second, first }, 0.9));
				Assert.AreEqual(100, GeometryProperties.GetArea(target));
				Assert.AreEqual(50, GeometryProperties.GetArea(first));
			}
			finally
			{
				ComUtils.ReleaseComObject(target);
				ComUtils.ReleaseComObject(first);
				ComUtils.ReleaseComObject(second);
			}
		}

		[TestCase(0, true)]
		[TestCase(0.9, false)]
		public void HandlesNoCandidates(double threshold, bool expected)
		{
			IPolygon target = CreateRectangle(0, 10);
			try
			{
				Assert.AreEqual(expected, IsCovered(
					target, 100, new IGeometry[0], threshold));
			}
			finally
			{
				ComUtils.ReleaseComObject(target);
			}
		}

		private static bool IsCovered(IGeometry target, double area,
		                              IGeometry[] candidates, double threshold)
		{
			MethodInfo method = typeof(QaAreaPredictionMatch).Assembly
				.GetType("ProSuite.QA.Tests.AreaPredictionCoverage", true)
				.GetMethod("IsCovered", BindingFlags.Static | BindingFlags.Public);
			return (bool) method.Invoke(null, new object[] { target, area, candidates, threshold });
		}

		private static IPolygon CreateRectangle(double xmin, double xmax)
		{
			return GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(
				xmin, 0, xmax, 10,
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95)));
		}
	}
}
