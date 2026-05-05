using System.Threading.Tasks;

namespace ProSuite.Commons.UI
{
	public interface IWindowBase<in T> where T : ViewModelBase
	{
		void Unload();

		void ApplyCore(T viewModel);

		Task ShowDialogAsync(IWindowPositioner positioner);
	}
}
