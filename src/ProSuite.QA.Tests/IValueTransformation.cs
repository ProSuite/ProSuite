using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public interface IValueTransformation
	{
		[CanBeNull]
		object TransformValue([NotNull] IReadOnlyRow row, [CanBeNull] object value);
	}
}
