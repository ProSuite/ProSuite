using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.NamedValuesExpressions
{
	public class NamedValuesConjunctionExpression : NamedValuesExpression
	{
		[NotNull] private readonly List<NamedValues> _namedValuesCollection =
			new List<NamedValues>();

		public NamedValuesConjunctionExpression Add([NotNull] NamedValues namedValues)
		{
			Assert.ArgumentNotNull(namedValues, nameof(namedValues));

			_namedValuesCollection.Add(namedValues);

			return this;
		}

		[NotNull]
		public IEnumerable<NamedValues> NamedValuesCollection => _namedValuesCollection;
	}
}
