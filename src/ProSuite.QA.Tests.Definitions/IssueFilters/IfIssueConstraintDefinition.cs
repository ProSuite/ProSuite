using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	public class IfIssueConstraintDefinition : AlgorithmDefinition
	{
		public string Constraint { get; }

		[DocIf(nameof(DocIfStrings.IfIssueConstraint_0))]
		public IfIssueConstraintDefinition(
			[DocIf(nameof(DocIfStrings.IfIssueConstraint_constraint))]
			string constraint)
			: base(new ITableSchemaDef[] { })
		{
			Constraint = constraint;
		}
	}
}
