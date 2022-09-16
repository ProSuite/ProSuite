namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IObservedView<T> where T : IViewObserver
	{
		T Observer { get; set; }
	}
}
