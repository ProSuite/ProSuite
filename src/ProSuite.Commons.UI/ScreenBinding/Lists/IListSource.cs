namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public interface IListSource
	{
		IPicklist GetList<T>();

		IPicklist GetList(string key);
	}
}
