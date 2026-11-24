using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.Microservices.AO.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Test
{
	/// <summary>
	/// Tests the client-side conversion of rows into data messages, which the ArcMap
	/// client (Topgis) uses to answer data requests of the QA server.
	/// </summary>
	[TestFixture]
	public class VerificationRequestUtilsTest
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
		public void ReadGdbData_converts_numbers_stored_as_another_numeric_type()
		{
			// Issue C of the QA server issue list: "Specified cast is not valid." is the message
			// of a failed unboxing, e.g. (int) of a boxed long. Depending on the database and
			// driver, a numeric value can arrive as another numeric type than the field type.
			GdbTable errorTable = CreateErrorTable();

			GdbRow row = AddRow(errorTable, 1);
			row.set_Value(errorTable.FindField("FEHLERART"), 300L);
			row.set_Value(errorTable.FindField("STATUS"), 2);
			row.set_Value(errorTable.FindField("GEWICHT"), 1.5m);
			row.set_Value(errorTable.FindField("FAKTOR"), 2.5);

			GdbData data = ReadAllRows(errorTable);

			Assert.AreEqual(1, data.GdbObjects.Count);
			GdbObjectMsg objectMsg = data.GdbObjects[0];

			Assert.AreEqual(300, GetValue(objectMsg, errorTable, "FEHLERART").IntValue);
			Assert.AreEqual(2, GetValue(objectMsg, errorTable, "STATUS").ShortIntValue);
			Assert.AreEqual(1.5, GetValue(objectMsg, errorTable, "GEWICHT").DoubleValue);
			Assert.AreEqual(2.5f, GetValue(objectMsg, errorTable, "FAKTOR").FloatValue);
		}

		[Test]
		public void ReadGdbData_error_names_table_row_field_and_value_type()
		{
			GdbTable errorTable = CreateErrorTable();

			GdbRow row = AddRow(errorTable, 42);
			row.set_Value(errorTable.FindField("FEHLERART"), "not a number");

			var exception = Assert.Catch<Exception>(() => ReadAllRows(errorTable));

			string message = exception.Message;
			Console.WriteLine(message);

			StringAssert.Contains("TOPGIS_TLM.TLM_ERRORS_ROW", message);
			StringAssert.Contains("42", message);
			StringAssert.Contains("FEHLERART", message);
			StringAssert.Contains(nameof(esriFieldType.esriFieldTypeInteger), message);
			StringAssert.Contains(typeof(string).FullName, message);
		}

		/// <summary>
		/// Reads the allowed errors of the real error table with the same code as the ArcMap
		/// client, and lists the .NET type of every field value.
		/// Set the environment variable PROSUITE_TEST_ERRORS_SDE_FILE to an .sde connection file
		/// of a TOPGIS database (for example TOPGIST as topgis_tlm).
		/// </summary>
		[Test]
		[Explicit("Needs a TOPGIS database connection")]
		[Category(TestCategory.Sde)]
		public void ReadGdbData_reads_allowed_errors_of_real_error_table()
		{
			string sdeFilePath =
				Environment.GetEnvironmentVariable("PROSUITE_TEST_ERRORS_SDE_FILE");

			if (string.IsNullOrEmpty(sdeFilePath))
			{
				Assert.Ignore("PROSUITE_TEST_ERRORS_SDE_FILE is not set");
			}

			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(sdeFilePath);

			ITable table = DatasetUtils.OpenTable(workspace, "TOPGIS_TLM.TLM_ERRORS_ROW");
			IReadOnlyTable roTable = ReadOnlyTableFactory.Create(table);

			const string whereClause = "FEHLERART = 300";

			// The field value types as returned by ArcObjects for this database:
			IReadOnlyList<ITableField> fields = ((ITableSchemaDef) roTable).TableFields;
			var valueTypesByField = new Dictionary<string, HashSet<string>>();

			foreach (IReadOnlyRow row in roTable.EnumRows(
				         new AoTableFilter { WhereClause = whereClause }, true))
			{
				for (int i = 0; i < fields.Count; i++)
				{
					object value = row.GetValue(i);
					string typeName = value?.GetType().FullName ?? "null";

					if (! valueTypesByField.TryGetValue(fields[i].Name, out var typeNames))
					{
						typeNames = new HashSet<string>();
						valueTypesByField.Add(fields[i].Name, typeNames);
					}

					typeNames.Add(typeName);
				}
			}

			foreach (ITableField field in fields)
			{
				valueTypesByField.TryGetValue(field.Name, out var typeNames);

				Console.WriteLine(
					"{0} ({1}): {2}", field.Name, (esriFieldType) field.FieldType,
					typeNames == null ? "<no rows>" : string.Join(", ", typeNames));
			}

			// The same call as the ArcMap client makes for the server's data request:
			ITableFilter filter =
				VerificationRequestUtils.CreateFilter(roTable, "*", whereClause, null);

			GdbData data = VerificationRequestUtils.ReadGdbData(
				roTable, filter, "*", ((IObjectClass) table).ObjectClassID);

			Console.WriteLine("Converted {0} rows", data.GdbObjects.Count);
		}

		private static GdbTable CreateErrorTable()
		{
			var table = new GdbTable(417, "TOPGIS_TLM.TLM_ERRORS_ROW");

			table.AddField(FieldUtils.CreateOIDField());
			table.AddField(FieldUtils.CreateIntegerField("FEHLERART"));
			table.AddField(FieldUtils.CreateSmallIntegerField("STATUS"));
			table.AddField(FieldUtils.CreateDoubleField("GEWICHT"));
			table.AddField(FieldUtils.CreateSingleField("FAKTOR"));
			table.AddField(FieldUtils.CreateTextField("BESCHREIBUNG", 200));

			return table;
		}

		private static GdbRow AddRow(GdbTable table, long oid)
		{
			GdbRow row = table.CreateObject(oid);

			((InMemoryDataset) table.BackingDataset).AllRows.Add(row);

			return row;
		}

		private static GdbData ReadAllRows(GdbTable table)
		{
			IReadOnlyTable roTable = table;

			ITableFilter filter =
				VerificationRequestUtils.CreateFilter(roTable, "*", null, null);

			return VerificationRequestUtils.ReadGdbData(roTable, filter, "*",
			                                           table.ObjectClassID);
		}

		private static AttributeValue GetValue(GdbObjectMsg objectMsg,
		                                       GdbTable table,
		                                       string fieldName)
		{
			int fieldIndex = table.FindField(fieldName);

			return objectMsg.Values.ElementAt(fieldIndex);
		}
	}
}
