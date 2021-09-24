using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.IssueFilters
{
	public class IfInvolvedRows : IssueFilter
	{
		private readonly string _constraint;

		public IfInvolvedRows(string constraint)
			: base(new ITable[] { })
		{
			_constraint = constraint;
		}

		public override bool Cancel(QaError error)
		{
			return false;
		}
	}
}
