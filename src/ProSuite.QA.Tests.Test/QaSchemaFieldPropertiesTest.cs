using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Schema;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.QA.Tests.Test.TestData;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldPropertiesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

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

		// TODO add tests to cover all cases (expected domain etc.)

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void InvalidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100004001");

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				"Field 'NAME' has same alias ('Gemeindename') as specification for field 'GEMEINDE'. Field name should also be equal",
				errors[0].Description);
		}

		[Test]
		public void ValidTable1()
		{
			NoErrors(GetErrors("GEO_00100024004"));
		}

		[Test]
		public void ValidTable2()
		{
			NoErrors(GetErrors("GEO_00100059002"));
		}

		[Test]
		public void InvalidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100436001");
			Assert.AreEqual(3, errors.Count);

			Assert.AreEqual(
				"Expected field length for field 'GEMEINDE': 30. Actual field length: 13",
				errors[0].Description);
			Assert.AreEqual(
				"Expected alias name for field 'GEMEINDE': 'Gemeindename'. Actual alias name: 'Name der Standortgemeinde'",
				errors[1].Description);
			Assert.AreEqual(
				"Expected field type for field 'Y_COORD': Double. Actual field type: Text",
				errors[2].Description);
		}

		[Test]
		public void InvalidTable3()
		{
			// errors via 'reserved fields' lookup are reported by QaSchemaReservedFieldProperties
			IList<QaError> errors = GetErrors("GEO_00100510001");

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable3()
		{
			NoErrors(GetErrors("GEO_00100633001"));
		}

		private static void NoErrors([NotNull] ICollection<QaError> errors)
		{
			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			ITable fieldSpecificationsTable = workspace.OpenTable("S_011");

			const bool matchAliasName = true;
			var test = new QaSchemaFieldPropertiesFromTable(table, fieldSpecificationsTable,
			                                                matchAliasName);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}

		#endregion

		private class QaSchemaFieldPropertiesFromTable : QaSchemaFieldPropertiesBase
		{
			private readonly ITable _fieldSpecificationsTable;

			public QaSchemaFieldPropertiesFromTable([NotNull] ITable table,
			                                        [NotNull] ITable fieldSpecificationsTable,
			                                        bool matchAliasName)
				: base(table, matchAliasName, fieldSpecificationsTable)
			{
				_fieldSpecificationsTable = fieldSpecificationsTable;
			}

			[NotNull]
			private IEnumerable<FieldSpecification> ReadFieldSpecifications(
				[NotNull] ITable table)
			{
				int nameFieldIndex = table.FindField("ATTRIBUTE");
				int typeStringFieldIndex = table.FindField("FIELDTYPE_ARCGIS");
				int aliasFieldIndex = table.FindField("ALIAS");
				int accessTypeFieldIndex = table.FindField("FIELDTYPE_ACCESS");

				const bool recycle = true;
				foreach (IRow row in GdbQueryUtils.GetRows(table, GetQueryFilter(), recycle))
				{
					var name = row.Value[nameFieldIndex] as string;

					Console.WriteLine(name);
					if (string.IsNullOrEmpty(name))
					{
						continue;
					}

					object type = row.Value[typeStringFieldIndex];
					var typeString = type as string;

					if (string.IsNullOrEmpty(typeString))
					{
						throw new InvalidConfigurationException(
							string.Format("Expected type is undefined: '{0}'", type));
					}

					var alias = row.Value[aliasFieldIndex] as string;
					esriFieldType expectedType = GetFieldType(typeString);

					var accessTypeString = row.Value[accessTypeFieldIndex] as string;

					int length = expectedType == esriFieldType.esriFieldTypeString
						             ? GetTextLength(accessTypeString)
						             : -1;

					yield return
						new FieldSpecification(name, expectedType, length, alias, null, true);
				}
			}

			[NotNull]
			private IQueryFilter GetQueryFilter()
			{
				string constraint = GetConstraint(_fieldSpecificationsTable);

				IQueryFilter result = new QueryFilter();

				if (StringUtils.IsNotEmpty(constraint))
				{
					result.WhereClause = constraint;
				}

				return result;
			}

			private static int GetTextLength(string accessType)
			{
				string trimmed = accessType.ToUpper()
				                           .Replace("TEXT(", string.Empty)
				                           .Replace(")", string.Empty)
				                           .Trim();

				int result;
				if (! int.TryParse(trimmed, out result))
				{
					throw new InvalidConfigurationException(
						$"Unable to parse text length from string '{accessType}'");
				}

				return result;
			}

			private static esriFieldType GetFieldType([NotNull] string typeString)
			{
				switch (typeString)
				{
					case "SHORT INTEGER":
						return esriFieldType.esriFieldTypeSmallInteger;

					case "LONG INTEGER":
						return esriFieldType.esriFieldTypeInteger;

					case "DOUBLE":
						return esriFieldType.esriFieldTypeDouble;

					case "TEXT":
						return esriFieldType.esriFieldTypeString;

					case "DATE":
						return esriFieldType.esriFieldTypeDate;
				}

				throw new ArgumentException(
					string.Format("Unsupported type string: {0}", typeString), nameof(typeString));
			}

			protected override IEnumerable<FieldSpecification> GetFieldSpecifications()
			{
				return ReadFieldSpecifications(_fieldSpecificationsTable);
			}
		}
	}
}
