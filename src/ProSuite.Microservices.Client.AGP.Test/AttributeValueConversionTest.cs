using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace ProSuite.Microservices.Client.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class AttributeValueConversionTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void ToAttributeValueMsg_converts_all_fields_of_a_row()
		{
			// ArcGIS Pro returns the object ID as int for tables with 32-bit object IDs and as
			// long for tables with 64-bit object IDs (measured with Pro 3.8). It used to be
			// unboxed as int, which fails for 64-bit object IDs, and was sent as short integer.
			// It is now always sent as big integer, like in the columnar transfer.
			string gdbPath = Path.Combine(Path.GetTempPath(),
			                              $"AttributeValueConversionTest_{Guid.NewGuid():N}.gdb");

			using (Geodatabase geodatabase = SchemaBuilder.CreateGeodatabase(
				       new FileGeodatabaseConnectionPath(new Uri(gdbPath))))
			{
				var tableDescription = new TableDescription(
					"ERRORS_ROW",
					new List<FieldDescription>
					{
						FieldDescription.CreateIntegerField("FEHLERART"),
						FieldDescription.CreateStringField("BESCHREIBUNG", 200),
						new FieldDescription("DATUM_ERSTELLUNG", FieldType.Date)
					});

				var schemaBuilder = new SchemaBuilder(geodatabase);
				schemaBuilder.Create(tableDescription);
				Assert.True(schemaBuilder.Build(), string.Join(", ", schemaBuilder.ErrorMessages));

				using Table table = geodatabase.OpenDataset<Table>("ERRORS_ROW");

				long oid;
				using (RowBuffer rowBuffer = table.CreateRowBuffer())
				{
					rowBuffer["FEHLERART"] = 300;
					rowBuffer["BESCHREIBUNG"] = "Erlaubter Fehler";
					rowBuffer["DATUM_ERSTELLUNG"] = new DateTime(2026, 10, 16);

					using Row createdRow = table.CreateRow(rowBuffer);
					oid = createdRow.GetObjectID();
				}

				IReadOnlyList<Field> fields = table.GetDefinition().GetFields();

				using RowCursor cursor = table.Search(null, false);
				Assert.True(cursor.MoveNext());

				using Row row = cursor.Current;

				var valuesByField = new Dictionary<string, AttributeValue>();

				for (int i = 0; i < fields.Count; i++)
				{
					object value = row[i];

					Console.WriteLine("{0} ({1}): {2}", fields[i].Name, fields[i].FieldType,
					                  value?.GetType().FullName ?? "null");

					valuesByField.Add(fields[i].Name,
					                  ProtobufConversionUtils.ToAttributeValueMsg(value, fields[i]));
				}

				string oidFieldName = table.GetDefinition().GetObjectIDField();

				Assert.AreEqual(oid, valuesByField[oidFieldName].BigIntValue);
				Assert.AreEqual(300, valuesByField["FEHLERART"].IntValue);
				Assert.AreEqual("Erlaubter Fehler", valuesByField["BESCHREIBUNG"].StringValue);
				Assert.AreEqual(new DateTime(2026, 10, 16).Ticks,
				                valuesByField["DATUM_ERSTELLUNG"].DateTimeTicksValue);
			}
		}
	}
}
