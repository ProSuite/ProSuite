using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ValidRelationConstraint : RowPairCondition, IValidRelationConstraint
	{
		private const bool _undefinedConditionIsFulfilled = false;

		public ValidRelationConstraint([CanBeNull] string constraint,
		                               bool constraintIsDirected,
		                               bool caseSensitive)
			: base(constraint,
			       constraintIsDirected,
			       _undefinedConditionIsFulfilled,
			       caseSensitive) { }

		public bool HasConstraint => Condition != null;
	}
}
