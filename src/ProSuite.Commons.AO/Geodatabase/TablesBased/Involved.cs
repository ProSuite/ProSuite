using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase.TablesBased
{
	public abstract class Involved
	{
		public abstract IEnumerable<InvolvedRow> EnumInvolvedRows();
	}
}
