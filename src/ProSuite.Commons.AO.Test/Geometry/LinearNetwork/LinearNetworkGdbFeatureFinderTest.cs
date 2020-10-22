using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.LinearNetwork;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.IO;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Test.Geometry.LinearNetwork
{
	[TestFixture]
	public class LinearNetworkGdbFeatureFinderTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		#region Setup/Teardown

		[SetUp]
		public void SetUp() { }

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

		#endregion

		[Test]
		public void CanFindJunctionAtEdgeEnd()
		{
			const string fgdbName = "NetworkJunctionFinderTest.gdb";

			// Create FGDB with test features:
			IFeatureClass edgeClass, junctionClass;
			LinearNetworkGdbFeatureFinder featureFinder =
				CreateTestGdbSchema(fgdbName, out edgeClass, out junctionClass);

			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPoint fromPoint1 = GeometryFactory.CreatePoint(2600000, 1200000, lv95);
			IPoint toPoint1 = GeometryFactory.CreatePoint(2600020, 1200010, lv95);

			CreateEdge(edgeClass, fromPoint1, toPoint1);

			Assert.AreEqual(0, featureFinder.FindJunctionFeaturesAt(fromPoint1).Count);
			Assert.AreEqual(0, featureFinder.FindJunctionFeaturesAt(toPoint1).Count);

			IFeature from1Junction = CreateJunction(junctionClass, fromPoint1);

			IList<IFeature> found = featureFinder.FindJunctionFeaturesAt(fromPoint1);
			Assert.AreEqual(1, found.Count);
			Assert.AreEqual(from1Junction.OID, found[0].OID);

			Assert.AreEqual(0, featureFinder.FindJunctionFeaturesAt(toPoint1).Count);

			// Duplicate junction (different Z)
			fromPoint1.Z = 123;
			IFeature fromJunction2 = CreateJunction(junctionClass, fromPoint1);
			found = featureFinder.FindJunctionFeaturesAt(fromPoint1);
			Assert.AreEqual(2, found.Count);
			Assert.AreNotEqual(found[0].OID, found[1].OID);

			Assert.IsTrue(found.Any(
				              j => GdbObjectUtils.IsSameObject(
					              j, fromJunction2, ObjectClassEquality.SameInstance)));

			Assert.IsTrue(found[0].Class == junctionClass && found[1].Class == junctionClass);

			// Stand-alone junction
			IPoint disjointPoint = GeometryFactory.CreatePoint(2600025, 1200011, lv95);
			found = featureFinder.FindJunctionFeaturesAt(disjointPoint);
			Assert.AreEqual(0, found.Count);

			IFeature standalone = CreateJunction(junctionClass, disjointPoint);
			found = featureFinder.FindJunctionFeaturesAt(disjointPoint);
			Assert.AreEqual(1, found.Count);
			Assert.IsTrue(GdbObjectUtils.IsSameObject(found[0], standalone,
			                                          ObjectClassEquality.SameInstance));
		}

		[Test]
		public void CanFindEdges()
		{
			const string fgdbName = "NetworkEdgeFinderTest.gdb";

			// Create FGDB with test features:
			IFeatureClass edgeClass, junctionClass;
			LinearNetworkGdbFeatureFinder featureFinder =
				CreateTestGdbSchema(fgdbName, out edgeClass, out junctionClass);

			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPoint fromPoint1 = GeometryFactory.CreatePoint(2600000, 1200000, lv95);
			IPoint toPoint1 = GeometryFactory.CreatePoint(2600020, 1200010, lv95);

			IFeature edge1 = CreateEdge(edgeClass, fromPoint1, toPoint1);

			Assert.AreEqual(
				0, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.Both).Count);

			IPoint toPoint2 = GeometryFactory.CreatePoint(2600025, 1200000, lv95);
			IFeature edge2 = CreateEdge(edgeClass, toPoint1, toPoint2);

			Assert.AreEqual(
				0, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.From).Count);
			Assert.AreEqual(
				1, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.To).Count);
			Assert.AreEqual(
				1, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.Both).Count);

			// Junction makes no difference:
			CreateJunction(junctionClass, toPoint1);
			Assert.AreEqual(
				0, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.From).Count);
			Assert.AreEqual(
				1, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.To).Count);
			Assert.AreEqual(
				1, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.Both).Count);

			IPoint toPoint3 = GeometryFactory.CreatePoint(2600035, 1200020, lv95);
			IFeature edge3 = CreateEdge(edgeClass, toPoint2, toPoint3);

			IList<IFeature> found =
				featureFinder.GetConnectedEdgeFeatures(edge2, null, LineEnd.From);
			Assert.AreEqual(1, found.Count);
			Assert.IsTrue(
				GdbObjectUtils.IsSameObject(edge1, found[0], ObjectClassEquality.SameInstance));

			found = featureFinder.GetConnectedEdgeFeatures(edge2, null, LineEnd.To);
			Assert.AreEqual(1, found.Count);
			Assert.IsTrue(
				GdbObjectUtils.IsSameObject(edge3, found[0], ObjectClassEquality.SameInstance));

			found = featureFinder.GetConnectedEdgeFeatures(edge2, null, LineEnd.Both);
			Assert.AreEqual(2, found.Count);

			// Now in the cache:
			IEnvelope cacheEnv =
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2600100, 1200100, lv95);
			featureFinder.CacheTargetFeatureCandidates(cacheEnv);

			// NOTE: Once the TargetFeatures cache is not null, the DB is not searched any more (usage in service!)
			Assert.AreEqual(
				0, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.From).Count);
			Assert.AreEqual(
				1, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.To).Count);
			Assert.AreEqual(
				1, featureFinder.GetConnectedEdgeFeatures(edge1, null, LineEnd.Both).Count);

			found =
				featureFinder.GetConnectedEdgeFeatures(edge2, null, LineEnd.From);
			Assert.AreEqual(1, found.Count);
			Assert.IsTrue(
				GdbObjectUtils.IsSameObject(edge1, found[0], ObjectClassEquality.SameInstance));

			found = featureFinder.GetConnectedEdgeFeatures(edge2, null, LineEnd.To);
			Assert.AreEqual(1, found.Count);
			Assert.IsTrue(
				GdbObjectUtils.IsSameObject(edge3, found[0], ObjectClassEquality.SameInstance));

			found = featureFinder.GetConnectedEdgeFeatures(edge2, null, LineEnd.Both);
			Assert.AreEqual(2, found.Count);
		}

		private static LinearNetworkGdbFeatureFinder CreateTestGdbSchema(
			string fgdbName,
			out IFeatureClass edgeClass, out IFeatureClass junctionClass)
		{
			const string unitTestDir = @"C:\Temp\UnitTestData";

			try
			{
				FileSystemUtils.DeleteDirectory(Path.Combine(unitTestDir, fgdbName), true,
				                                true);
			}
			catch (Exception)
			{
				// ignored
			}

			IWorkspaceName workspaceName =
				WorkspaceUtils.CreateFileGdbWorkspace(unitTestDir,
				                                      fgdbName);

			IWorkspace workspace = WorkspaceUtils.OpenWorkspace(workspaceName);

			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IFieldsEdit fieldsReference = new FieldsClass();
			fieldsReference.AddField(FieldUtils.CreateOIDField());
			fieldsReference.AddField(
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, lv95));

			edgeClass = DatasetUtils.CreateSimpleFeatureClass(
				(IFeatureWorkspace) workspace, "Edges", string.Empty,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, lv95));

			junctionClass = DatasetUtils.CreateSimpleFeatureClass(
				(IFeatureWorkspace) workspace, "Junctions", string.Empty,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPoint, lv95));

			List<LinearNetworkClassDef> networkClassDefinitions =
				new List<LinearNetworkClassDef>
				{
					new LinearNetworkClassDef(edgeClass),
					new LinearNetworkClassDef(junctionClass)
				};

			LinearNetworkGdbFeatureFinder featureFinder =
				new LinearNetworkGdbFeatureFinder(networkClassDefinitions);
			return featureFinder;
		}

		private static IFeature CreateJunction(IFeatureClass junctionClass, IPoint fromPoint)
		{
			IFeature from1Junction = junctionClass.CreateFeature();
			from1Junction.Shape = fromPoint;
			from1Junction.Store();
			return from1Junction;
		}

		private static IFeature CreateEdge(IFeatureClass edgeClass, IPoint fromPoint,
		                                   IPoint toPoint)
		{
			IFeature edge1 = edgeClass.CreateFeature();
			edge1.Shape = GeometryFactory.CreatePolyline(fromPoint, toPoint);
			edge1.Store();
			return edge1;
		}
	}
}
