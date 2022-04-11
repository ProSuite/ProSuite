namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IViewObserver
	{
		void NotifyChanged(bool dirty);
	}
}
