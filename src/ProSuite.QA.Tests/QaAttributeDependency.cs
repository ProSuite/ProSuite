using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProSuite.Commons.AttributeDependencies;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	[InternallyUsedTest]
	public class QaAttributeDependency : ContainerTest
	{
		private readonly string[] _sourceFields;
		private readonly string[] _targetFields;

		private readonly IList<int> _sourceFieldIndices;
		private readonly IList<int> _targetFieldIndices;
		private readonly IDictionary<IList<object>, IList<object>> _mappings;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string SourceOrTargetFieldNotDefined = "SourceOrTargetFieldNotDefined";
			public const string NoMappingDefined = "NoMappingDefined";
			public const string SourceValuesNotDefined = "SourceValuesNotDefined";
			public const string TargetValuesNotDefined = "TargetValuesNotDefined";

			public Code() : base("AttributeDependency") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaAttributeDependency_0))]
		public QaAttributeDependency(
			[Doc(nameof(DocStrings.QaAttributeDependency_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaAttributeDependency_sourceFields))] [NotNull]
			string[] sourceFields,
			[Doc(nameof(DocStrings.QaAttributeDependency_targetFields))] [NotNull]
			string[] targetFields,
			[Doc(nameof(DocStrings.QaAttributeDependency_mappings))] [NotNull]
			IDictionary<string, string> mappings)
			: base(table)
		{
			Assert.ArgumentNotNull(sourceFields, nameof(sourceFields));
			Assert.ArgumentNotNull(targetFields, nameof(targetFields));
			Assert.ArgumentNotNull(mappings, nameof(mappings));

			_sourceFields = sourceFields;
			_targetFields = targetFields;

			_sourceFieldIndices = InitializeFieldIndices(table, sourceFields);
			_targetFieldIndices = InitializeFieldIndices(table, targetFields);

			_mappings = new Dictionary<IList<object>, IList<object>>(mappings.Count);
			foreach (KeyValuePair<string, string> keyValuePair in mappings)
			{
				_mappings.Add(AttributeDependencyUtils.ParseValues(keyValuePair.Key),
				              AttributeDependencyUtils.ParseValues(keyValuePair.Value));
			}
		}

		private static List<int> InitializeFieldIndices(IReadOnlyTable table,
		                                                IReadOnlyCollection<string> fieldNames)
		{
			var fieldIndices = new List<int>(fieldNames.Count);
			foreach (string field in fieldNames)
			{
				int fieldIndex = table.FindField(field);
				Assert.True(fieldIndex >= 0, "field '{0}' not found in table '{1}'",
				            field, table.Name);

				fieldIndices.Add(fieldIndex);
			}
			return fieldIndices;
		}

		#endregion

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (_sourceFieldIndices.Count == 0 || _targetFieldIndices.Count == 0)
			{
				const string description =
					"Either no source field or no target field defined in Attribute Dependency";

                return ReportError(
                    description, InvolvedRowUtils.GetInvolvedRows(row),
                    TestUtils.GetShapeCopy(row), Codes[Code.SourceOrTargetFieldNotDefined], null);
			}

			if (_mappings.Count == 0)
			{
				const string description =
					"No mapping (attribute value combination) defined in Attribute Dependency";
				return ReportError(
                    description, InvolvedRowUtils.GetInvolvedRows(row),
                    TestUtils.GetShapeCopy(row), Codes[Code.NoMappingDefined], null);
			}

			IList<object> sourceValues =
				_sourceFieldIndices.Select(row.get_Value).ToList();
			IList<IList<object>> combos = GetMatchingTargetValueCombinations(sourceValues,
			                                                                 _mappings);

			if (combos.Count < 1)
			{
				string noMatchingSource =
					string.Format("Source values ({0}) {1} not defined in Attribute Dependency",
					              StringUtils.Concatenate(_sourceFields, ","),
					              AttributeDependencyUtils.Format(sourceValues));
				return ReportError(
                    noMatchingSource, InvolvedRowUtils.GetInvolvedRows(row),
                    TestUtils.GetShapeCopy(row), Codes[Code.SourceValuesNotDefined], null);
			}

			IList<object> targetValues =
				_targetFieldIndices.Select(row.get_Value).ToList();

			if (combos.Any(combo => AttributeDependencyUtils.ValuesMatch(combo, targetValues)))
			{
				return 0;
			}

			var sb = new StringBuilder();

			string noMatchingTarget =
				string.Format(
					"Target values ({0}) {1} not defined in Attribute Dependency (given matching source values ({2}) {3})",
					StringUtils.Concatenate(_targetFields, ","),
					AttributeDependencyUtils.Format(targetValues, sb),
					StringUtils.Concatenate(_sourceFields, ","),
					AttributeDependencyUtils.Format(sourceValues, sb));
			return ReportError(
                noMatchingTarget, InvolvedRowUtils.GetInvolvedRows(row),
                TestUtils.GetShapeCopy(row), Codes[Code.TargetValuesNotDefined], null);
		}

		private static IList<IList<object>> GetMatchingTargetValueCombinations(
			IList<object> sourceValues,
			IEnumerable<KeyValuePair<IList<object>, IList<object>>> mappings)
		{
			return (from mapping in mappings
			        where AttributeDependencyUtils.ValuesMatch(mapping.Key, sourceValues)
			        select mapping.Value).ToList();
		}
	}
}
