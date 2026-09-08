using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	public class IfIssueConstraint : IssueFilter
	{
		[NotNull] private readonly IssueConstraint _constraint;

		[DocIf(nameof(DocIfStrings.IfIssueConstraint_0))]
		public IfIssueConstraint(
			[DocIf(nameof(DocIfStrings.IfIssueConstraint_constraint))]
			string constraint)
			: base(new IReadOnlyTable[] { }) // needs no data of its own
		{
			_constraint = new IssueConstraint(constraint);
		}

		[InternallyUsedTest]
		public IfIssueConstraint([NotNull] IfIssueConstraintDefinition definition)
			: this(definition.Constraint) { }

		public override bool Check(QaErrorEventArgs error)
		{
			return _constraint.IsFulfilled(error.QaError);
		}
	}
}
