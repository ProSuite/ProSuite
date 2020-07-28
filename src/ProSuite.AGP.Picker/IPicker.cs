using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProSuite.AGP.Picker
{
	public interface IPicker
	{
		Task<IPickableItem> PickSingle();

		Task<List<IPickableItem>> PickMany();
		
	}
}
