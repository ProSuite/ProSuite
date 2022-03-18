using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPicker
	{
		Task<IPickableItem> PickSingle();

		Task<IList<IPickableItem>> PickMany();
	}
}
