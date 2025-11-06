using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRegularExpressionDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string Pattern { get; }
		public IEnumerable<string> FieldNames { get; }
		public bool MatchIsError { get; }

		public string PatternDescription { get; }

		[Doc(nameof(DocStrings.QaRegularExpression_0))]
		public QaRegularExpressionDefinition(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldName))] [NotNull]
				string fieldName)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, TestDefinitionUtils.GetTokens(fieldName), false, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_1))]
		public QaRegularExpressionDefinition(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldNames))] [NotNull]
				IEnumerable<string> fieldNames)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, fieldNames, false, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_2))]
		public QaRegularExpressionDefinition(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldName))] [NotNull]
				string fieldName,
				[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
				bool matchIsError)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, TestDefinitionUtils.GetTokens(fieldName),
			       matchIsError, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_3))]
		public QaRegularExpressionDefinition(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldNames))] [NotNull]
				IEnumerable<string> fieldNames,
				[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
				bool matchIsError)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, fieldNames, matchIsError, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_4))]
		public QaRegularExpressionDefinition(
			[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaRegularExpression_fieldName))] [NotNull]
			string fieldName,
			[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaRegularExpression_patternDescription))] [CanBeNull]
			string patternDescription)
			: this(table, pattern, TestDefinitionUtils.GetTokens(fieldName),
			       matchIsError, patternDescription) { }

		[Doc(nameof(DocStrings.QaRegularExpression_5))]
		public QaRegularExpressionDefinition(
			[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaRegularExpression_fieldNames))] [NotNull]
			IEnumerable<string> fieldNames,
			[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaRegularExpression_patternDescription))] [CanBeNull]
			string patternDescription)
			: base(table)
		{
			Assert.ArgumentNotNullOrEmpty(pattern, nameof(pattern));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			Table = table;
			Pattern = pattern;
			FieldNames = fieldNames.ToList();
			MatchIsError = matchIsError;
			PatternDescription = patternDescription;
		}

		[Doc(nameof(DocStrings.QaRegularExpression_FieldListType))]
		[TestParameter(FieldListType.RelevantFields)]
		public FieldListType FieldListType { get; set; }
	}
}
