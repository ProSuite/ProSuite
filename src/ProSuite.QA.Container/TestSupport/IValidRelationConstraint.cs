using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public interface IValidRelationConstraint
	{
		bool HasConstraint { get; }

		bool IsFulfilled([NotNull] IReadOnlyRow row1, int tableIndex1,
		                 [NotNull] IReadOnlyRow row2, int tableIndex2,
		                 [NotNull] out string conditionMessage);
	}
}
