using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.NamedValuesExpressions
{
	public class SimpleNamedValuesExpression : NamedValuesExpression
	{
		public SimpleNamedValuesExpression([NotNull] NamedValues namedValues)
		{
			Assert.ArgumentNotNull(namedValues, nameof(namedValues));

			NamedValues = namedValues;
		}

		[NotNull]
		public NamedValues NamedValues { get; }
	}
}