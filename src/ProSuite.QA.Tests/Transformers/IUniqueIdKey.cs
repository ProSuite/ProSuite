using System.Collections.Generic;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IUniqueIdKey
	{
		bool IsVirtuell { get; }

		IList<InvolvedRow> GetInvolvedRows();
	}
}
