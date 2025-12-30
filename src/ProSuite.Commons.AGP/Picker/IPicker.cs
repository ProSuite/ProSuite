using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProSuite.Commons.AGP.Picker;

public interface IPicker
{
	Task<IPickableItem> PickSingle();

	Task<IList<IPickableItem>> PickMany();
}
