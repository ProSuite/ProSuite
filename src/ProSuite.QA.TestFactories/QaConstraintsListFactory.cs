using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.Constraints;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaConstraintsListFactory : QaFactoryBase
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaConstraint.Codes;

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] args = base.Args(datasetContext, testParameters, out tableParameters);

			Assert.True(args.Length == 5, "expected 5 arguments, got {0}", args.Length);
			Assert.True(args[0] is IReadOnlyTable, "arg0 is {0}, expected ITable",
			            args[0].GetType());
			Assert.True(args[1] is IReadOnlyTable, "arg1 is {0}, expected ITable",
			            args[1].GetType());
			Assert.True(args[2] is string, "arg2 is {0}, expected string", args[2].GetType());
			Assert.True(args[3] is bool, "arg3 is {0}, expected boolean", args[3].GetType());
			Assert.True(args[4] is string, "arg4 is {0}, expected string", args[4].GetType());

			var testArgs = new object[2];

			string filterExpression = tableParameters[1].FilterExpression;
			tableParameters.RemoveAt(1);

			var invertCondition = (bool) args[3];
			testArgs[0] = args[0];
			testArgs[1] = GetConstraints((IReadOnlyTable) args[1],
			                             filterExpression,
			                             (string) args[2],
			                             args[4] as string,
			                             invertCondition);

			return testArgs;
		}

		[NotNull]
		private static IList<ConstraintNode> GetConstraints(
			[NotNull] IReadOnlyTable constraintsTable,
			[CanBeNull] string filterExpression,
			[NotNull] string constraintField,
			[CanBeNull] string constraintDescriptionField,
			bool invertCondition)
		{
			var result = new List<ConstraintNode>();

			int constraintIndex = constraintsTable.FindField(constraintField);
			Assert.True(constraintIndex >= 0, "Field {0} not found in table {1}",
			            constraintField, constraintsTable.Name);

			int descriptionIndex = -1;
			if (StringUtils.IsNotEmpty(constraintDescriptionField))
			{
				descriptionIndex = constraintsTable.FindField(constraintDescriptionField);
				Assert.True(descriptionIndex >= 0,
				            "Field {0} not found in table {1}",
				            constraintDescriptionField, constraintsTable.Name);
			}

			ITableFilter filter = CreateQueryFilter(filterExpression, constraintField,
			                                        constraintDescriptionField);

			const bool recycling = true;
			foreach (IReadOnlyRow row in constraintsTable.EnumRows(filter, recycling))
			{
				var constraint = row.get_Value(constraintIndex) as string;

				if (constraint == null || constraint.Trim().Length == 0)
				{
					continue;
				}

				string constraintDescription =
					descriptionIndex >= 0
						? row.get_Value(descriptionIndex) as string
						: null;

				result.Add(
					CreateConstraintNode(constraint, constraintDescription, invertCondition));
			}

			return result;
		}

		[NotNull]
		private static ConstraintNode CreateConstraintNode(
			[NotNull] string constraint,
			[CanBeNull] string constraintDescription, bool invertCondition)
		{
			var functionReplacements = new Dictionary<string, string> { { "LENGTH", "LEN" } };

			string translated = TranslateConstraint(constraint, functionReplacements);

			if (invertCondition)
			{
				translated = string.Format("NOT ({0})", translated);
			}

			return new ConstraintNode(translated, constraintDescription);
		}

		[NotNull]
		private static string TranslateConstraint(
			[NotNull] string constraint,
			[NotNull] Dictionary<string, string> functionReplacements)
		{
			string result = constraint;
			foreach (KeyValuePair<string, string> functionReplacement in functionReplacements)
			{
				result = ReplaceFunction(result, functionReplacement.Key,
				                         functionReplacement.Value);
			}

			return result;
		}

		[NotNull]
		private static string ReplaceFunction([NotNull] string constraint,
		                                      [NotNull] string function,
		                                      [NotNull] string replacement)
		{
			string paddedConstraint = string.Format(" {0}", constraint);
			return ReplaceFunction(paddedConstraint,
			                       @"[ ]{0}[ \t]+\(", function,
			                       " {0}(", replacement).Trim();
		}

		[NotNull]
		private static string ReplaceFunction([NotNull] string constraint,
		                                      [NotNull] string patternFormat,
		                                      [NotNull] string function,
		                                      [NotNull] string replacementFormat,
		                                      [NotNull] string replacement)
		{
			string pattern = string.Format(patternFormat, function);

			var regex = new Regex(pattern, RegexOptions.IgnoreCase);

			return regex.Replace(constraint, string.Format(replacementFormat, replacement));
		}

		[NotNull]
		private static ITableFilter CreateQueryFilter(
			[CanBeNull] string filterExpression,
			[NotNull] string constraintField,
			[CanBeNull] string constraintDescriptionField)
		{
			ITableFilter result = new AoTableFilter
			                      {
				                      WhereClause = filterExpression,
				                      SubFields = constraintField
			                      };

			var subfields = new List<string> { constraintField };
			if (StringUtils.IsNotEmpty(constraintDescriptionField))
			{
				subfields.Add(constraintDescriptionField);
			}

			TableFilterUtils.SetSubFields(result, subfields);

			return result;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			return new QaConstraint((IReadOnlyTable) args[0], (IList<ConstraintNode>) args[1]);
		}
	}
}
