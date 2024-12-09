using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Test.Geodatabase.GdbSchema
{
	[TestFixture]
	public class GdbRowTest
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
		public void CanCreateFeatureAndComReleaseShape()
		{
			GdbFeatureClass schema = CreateSchema();

			IGeometry shape =
				GeometryFactory.CreatePoint(2600000, 1200000,
				                            DatasetUtils.GetSpatialReference(schema));

			// Now the RCW reference count of shape is 1

			int oid = 0;
			GdbFeature propSetBackedFeature =
				GdbFeature.Create(++oid, schema, new PropertySetValueList());

			propSetBackedFeature.Shape = shape;

			IGeometry retrievedShape = propSetBackedFeature.Shape;

			// Now the RCW reference count of shape should be 2
			int remainingRefCount = Marshal.ReleaseComObject(retrievedShape);

			Assert.AreEqual(1, remainingRefCount);

			// The same with the faster ValueList:
			GdbFeature valueListBackedFeature =
				GdbFeature.Create(++oid, schema, new ValueList(schema.Fields.FieldCount));

			valueListBackedFeature.Shape = shape;

			retrievedShape = valueListBackedFeature.Shape;

			// Now the RCW reference count of shape should be 2
			remainingRefCount = Marshal.ReleaseComObject(retrievedShape);

			Assert.AreEqual(1, remainingRefCount);
		}

		[Test]
		[Category(Commons.Test.TestCategory.Performance)]
		public void CreateFeaturePerformance()
		{
			// NOTE regarding PropertySetValueList:
			// Putting the shape into a NEW propertySet each time eats half of the performance
			// The other half is probably the instantiation of the property set.
			GdbFeatureClass schema = CreateSchema();

			IGeometry shape =
				GeometryFactory.CreatePoint(2600000, 1200000,
				                            DatasetUtils.GetSpatialReference(schema));

			// Now the RCW reference count of shape is 1

			Stopwatch watch = Stopwatch.StartNew();

			const int count = 10000;

			for (int i = 0; i < count; i++)
			{
				GdbFeature feature = CreateGdbFeature(i, schema, shape, new PropertySetValueList());

				Assert.NotNull(feature);
				Assert.AreEqual(i, feature.OID);
			}

			watch.Stop();

			Console.WriteLine($"PropertySet-backed: {watch.ElapsedMilliseconds}ms");

			watch.Restart();

			int fieldCount = schema.Fields.FieldCount;

			for (int i = 0; i < count; i++)
			{
				GdbFeature feature = CreateGdbFeature(i, schema, shape, new ValueList(fieldCount));

				Assert.NotNull(feature);
				Assert.AreEqual(i, feature.OID);
			}

			watch.Stop();

			Assert.Less(watch.ElapsedMilliseconds, 40);

			Console.WriteLine($"ValueList-backed: {watch.ElapsedMilliseconds}ms");
		}

		[Test]
		[Category(Commons.Test.TestCategory.Performance)]
		public void CreateFeatureAndGetShapePerformance()
		{
			// This unit test shows that getting the object from the property set is
			// the same and pretty fast.
			GdbFeatureClass schema = CreateSchema();

			IGeometry shape =
				GeometryFactory.CreatePoint(2600000, 1200000,
				                            DatasetUtils.GetSpatialReference(schema));

			// Now the RCW reference count of shape is 1

			Stopwatch watch = Stopwatch.StartNew();

			const int count = 10000;

			for (int i = 0; i < count; i++)
			{
				watch.Stop();
				GdbFeature feature = CreateGdbFeature(i, schema, shape, new PropertySetValueList());

				Assert.NotNull(feature);

				watch.Start();
				Assert.NotNull(feature.Shape);
			}

			watch.Stop();

			Assert.Less(watch.ElapsedMilliseconds, 40);

			Console.WriteLine($"PropertySet-backed: {watch.ElapsedMilliseconds}ms");

			watch.Restart();

			int fieldCount = schema.Fields.FieldCount;

			for (int i = 0; i < count; i++)
			{
				watch.Stop();
				GdbFeature feature = CreateGdbFeature(i, schema, shape, new ValueList(fieldCount));

				Assert.NotNull(feature);

				watch.Start();

				Assert.NotNull(feature.Shape);
			}

			watch.Stop();

			Assert.Less(watch.ElapsedMilliseconds, 40);

			Console.WriteLine($"ValueList-backed: {watch.ElapsedMilliseconds}ms");
		}

		[Test]
		[Category(Commons.Test.TestCategory.Performance)]
		public void CanRecycleRow()
		{
			GdbFeatureClass schema = CreateSchema();

			IGeometry shape =
				GeometryFactory.CreatePoint(2600000, 1200000,
				                            DatasetUtils.GetSpatialReference(schema));

			Stopwatch watch = Stopwatch.StartNew();

			const int count = 10000;

			GdbFeature feature = CreateGdbFeature(0, schema, shape, new PropertySetValueList());

			for (int i = 1; i < count; i++)
			{
				feature.Recycle(i);

				SetValues(feature, shape);

				Assert.NotNull(feature);
				Assert.AreEqual(i, feature.OID);
				Assert.NotNull(feature.Shape);
			}

			watch.Stop();

			// This is often just below 100, give it some slack:
			Assert.Less(watch.ElapsedMilliseconds, 150);

			Console.WriteLine($"PropertySet-backed: {watch.ElapsedMilliseconds}ms");

			watch.Restart();

			int fieldCount = schema.Fields.FieldCount;
			feature = CreateGdbFeature(0, schema, shape, new ValueList(fieldCount));

			for (int i = 1; i < count; i++)
			{
				feature.Recycle(i);

				SetValues(feature, shape);

				Assert.NotNull(feature);
				Assert.AreEqual(i, feature.OID);
				Assert.NotNull(feature.Shape);
			}

			watch.Stop();

			Assert.Less(watch.ElapsedMilliseconds, 100);

			Console.WriteLine($"ValueList-backed: {watch.ElapsedMilliseconds}ms");
		}

		private static GdbFeature CreateGdbFeature(int oid,
		                                           GdbFeatureClass schema,
		                                           IGeometry shape,
		                                           IValueList valueListImpl)
		{
			GdbFeature feature =
				GdbFeature.Create(oid, schema, valueListImpl);

			SetValues(feature, shape);

			return feature;
		}

		private static void SetValues(GdbFeature feature, IGeometry shape)
		{
			feature.Shape = shape;
			feature.set_Value(2, DateTime.Now);
			feature.set_Value(3, 3.14159);
			feature.set_Value(4, 42);
			feature.set_Value(5, "bla");
		}

		private static GdbFeatureClass CreateSchema()
		{
			GdbFeatureClass gdbFeatureClass =
				new GdbFeatureClass(41, "TESTABLE", esriGeometryType.esriGeometryPoint,
				                    "Test table");

			IFeatureClass featureClass = gdbFeatureClass;

			// Add OID field
			gdbFeatureClass.AddField(FieldUtils.CreateOIDField());

			// Add Shape field
			gdbFeatureClass.AddField(
				FieldUtils.CreateShapeField(
					esriGeometryType.esriGeometryPoint,
					SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95)));

			gdbFeatureClass.AddField(FieldUtils.CreateDateField("DATE"));
			gdbFeatureClass.AddField(FieldUtils.CreateDoubleField("DOUBLE"));
			gdbFeatureClass.AddField(FieldUtils.CreateIntegerField("INT"));
			gdbFeatureClass.AddField(FieldUtils.CreateTextField("TEXT", 244, "Text"));

			return gdbFeatureClass;
		}
	}
}
