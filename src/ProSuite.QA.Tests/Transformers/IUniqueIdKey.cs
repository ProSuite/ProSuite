using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase.TableBased;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IUniqueIdKey
	{
		bool IsVirtuell { get; }

		IList<InvolvedRow> GetInvolvedRows();
	}
}
