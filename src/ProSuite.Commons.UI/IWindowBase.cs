namespace ProSuite.Commons.UI
{
	public interface IWindowBase<in T> where T : ViewModelBase
	{
		void ApplyCore(T viewModel);

		void Unload();
	}
}
