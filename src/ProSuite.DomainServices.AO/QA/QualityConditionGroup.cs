using System.Collections.Generic;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public class QualityConditionGroup
	{
		public QualityConditionExecType ExecType { get; }

		public QualityConditionGroup(
			QualityConditionExecType execType,
			IDictionary<QualityCondition, IList<ITest>> qualityConditions = null)
		{
			ExecType = execType;
			QualityConditions = qualityConditions == null
				                    ? new Dictionary<QualityCondition, IList<ITest>>()
				                    : new Dictionary<QualityCondition, IList<ITest>>(
					                    qualityConditions);
		}

		public IDictionary<QualityCondition, IList<ITest>> QualityConditions { get; }
	}
}
