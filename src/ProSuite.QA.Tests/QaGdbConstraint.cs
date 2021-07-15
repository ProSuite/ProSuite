using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Checks Constraints on a table
	/// </summary>
	[UsedImplicitly]
	[AttributeTest]
	public class QaGdbConstraint : ContainerTest
	{
		private readonly List<IAttributeRule> _attrRules;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidRuleDefinition = "InvalidRuleDefinition";
			public const string GdbConstraintNotFulfilled = "GdbConstraintNotFulfilled";

			public Code() : base("GdbConstraint") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaGdbConstraint_0))]
		public QaGdbConstraint(
			[Doc(nameof(DocStrings.QaGdbConstraint_table))]
			ITable table)
			: base(table)
		{
			_attrRules = new List<IAttributeRule>();

			var validation = table as IValidation;
			if (validation != null)
			{
				IEnumRule rules = validation.Rules;
				rules.Reset();

				IRule rule;
				while ((rule = rules.Next()) != null)
				{
					if (rule.Type == esriRuleType.esriRTAttribute)
					{
						_attrRules.Add((IAttributeRule) rule);
					}
				}
			}
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			return (AreaOfInterest != null);
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			int errorCount = 0;
			int? iSubtype = null;
			IAttributeRule invalidRule = null;

			foreach (IAttributeRule rule in _attrRules)
			{
				string message = string.Empty;
				IssueCode issueCode = Codes[Code.GdbConstraintNotFulfilled];
				bool valid = true;

				try
				{
					if (rule.SubtypeCode >= 0)
					{
						if (! iSubtype.HasValue)
						{
							try
							{
								iSubtype = ((IRowSubtypes) row).SubtypeCode;
							}
							catch (Exception exp)
							{
								message = exp.Message;
								valid = false;
							}
						}
						else if (iSubtype.Value == rule.SubtypeCode)
						{
							valid = rule.Validate(row, out message);
						}
					}
					else
					{
						valid = rule.Validate(row, out message);
					}
				}
				catch (Exception exp)
				{
					invalidRule = rule;
					valid = false;
					message =
						string.Format(
							"Invalid rule definition (inform your system administrator): {0}" +
							"; {1}; {2}", rule.FieldName, rule.Category, exp.Message);
					issueCode = Codes[Code.InvalidRuleDefinition];
				}

				if (! valid)
				{
					string description = string.Format("First invalid rule: {0}", message);
					errorCount += ReportError(description,
					                          TestUtils.GetShapeCopy(row),
					                          issueCode,
					                          rule.FieldName, row);

					break;
				}
			}

			if (invalidRule != null)
			{
				_attrRules.Remove(invalidRule);
			}

			return errorCount;
		}
	}
}
