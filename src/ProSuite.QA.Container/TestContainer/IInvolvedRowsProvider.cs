using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase.TableBased;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IInvolvedRowsProvider
	{
		IList<InvolvedRow> GetInvolvedRows(long uniqueId);
	}
}
