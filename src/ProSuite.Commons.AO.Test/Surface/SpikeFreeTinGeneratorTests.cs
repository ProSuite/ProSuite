using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Shared.ArcGIS;

namespace ProSuite.Commons.AO.Test.Surface
{
	[TestFixture]
	public class SpikeFreeTinGeneratorTests
	{
		private static ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void Setup()
		{
			ArcGISLicenseUtils.EnsureArcGISLicense(true);

			_spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);
		}

		[Test]
		public void SpikesAreFilteredOut()
		{
			var spike = GeometryFactory.CreatePoint(50, 50, 10);

			var points = new List<IPoint>
			             {
				             GeometryFactory.CreatePoint(49, 49, 100),
				             GeometryFactory.CreatePoint(50, 51, 99),
				             GeometryFactory.CreatePoint(51, 49, 98),
				             spike
			             };

			var tinGenerator = CreateSpikeFreeTineGeneratorForPoints(points, 10, 10);

			var envelope =
				GeometryFactory.CreateEnvelope(0, 0, 100, 100, 0, 100, _spatialReference);
			var tin = tinGenerator.GenerateTin(envelope);
			Assert.AreEqual(points.Count - 1, tin.DataNodeCount);
			Assert.Greater(tin.Extent.ZMin, spike.Z);
		}

		[Test]
		public void MultipleSpikesAreFilteredOut()
		{
			var spike = GeometryFactory.CreatePoint(50, 50, 10);
			var spike2 = GeometryFactory.CreatePoint(50, 50, 20);

			var points = new List<IPoint>
			             {
				             GeometryFactory.CreatePoint(49, 49, 100),
				             GeometryFactory.CreatePoint(50, 51, 99),
				             GeometryFactory.CreatePoint(51, 49, 98),
				             spike,
				             spike2
			             };

			var tinGenerator = CreateSpikeFreeTineGeneratorForPoints(points, 10, 10);

			var envelope =
				GeometryFactory.CreateEnvelope(0, 0, 100, 100, 0, 100, _spatialReference);
			var tin = tinGenerator.GenerateTin(envelope);
			Assert.AreEqual(points.Count - 2, tin.DataNodeCount);
			Assert.Greater(tin.Extent.ZMin, spike.Z);
			Assert.Greater(tin.Extent.ZMin, spike2.Z);
		}

		[Test]
		public void PointsAreOrderedByZValueBeforeAdding()
		{
			var spike = GeometryFactory.CreatePoint(50, 50, 10);

			var points = new List<IPoint>
			             {
				             GeometryFactory.CreatePoint(49, 49, 100),
				             spike,
				             GeometryFactory.CreatePoint(50, 51, 99),
				             GeometryFactory.CreatePoint(51, 49, 98)
			             };

			var tinGenerator = CreateSpikeFreeTineGeneratorForPoints(points, 10, 10);

			var envelope =
				GeometryFactory.CreateEnvelope(0, 0, 100, 100, 0, 100, _spatialReference);
			var tin = tinGenerator.GenerateTin(envelope);
			Assert.AreEqual(points.Count - 1, tin.DataNodeCount);
			Assert.Greater(tin.Extent.ZMin, spike.Z);
		}

		[Test]
		public void SpikesAredAddedIfTriangleIsOverFreezingCap()
		{
			var spike = GeometryFactory.CreatePoint(50, 50, 10);

			var points = new List<IPoint>
			             {
				             GeometryFactory.CreatePoint(0, 0, 100),
				             GeometryFactory.CreatePoint(50, 100, 99),
				             GeometryFactory.CreatePoint(100, 0, 98),
				             spike
			             };

			var tinGenerator = CreateSpikeFreeTineGeneratorForPoints(points, 10, 10);

			var envelope =
				GeometryFactory.CreateEnvelope(0, 0, 100, 100, 0, 100, _spatialReference);
			var tin = tinGenerator.GenerateTin(envelope);
			Assert.AreEqual(tin.DataNodeCount, 4);
			Assert.AreEqual(tin.Extent.ZMin, spike.Z);
		}

		[Test]
		public void SpikesAreAddedIfZDistanceIsSmallerThanInsertionBuffer()
		{
			var spike = GeometryFactory.CreatePoint(50, 50, 97);

			var points = new List<IPoint>
			             {
				             GeometryFactory.CreatePoint(49, 49, 100),
				             GeometryFactory.CreatePoint(50, 51, 99),
				             GeometryFactory.CreatePoint(51, 49, 98),
				             spike
			             };

			var tinGenerator = CreateSpikeFreeTineGeneratorForPoints(points, 10, 10);

			var envelope =
				GeometryFactory.CreateEnvelope(0, 0, 100, 100, 0, 100, _spatialReference);
			var tin = tinGenerator.GenerateTin(envelope);
			Assert.AreEqual(tin.DataNodeCount, 4);
			Assert.AreEqual(tin.Extent.ZMin, spike.Z);
		}

		private static ITinGenerator CreateSpikeFreeTineGeneratorForPoints(
			IList<IPoint> points, double freezeDistance, double insertionBuffer)
		{
			const string idFieldName = "OID";
			foreach (IPoint point in points)
			{
				point.SpatialReference = _spatialReference;
			}

			var inMemoryWorkspace = (IFeatureWorkspace) WorkspaceUtils.OpenWorkspace(
				WorkspaceUtils.CreateInMemoryWorkspace("TransformationCheckServiceTest"));

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryMultipoint,
				                            _spatialReference,
				                            1000, hasZ: true),
				FieldUtils.CreateIntegerField(idFieldName));

			var featureClass =
				DatasetUtils.CreateSimpleFeatureClass(inMemoryWorkspace, "TEST", fields);

			IFeature feature = featureClass.CreateFeature();

			int idFieldIndex = featureClass.FindField(idFieldName);
			var multipoint = GeometryFactory.CreateMultipoint(points);
			feature.Shape = multipoint;
			feature.Value[idFieldIndex] = DBNull.Value;
			feature.Store();

			SimpleTerrainDataSource terrainDataSource = new SimpleTerrainDataSource(featureClass,
				esriTinSurfaceType.esriTinHardLine);

			SimpleTerrain simpleTerrain =
				new SimpleTerrain("TEST", new List<SimpleTerrainDataSource> { terrainDataSource },
				                  10, null);
			var tinGenerator =
				new SpikeFreeTinGenerator(simpleTerrain, freezeDistance, insertionBuffer, null, null);
			tinGenerator.AllowIncompleteInterpolationDomainAtBoundary = true;

			return tinGenerator;
		}
	}
}
