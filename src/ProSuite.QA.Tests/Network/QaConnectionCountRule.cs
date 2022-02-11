using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Network
{
	public class QaConnectionCountRule
	{
		public QaConnectionCountRule([NotNull] IReadOnlyTable table,
		                             [NotNull] string countSelectionExpression)
		{
			Table = table;
			CountSelectionExpression = countSelectionExpression;
		}

		[NotNull]
		public IReadOnlyTable Table { get; }

		[NotNull]
		public string CountSelectionExpression { get; }
	}
}
