using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase.TablesBased;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IInvolvedRowsProvider
	{
		IList<InvolvedRow> GetInvolvedRows(long uniqueId);
	}
}
