using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class DatasetUtilsTest
{
	private ArcGIS.Core.Data.Geodatabase _testGdb;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		_testGdb = CreateTestGdb();
	}

	[Test]
	public void Subtypes_Tests()
	{
		//Test an FC with Subtypes
		var fc = _testGdb.OpenDataset<FeatureClass>("fc");
		Assert.NotNull(fc);

		var defaultSubtypeCode = DatasetUtils.GetDefaultSubtypeCode(fc);
		Assert.AreEqual(3, defaultSubtypeCode);

		var defaultSubtype = DatasetUtils.GetDefaultSubtype(fc);
		Assert.NotNull(defaultSubtype);
		Assert.AreEqual("Three", defaultSubtype.GetName());

		var subtype = DatasetUtils.GetSubtype(fc, 2);
		Assert.NotNull(subtype);
		Assert.AreEqual("Two", subtype.GetName());

		var missingSubtype = DatasetUtils.GetSubtype(fc, 999);
		Assert.Null(missingSubtype);

		var subtypeFieldName = DatasetUtils.GetSubtypeFieldName(fc);
		Assert.NotNull(subtypeFieldName);
		Assert.AreEqual("SomeInteger", subtypeFieldName);

		var subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(fc);
		Assert.AreEqual(fc.GetDefinition().FindField(subtypeFieldName), subtypeFieldIndex);

		// Test a Table without Subtypes
		var table = _testGdb.OpenDataset<Table>("tbl");
		Assert.NotNull(table);
		Assert.AreEqual(-1, DatasetUtils.GetDefaultSubtypeCode(table));
		Assert.Null(DatasetUtils.GetDefaultSubtype(table));
		Assert.Null(DatasetUtils.GetSubtype(table, 2));
		Assert.Null(DatasetUtils.GetSubtypeFieldName(table));
		Assert.AreEqual(-1, DatasetUtils.GetSubtypeFieldIndex(table));
	}

	private static ArcGIS.Core.Data.Geodatabase CreateTestGdb()
	{
		var gdb = SchemaBuilder.CreateGeodatabase(new MemoryConnectionProperties());

		SchemaBuilder schemaBuilder = new SchemaBuilder(gdb);

		FeatureClassDescription featureClassDescription = new FeatureClassDescription(
			"fc", new List<FieldDescription>
			      {
				      new FieldDescription("SomeInteger", FieldType.Integer),
				      new FieldDescription("SomeString", FieldType.String)
			      },
			new ShapeDescription(GeometryType.Point, SpatialReferences.WGS84));

		featureClassDescription.SubtypeFieldDescription =
			new SubtypeFieldDescription(
				"SomeInteger",
				new Dictionary<int, string>
				{
					{ 1, "One" },
					{ 2, "Two" },
					{ 3, "Three" }
				})
			{ DefaultSubtypeCode = 3 };

		schemaBuilder.Create(featureClassDescription);

		TableDescription tableDescription = new TableDescription(
			"tbl", new List<FieldDescription>
			       {
				       new FieldDescription("SomeInteger", FieldType.Integer),
				       new FieldDescription("SomeString", FieldType.String)
			       });

		schemaBuilder.Create(tableDescription);

		if (! schemaBuilder.Build())
		{
			IReadOnlyList<string> errorMessages = schemaBuilder.ErrorMessages;
			//etc.
		}

		return gdb;
	}
}
