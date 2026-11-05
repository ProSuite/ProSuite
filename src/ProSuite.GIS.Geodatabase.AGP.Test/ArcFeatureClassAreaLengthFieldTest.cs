using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	/// <summary>
	/// The area and length fields must be found, because RelationshipClassJoinUtils leaves them
	/// out of the subfields of a relationship class join. If they are not found, Oracle fails
	/// with ORA-00904 ("SHAPE"."LEN": invalid identifier) when the join is read.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ArcFeatureClassAreaLengthFieldTest
	{
		private ArcGIS.Core.Data.Geodatabase _gdb;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();

			string geodatabasePath = Path.Combine(
				Path.GetTempPath(), $"ArcFeatureClassAreaLengthFieldTest_{Guid.NewGuid():N}.gdb");

			_gdb = SchemaBuilder.CreateGeodatabase(
				new FileGeodatabaseConnectionPath(new Uri(geodatabasePath)));
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_gdb?.Dispose();
		}

		/// <summary>
		/// In a file geodatabase, the definition returns the area and length field names in
		/// the same letter case as the fields. The case of GOTOP-469 (e.g. 'Shape.AREA' for the
		/// field 'SHAPE.AREA', seen on an Oracle geodatabase) cannot be created here, therefore
		/// the test <see cref="FindField_finds_area_and_length_field_in_other_letter_case"/>
		/// checks the field lookup with names in another letter case.
		/// </summary>
		[Test]
		public void Learning_test_file_geodatabase_returns_area_and_length_field_in_field_case()
		{
			using FeatureClass featureClass = CreatePolygonFeatureClass("LEARN_POLYGON");
			using FeatureClassDefinition definition = featureClass.GetDefinition();

			List<string> fieldNames = definition.GetFields().Select(f => f.Name).ToList();

			Assert.Contains(definition.GetAreaField(), fieldNames);
			Assert.Contains(definition.GetLengthField(), fieldNames);
		}

		[Test]
		public void AreaField_and_LengthField_return_the_fields(
			[Values(false, true)] bool cachePropertiesEagerly)
		{
			using FeatureClass featureClass =
				CreatePolygonFeatureClass($"POLYGON_CACHED_{cachePropertiesEagerly}");
			using FeatureClassDefinition definition = featureClass.GetDefinition();

			var arcFeatureClass = new ArcFeatureClass(featureClass, cachePropertiesEagerly);

			IField areaField = arcFeatureClass.AreaField;
			IField lengthField = arcFeatureClass.LengthField;

			Assert.NotNull(areaField, "Area field not found");
			Assert.NotNull(lengthField, "Length field not found");
			Assert.AreEqual(definition.GetAreaField(), areaField.Name);
			Assert.AreEqual(definition.GetLengthField(), lengthField.Name);
		}

		/// <summary>
		/// ArcFeatureClass.AreaField and LengthField look up the names returned by the
		/// definition with this method, so they also find the fields if the definition returns
		/// the names in another letter case (GOTOP-469).
		/// </summary>
		[Test]
		public void FindField_finds_area_and_length_field_in_other_letter_case()
		{
			using FeatureClass featureClass = CreatePolygonFeatureClass("POLYGON_CASE");
			using FeatureClassDefinition definition = featureClass.GetDefinition();

			var arcFeatureClass = new ArcFeatureClass(featureClass);

			string areaFieldName = definition.GetAreaField();
			string lengthFieldName = definition.GetLengthField();

			AssertFieldFound(arcFeatureClass, areaFieldName.ToUpperInvariant(), areaFieldName);
			AssertFieldFound(arcFeatureClass, areaFieldName.ToLowerInvariant(), areaFieldName);
			AssertFieldFound(arcFeatureClass, lengthFieldName.ToUpperInvariant(), lengthFieldName);
			AssertFieldFound(arcFeatureClass, lengthFieldName.ToLowerInvariant(), lengthFieldName);
		}

		private static void AssertFieldFound(ArcFeatureClass featureClass,
		                                     string searchedName,
		                                     string expectedFieldName)
		{
			int fieldIndex = featureClass.Fields.FindField(searchedName);

			Assert.GreaterOrEqual(fieldIndex, 0, $"Field {searchedName} not found");
			Assert.AreEqual(expectedFieldName, featureClass.Fields.get_Field(fieldIndex).Name);
		}

		private FeatureClass CreatePolygonFeatureClass(string name)
		{
			return DatasetUtils.CreateFeatureClass(
				_gdb, name,
				new List<FieldDescription> { new FieldDescription("NAME", FieldType.String) },
				GeometryType.Polygon, SpatialReferences.WGS84, hasZ: false);
		}
	}
}
