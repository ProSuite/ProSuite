using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.KeySets;
using Tuple = ProSuite.QA.Tests.KeySets.Tuple;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaForeignKey : NonContainerTest
	{
		[NotNull] private readonly IReadOnlyTable _table;
		[NotNull] private readonly IReadOnlyTable _referencedTable;
		private readonly bool _referenceIsError;

		[NotNull] private readonly List<string> _foreignKeyFields;
		[NotNull] private readonly List<string> _referencedKeyFields;
		[NotNull] private readonly List<int> _foreignKeyFieldIndices;
		[NotNull] private readonly List<int> _referencedKeyFieldIndices;
		[NotNull] private readonly List<esriFieldType> _foreignKeyFieldTypes;
		[NotNull] private readonly List<esriFieldType> _referencedKeyFieldTypes;
		[NotNull] private readonly string _foreignKeyFieldNamesString;
		[NotNull] private readonly string _referencedKeyFieldNamesString;

		private readonly bool _usesSingleKey;

		private string _whereClause;
		private IKeySet _keySet;
		private ITupleKeySet _tupleKeySet;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoReferencedRow = "NoReferencedRow";
			public const string UnexpectedReferencedRow = "UnexpectedReferencedRow";
			public const string UnableToConvertFieldValue = "UnableToConvertFieldValue";

			public Code() : base("ForeignKeys") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaForeignKey_0))]
		public QaForeignKey(
			[Doc(nameof(DocStrings.QaForeignKey_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaForeignKey_foreignKeyField))] [NotNull]
			string foreignKeyField,
			[Doc(nameof(DocStrings.QaForeignKey_referencedTable))] [NotNull]
			IReadOnlyTable referencedTable,
			[Doc(nameof(DocStrings.QaForeignKey_referencedKeyField))] [NotNull]
			string referencedKeyField)
			: this(table, new[] {foreignKeyField}, referencedTable, new[] {referencedKeyField}) { }

		[Doc(nameof(DocStrings.QaForeignKey_1))]
		public QaForeignKey(
				[Doc(nameof(DocStrings.QaForeignKey_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaForeignKey_foreignKeyFields))] [NotNull]
				IEnumerable<string>
					foreignKeyFields,
				[Doc(nameof(DocStrings.QaForeignKey_referencedTable))] [NotNull]
				IReadOnlyTable referencedTable,
				[Doc(nameof(DocStrings.QaForeignKey_referencedKeyFields))] [NotNull]
				IEnumerable<string>
					referencedKeyFields)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, foreignKeyFields, referencedTable, referencedKeyFields, false) { }

		[Doc(nameof(DocStrings.QaForeignKey_2))]
		public QaForeignKey(
			[Doc(nameof(DocStrings.QaForeignKey_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaForeignKey_foreignKeyFields))] [NotNull]
			IEnumerable<string>
				foreignKeyFields,
			[Doc(nameof(DocStrings.QaForeignKey_referencedTable))] [NotNull]
			IReadOnlyTable referencedTable,
			[Doc(nameof(DocStrings.QaForeignKey_referencedKeyFields))] [NotNull]
			IEnumerable<string>
				referencedKeyFields,
			[Doc(nameof(DocStrings.QaForeignKey_referenceIsError))]
			bool referenceIsError)
			: base(new[] {table, referencedTable})
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(foreignKeyFields, nameof(foreignKeyFields));
			Assert.ArgumentNotNull(referencedTable, nameof(referencedTable));
			Assert.ArgumentNotNull(referencedKeyFields, nameof(referencedKeyFields));

			_table = table;
			_foreignKeyFields = new List<string>(foreignKeyFields);
			_referencedTable = referencedTable;
			_referenceIsError = referenceIsError;
			_referencedKeyFields = new List<string>(referencedKeyFields);

			Assert.ArgumentCondition(_foreignKeyFields.Count > 0,
			                         "There must be at least one foreign key field");
			Assert.ArgumentCondition(_foreignKeyFields.Count == _referencedKeyFields.Count,
			                         "The number of foreign key fields must be equal to " +
			                         "the number of referenced key fields");

			_usesSingleKey = _foreignKeyFields.Count == 1;

			_foreignKeyFieldNamesString = StringUtils.Concatenate(_foreignKeyFields, ", ");
			_referencedKeyFieldNamesString = StringUtils.Concatenate(_referencedKeyFields, ", ");

			GetFieldInformation(table, referencedTable, _foreignKeyFields, _referencedKeyFields,
			                    out _foreignKeyFieldIndices,
			                    out _referencedKeyFieldIndices,
			                    out _foreignKeyFieldTypes,
			                    out _referencedKeyFieldTypes);
		}

		[InternallyUsedTest]
		public QaForeignKey([NotNull] QaForeignKeyDefinition definition)
			: this((IReadOnlyTable)definition.Table,
			       definition.ForeignKeyFields,
			       (IReadOnlyTable)definition.ReferencedTable,
			       definition.ReferencedKeyFields,
			       definition.ReferenceIsError)
		{ }


		#region Overrides of TestBase

		public override int Execute()
		{
			return ExecuteGeometry(null);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return ExecuteGeometry(boundingBox);
		}

		public override int Execute(IPolygon area)
		{
			return ExecuteGeometry(area);
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
		{
			int errorCount = 0;

			foreach (IReadOnlyRow row in selectedRows)
			{
				if (row.Table != _table)
				{
					continue;
				}

				EnsureKeySet();

				errorCount += VerifyRow(row);
			}

			return errorCount;
		}

		public override int Execute(IReadOnlyRow row)
		{
			if (row.Table != _table)
			{
				return NoError;
			}

			EnsureKeySet();

			return VerifyRow(row);
		}

		protected override ISpatialReference GetSpatialReference()
		{
			var geoDataset = _table as IReadOnlyGeoDataset;
			return geoDataset?.SpatialReference;
		}

		#endregion

		private static void GetFieldInformation(
			[NotNull] IReadOnlyTable table,
			[NotNull] IReadOnlyTable referencedTable,
			[NotNull] IList<string> fkFields,
			[NotNull] IList<string> pkFields,
			[NotNull] out List<int> foreignKeyFieldIndices,
			[NotNull] out List<int> referencedKeyFieldIndices,
			[NotNull] out List<esriFieldType> foreignKeyFieldTypes,
			[NotNull] out List<esriFieldType> referencedKeyFieldTypes)
		{
			foreignKeyFieldIndices = new List<int>();
			referencedKeyFieldIndices = new List<int>();
			foreignKeyFieldTypes = new List<esriFieldType>();
			referencedKeyFieldTypes = new List<esriFieldType>();

			for (int i = 0; i < fkFields.Count; i++)
			{
				string fkField = fkFields[i];
				string pkField = pkFields[i];

				int fkIndex = table.FindField(fkField);
				int pkIndex = referencedTable.FindField(pkField);

				const string format = "'field '{0}' not found in table '{1}'";
				Assert.ArgumentCondition(fkIndex >= 0, format,
				                         fkField, table.Name);
				Assert.ArgumentCondition(pkIndex >= 0, format,
				                         pkField, referencedTable.Name);

				esriFieldType fkType = KeySetUtils.GetFieldValueType(table, fkIndex);
				esriFieldType pkType = KeySetUtils.GetFieldValueType(referencedTable, pkIndex);

				bool supported = KeySetUtils.IsSupportedTypeCombination(fkType, pkType);

				Assert.ArgumentCondition(supported,
				                         "key fields have unsupported combination of types: " +
				                         "foreign key {0}: {1} - referenced key {2}: {3}",
				                         fkField, fkType, pkField, pkType);

				foreignKeyFieldIndices.Add(fkIndex);
				referencedKeyFieldIndices.Add(pkIndex);
				foreignKeyFieldTypes.Add(fkType);
				referencedKeyFieldTypes.Add(pkType);
			}
		}

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			EnsureKeySet();

			if (! (_table is IReadOnlyFeatureClass))
			{
				geometry = null;
			}

			ITableFilter filter = TestUtils.CreateFilter(geometry, AreaOfInterest,
			                                             GetConstraint(0), _table, null);

			const bool recycle = true;
			int errorCount = 0;

			foreach (IReadOnlyRow row in _table.EnumRows(filter, recycle))
			{
				errorCount += VerifyRow(row);
			}

			return errorCount;
		}

		private void EnsureKeySet()
		{
			if (_keySet != null || _tupleKeySet != null)
			{
				return;
			}

			_whereClause = GetConstraint(_referencedTable);

			if (_usesSingleKey)
			{
				_keySet = KeySetUtils.ReadKeySet(_referencedTable, _referencedKeyFields[0],
				                                 _whereClause, _referencedKeyFieldTypes[0],
				                                 _referencedKeyFieldIndices[0]);
			}
			else
			{
				_tupleKeySet = KeySetUtils.ReadTupleKeySet(_referencedTable, _referencedKeyFields,
				                                           _whereClause, _referencedKeyFieldTypes,
				                                           _referencedKeyFieldIndices);
			}
		}

		private int VerifyRow([NotNull] IReadOnlyRow row)
		{
			return _usesSingleKey
				       ? VerifySingleKey(row)
				       : VerifyTupleKey(row);
		}

		private int VerifySingleKey([NotNull] IReadOnlyRow row)
		{
			Assert.NotNull(_keySet, "keyset is null");

			int fkIndex = _foreignKeyFieldIndices[0];
			string fkField = _foreignKeyFields[0];
			string pkField = _referencedKeyFields[0];
			esriFieldType fkType = _foreignKeyFieldTypes[0];
			esriFieldType pkType = _referencedKeyFieldTypes[0];

			object foreignKey = row.get_Value(fkIndex);

			if (foreignKey == null || foreignKey is DBNull)
			{
				// the foreign key is null --> ok
				return NoError;
			}

			object convertedValue;
			try
			{
				convertedValue = FieldUtils.ConvertAttributeValue(foreignKey, fkType, pkType);
			}
			catch (Exception e)
			{
				string errorDescription = FieldValueUtils.GetTypeConversionErrorDescription(
					_referencedTable, foreignKey, fkField, pkField, e.Message);

				return ReportError(
					errorDescription, InvolvedRowUtils.GetInvolvedRows(row),
					null, Codes[Code.UnableToConvertFieldValue], fkField);
			}

			if (_referenceIsError)
			{
				return _keySet.Contains(convertedValue)
					       ? ReportUnallowedReference(row, foreignKey, fkField, pkField)
					       : NoError;
			}

			return _keySet.Contains(convertedValue)
				       ? NoError
				       : ReportMissingReference(row, foreignKey, fkField, pkField);
		}

		private int VerifyTupleKey([NotNull] IReadOnlyRow row)
		{
			Assert.NotNull(_tupleKeySet, "tuple keyset is null");

			string errorMessage;
			Tuple fkTuple = TryReadForeignKeyTuple(row, _foreignKeyFieldIndices,
			                                       _foreignKeyFieldTypes,
			                                       _referencedKeyFieldTypes,
			                                       _foreignKeyFields,
			                                       _referencedKeyFields,
			                                       out errorMessage);

			if (fkTuple == null)
			{
				// unable to read the tuple
				return ReportError(
					errorMessage, InvolvedRowUtils.GetInvolvedRows(row),
					null, Codes[Code.UnableToConvertFieldValue], null);
			}

			if (fkTuple.IsNull)
			{
				// the composite foreign key values are all null --> ok
				return NoError;
			}

			if (_referenceIsError)
			{
				return _tupleKeySet.Contains(fkTuple)
					       ? ReportUnallowedReference(row, fkTuple)
					       : NoError;
			}

			return _tupleKeySet.Contains(fkTuple)
				       ? NoError
				       : ReportMissingReference(row, fkTuple);
		}

		private int ReportMissingReference(
			[NotNull] IReadOnlyRow row,
			[NotNull] object foreignKey,
			[NotNull] string fkField,
			[NotNull] string pkField)
		{
			string description =
				string.Format(
					"Value [{0}] in field '{1}' does not reference any value in field '{2}' of table '{3}'{4}",
					FieldValueUtils.FormatValue(foreignKey), fkField, pkField,
					_referencedTable.Name,
					StringUtils.IsNotEmpty(_whereClause)
						? string.Format(" (in rows matching '{0}')", _whereClause)
						: string.Empty);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				null, Codes[Code.NoReferencedRow], fkField);
		}

		private int ReportUnallowedReference(
			[NotNull] IReadOnlyRow row,
			[NotNull] object foreignKey,
			[NotNull] string fkField,
			[NotNull] string pkField)
		{
			string description =
				string.Format(
					"Value [{0}] in field '{1}' references a value in field '{2}' of table '{3}'{4}",
					FieldValueUtils.FormatValue(foreignKey), fkField, pkField,
					_referencedTable.Name,
					StringUtils.IsNotEmpty(_whereClause)
						? string.Format(" (in rows matching '{0}')", _whereClause)
						: string.Empty);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				null, Codes[Code.UnexpectedReferencedRow], fkField);
		}

		private int ReportMissingReference([NotNull] IReadOnlyRow row,
		                                   [NotNull] Tuple fkTuple)
		{
			string description =
				string.Format(
					"Values [{0}] in fields '{1}' do not match a value combination in fields '{2}' of table '{3}'{4}",
					FormatTuple(fkTuple),
					_foreignKeyFieldNamesString, _referencedKeyFieldNamesString,
					_referencedTable.Name,
					StringUtils.IsNotEmpty(_whereClause)
						? string.Format(" (in rows matching '{0}')", _whereClause)
						: string.Empty);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				null, Codes[Code.NoReferencedRow], _foreignKeyFieldNamesString);
		}

		private int ReportUnallowedReference([NotNull] IReadOnlyRow row,
		                                     [NotNull] Tuple fkTuple)
		{
			string description =
				string.Format(
					"Values [{0}] in fields '{1}' references a value combination in fields '{2}' of table '{3}'{4}",
					FormatTuple(fkTuple),
					_foreignKeyFieldNamesString, _referencedKeyFieldNamesString,
					_referencedTable.Name,
					StringUtils.IsNotEmpty(_whereClause)
						? string.Format(" (in rows matching '{0}')", _whereClause)
						: string.Empty);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				null, Codes[Code.UnexpectedReferencedRow], _foreignKeyFieldNamesString);
		}

		[NotNull]
		private static string FormatTuple([NotNull] Tuple tuple)
		{
			return StringUtils.Concatenate(tuple.Keys, FieldValueUtils.FormatValue, ",");
		}

		[CanBeNull]
		private Tuple TryReadForeignKeyTuple([NotNull] IReadOnlyRow row,
		                                     [NotNull] IList<int> foreignKeyFieldIndices,
		                                     [NotNull] IList<esriFieldType> fieldTypes,
		                                     [NotNull] IList<esriFieldType> outputTypes,
		                                     [NotNull] IList<string> foreignKeyFields,
		                                     [NotNull] IList<string> referencedKeyFields,
		                                     [NotNull] out string errorMessage)
		{
			var values = new List<object>(foreignKeyFieldIndices.Count);

			for (int i = 0; i < foreignKeyFieldIndices.Count; i++)
			{
				int fieldIndex = foreignKeyFieldIndices[i];
				esriFieldType fieldType = fieldTypes[i];
				esriFieldType outputType = outputTypes[i];

				object value = row.get_Value(fieldIndex);

				object convertedValue;
				try
				{
					convertedValue = FieldUtils.ConvertAttributeValue(value, fieldType, outputType);
				}
				catch (Exception e)
				{
					errorMessage = FieldValueUtils.GetTypeConversionErrorDescription(
						_referencedTable, value,
						foreignKeyFields[i], referencedKeyFields[i],
						e.Message);
					return null;
				}

				values.Add(convertedValue);
			}

			errorMessage = string.Empty;
			return new Tuple(values);
		}
	}
}
