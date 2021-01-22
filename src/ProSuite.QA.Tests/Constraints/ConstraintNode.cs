using System.Collections.Generic;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Constraints
{
	public class ConstraintNode
	{
		[NotNull] private readonly List<ConstraintNode> _nodes;
		private readonly bool? _caseSensitivityOverride;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConstraintNode"/> class.
		/// </summary>
		/// <param name="condition">The condition.</param>
		/// <param name="description">An optional description (to be included in the error description).</param>
		/// <param name="issueCode">The optional issue code.</param>
		/// <param name="affectedComponent">The optional affected component
		/// (usually the field name when a constraint node is used to check one specific field,
		/// e.g. when checking gdb attribute rules), to be reported for errors</param>
		public ConstraintNode([NotNull] string condition,
		                      [CanBeNull] string description = null,
		                      [CanBeNull] IssueCode issueCode = null,
		                      [CanBeNull] string affectedComponent = null)
		{
			Assert.ArgumentNotNull(condition, nameof(condition));

			Condition = ExpressionUtils.ParseCaseSensitivityHint(
				condition,
				out _caseSensitivityOverride);

			Description = description;
			IssueCode = issueCode;
			AffectedComponent = affectedComponent;
			_nodes = new List<ConstraintNode>();
		}

		[NotNull]
		public string Condition { get; }

		[CanBeNull]
		public string Description { get; }

		[CanBeNull]
		public IssueCode IssueCode { get; }

		[CanBeNull]
		public string AffectedComponent { get; }

		[NotNull]
		public IList<ConstraintNode> Nodes => _nodes;

		public bool? CaseSensitivityOverride => _caseSensitivityOverride;

		internal TableView Helper { get; set; }

		public override string ToString()
		{
			return $"{nameof(Condition)}: {Condition}, " +
			       $"{nameof(Description)}: {Description}, " +
			       $"{nameof(IssueCode)}: {IssueCode}, " +
			       $"{nameof(AffectedComponent)}: {AffectedComponent}, " +
			       $"{nameof(Nodes)}: {Nodes.Count}, " +
			       $"{nameof(CaseSensitivityOverride)}: {CaseSensitivityOverride}";
		}
	}
}
