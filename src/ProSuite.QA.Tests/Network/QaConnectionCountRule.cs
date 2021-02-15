using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Network
{
	public class QaConnectionCountRule
	{
		public QaConnectionCountRule([NotNull] ITable table,
		                             [NotNull] string countSelectionExpression)
		{
			Table = table;
			CountSelectionExpression = countSelectionExpression;
		}

		[NotNull]
		public ITable Table { get; }

		[NotNull]
		public string CountSelectionExpression { get; }
	}
}
