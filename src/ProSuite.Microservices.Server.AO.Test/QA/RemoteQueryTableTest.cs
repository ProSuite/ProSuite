using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.QA;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.Microservices.Server.AO.Test.QA
{
	[TestFixture]
	public class RemoteQueryTableTest
	{
		private const string _relationshipClassQualifiedField =
			"TOPGIS_GC.GC_BEDR_GEOL_MAPPING_UNIT_ATT.GEOL_MAPPING_UNIT_ATT_UUID";

		private const string _actualField =
			"TOPGIS_GC.GC_BEDROCK.GEOL_MAPPING_UNIT_ATT_UUID";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void FindField_finds_field_by_its_qualified_name([Values] bool hasShape)
		{
			GdbTable join = CreateJoinFromClientSchema(hasShape);

			Assert.AreEqual(join.Fields.FindField(_actualField), join.FindField(_actualField));
			Assert.GreaterOrEqual(join.FindField(_actualField), 0);
		}

		[Test]
		public void FindField_finds_unique_field_qualified_with_another_name(
			[Values] bool hasShape)
		{
			GdbTable join = CreateJoinFromClientSchema(hasShape);

			int fieldIndex = join.FindField(_relationshipClassQualifiedField);

			Assert.GreaterOrEqual(fieldIndex, 0, "Field not found");
			Assert.AreEqual(_actualField, join.Fields.Field[fieldIndex].Name);
		}

		[Test]
		public void FindField_does_not_find_field_name_that_exists_in_both_tables(
			[Values] bool hasShape)
		{
			GdbTable join = CreateJoinFromClientSchema(hasShape);

			Assert.AreEqual(-1, join.FindField("OBJECTID"));
			Assert.AreEqual(-1, join.FindField("TOPGIS_GC.GC_BEDR_GEOL_MAPPING_UNIT_ATT.OBJECTID"));
		}

		[Test]
		public void FindField_does_not_find_missing_field([Values] bool hasShape)
		{
			GdbTable join = CreateJoinFromClientSchema(hasShape);

			Assert.AreEqual(-1, join.FindField("TOPGIS_GC.GC_BEDROCK.NO_SUCH_FIELD"));
		}

		/// <summary>
		/// GC_0103 (GeoCover) qualifies the field with GC_BEDR_GEOL_MAPPING_UNIT_ATT. This is a
		/// one-to-many relationship class without a table. The server's own join finds the field
		/// in GC_BEDROCK. The join built from the client's schema must find the same field.
		/// Before, QaConstraint failed with "Cannot find column
		/// [TOPGIS_GC.GC_BEDR_GEOL_MAPPING_UNIT_ATT.GEOL_MAPPING_UNIT_ATT_UUID]".
		/// See the TODO in TableBasedUtils.FindUniqueFieldInJoin.
		/// </summary>
		[Test]
		public void Constraint_with_unknown_table_name_finds_unique_field_as_on_server(
			[Values] bool hasShape)
		{
			GdbTable join = CreateJoinFromClientSchema(hasShape);

			Assert.DoesNotThrow(() => TableViewFactory.Create(
				                    join, $"{_relationshipClassQualifiedField} IS NOT NULL",
				                    useAsConstraint: true));
		}

		[Test]
		public void GetInvolvedRows_reads_object_ids_of_both_tables(
			[Values] bool hasShape,
			[Values("TOPGIS_GC.", "")] string ownerPrefixOfBaseTables)
		{
			var baseTables = new List<IReadOnlyTable>
			                 {
				                 CreateBaseTable($"{ownerPrefixOfBaseTables}GC_BEDROCK"),
				                 CreateBaseTable(
					                 $"{ownerPrefixOfBaseTables}GC_GEOL_MAPPING_UNIT_ATT")
			                 };

			GdbTable join = CreateJoinFromClientSchema(hasShape, baseTables);

			GdbRow joinedRow = join.CreateObject(11);
			joinedRow.set_Value(
				join.Fields.FindField("TOPGIS_GC.GC_GEOL_MAPPING_UNIT_ATT.OBJECTID"), 22);

			List<string> involvedRows =
				((ITableBased) join).GetInvolvedRows(joinedRow)
				                    .Cast<InvolvedRow>()
				                    .Select(r => $"{r.TableName}:{r.OID}")
				                    .ToList();

			CollectionAssert.AreEqual(
				new[]
				{
					$"{ownerPrefixOfBaseTables}GC_BEDROCK:11",
					$"{ownerPrefixOfBaseTables}GC_GEOL_MAPPING_UNIT_ATT:22"
				},
				involvedRows);
		}

		private static GdbTable CreateBaseTable(string name)
		{
			var result = new GdbTable(null, name, null, null, new WorkspaceMock());
			result.AddField(FieldUtils.CreateOIDField());

			return result;
		}

		/// <summary>
		/// Creates the joined table that the server builds from the schema that the client
		/// sends. The fields are the fields of a left join of TOPGIS_GC.GC_BEDROCK with
		/// TOPGIS_GC.GC_GEOL_MAPPING_UNIT_ATT through the relationship class
		/// TOPGIS_GC.GC_BEDR_GEOL_MAPPING_UNIT_ATT. The fields of GC_BEDROCK come first.
		/// </summary>
		private static GdbTable CreateJoinFromClientSchema(
			bool hasShape, IList<IReadOnlyTable> baseTables = null)
		{
			var objectClassMsg = new ObjectClassMsg
			                     {
				                     ClassHandle = 789,
				                     Name = "GC_BEDR_GEOL_MAPPING_UNIT_ATT_JOIN",
				                     GeometryType = hasShape
					                                    ? (int) esriGeometryType.esriGeometryPolygon
					                                    : (int) esriGeometryType.esriGeometryNull
			                     };

			if (hasShape)
			{
				ISpatialReference spatialReference =
					SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

				objectClassMsg.SpatialReference = ProtobufGeometryUtils.ToSpatialReferenceMsg(
					spatialReference,
					SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);
			}

			AddField(objectClassMsg, "TOPGIS_GC.GC_BEDROCK.OBJECTID",
			         esriFieldType.esriFieldTypeOID);
			AddField(objectClassMsg, "TOPGIS_GC.GC_GEOL_MAPPING_UNIT_ATT.OBJECTID",
			         esriFieldType.esriFieldTypeInteger);
			AddField(objectClassMsg, "TOPGIS_GC.GC_GEOL_MAPPING_UNIT_ATT.UUID",
			         esriFieldType.esriFieldTypeString);
			AddField(objectClassMsg, "TOPGIS_GC.GC_BEDROCK.UUID",
			         esriFieldType.esriFieldTypeString);
			AddField(objectClassMsg, _actualField, esriFieldType.esriFieldTypeString);

			if (hasShape)
			{
				AddField(objectClassMsg, "TOPGIS_GC.GC_BEDROCK.SHAPE",
				         esriFieldType.esriFieldTypeGeometry);
			}

			GdbTable result = ProtobufConversionUtils.FromQueryTableMsg(
				objectClassMsg, new WorkspaceMock(), t => null,
				baseTables ?? new List<IReadOnlyTable>());

			Assert.IsInstanceOf(hasShape ? typeof(RemoteQueryFeatureClass)
				                    : typeof(RemoteQueryTable), result);

			return result;
		}

		private static void AddField(ObjectClassMsg objectClassMsg, string name,
		                             esriFieldType fieldType)
		{
			objectClassMsg.Fields.Add(new FieldMsg
			                          {
				                          Name = name,
				                          Type = (int) fieldType,
				                          Length = fieldType == esriFieldType.esriFieldTypeString
					                                   ? 38
					                                   : 0
			                          });
		}
	}
}
