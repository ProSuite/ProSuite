using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Network
{
	public class QaConnectionCountRule
	{
		[CLSCompliant(false)]
		public QaConnectionCountRule([NotNull] ITable table,
		                             [NotNull] string countSelectionExpression)
		{
			Table = table;
			CountSelectionExpression = countSelectionExpression;
		}

		[CLSCompliant(false)]
		[NotNull]
		public ITable Table { get; }

		[NotNull]
		public string CountSelectionExpression { get; }
	}
}
