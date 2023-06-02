using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.ParameterTypes
{
	[UsedImplicitly]
	public class ConstraintNodeDefinition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConstraintNodeDefinition"/> class.
		/// </summary>
		/// <param name="condition">The condition.</param>
		/// <param name="description">An optional description (to be included in the error description).</param>
		/// <param name="issueCode">The optional issue code.</param>
		/// <param name="affectedComponent">The optional affected component
		/// (usually the field name when a constraint node is used to check one specific field,
		/// e.g. when checking gdb attribute rules), to be reported for errors</param>
		public ConstraintNodeDefinition([NotNull] string condition,
		                                [CanBeNull] string description = null,
		                                [CanBeNull] IssueCode issueCode = null,
		                                [CanBeNull] string affectedComponent = null)
		{
			Assert.ArgumentNotNull(condition, nameof(condition));

			Condition = condition;

			Description = description;
			IssueCode = issueCode;
			AffectedComponent = affectedComponent;
		}

		[NotNull]
		public string Condition { get; }

		[CanBeNull]
		public string Description { get; }

		[CanBeNull]
		public IssueCode IssueCode { get; }

		[CanBeNull]
		public string AffectedComponent { get; }

		public override string ToString()
		{
			return $"{nameof(Condition)}: {Condition}, " +
			       $"{nameof(Description)}: {Description}, " +
			       $"{nameof(IssueCode)}: {IssueCode}, " +
			       $"{nameof(AffectedComponent)}: {AffectedComponent}";
		}
	}
}
