using System.Collections.Generic;

namespace ProSuite.QA.Container
{
	public abstract class Involved
	{
		public abstract IEnumerable<InvolvedRow> EnumInvolvedRows();
	}
}
