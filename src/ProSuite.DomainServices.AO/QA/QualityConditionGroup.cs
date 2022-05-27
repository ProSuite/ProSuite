
using ProSuite.DomainModel.Core.QA;
using System.Collections.Generic;

namespace ProSuite.DomainServices.AO.QA
{
	public class QualityConditionGroup
	{
		private readonly QualityConditionExecType _execType;

		public QualityConditionGroup(QualityConditionExecType execType, IEnumerable<QualityCondition> tests = null)
		{
			_execType = execType;
			QualityConditions = new List<QualityCondition>();
			if (tests != null)
			{
				QualityConditions.AddRange(tests);
			}
		}
		public List<QualityCondition> QualityConditions { get; }
	}
}
