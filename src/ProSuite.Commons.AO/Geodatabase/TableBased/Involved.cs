using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase.TableBased
{
	public abstract class Involved
	{
		public abstract IEnumerable<InvolvedRow> EnumInvolvedRows();
	}
}
