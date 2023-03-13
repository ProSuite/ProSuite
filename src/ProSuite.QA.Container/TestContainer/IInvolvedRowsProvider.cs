using System.Collections.Generic;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IInvolvedRowsProvider
	{
		IList<InvolvedRow> GetInvolvedRows(long uniqueId);
	}
}
