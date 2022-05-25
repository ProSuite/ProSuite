using ProSuite.DdxEditor.Content.QA.QCon;

namespace ProSuite.DdxEditor.Content.Blazor;

// substitute with Prism
public abstract class Observable
{
	//protected Observable(object service)
	//{
	//	// T
	//}

	//protected Observable()
	//{

	//}

	protected void NotifyDirty()
	{
		DI.Get<QualityConditionPresenter>()?.NotifyChanged(true);
	}

	protected bool NotifyChanges<T>(T origin, T value)
	{
		if (Equals(origin, value))
		{
			return false;
		}

		NotifyDirty();
		return true;
	}
}
