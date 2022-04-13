using System;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public interface IDatasetView : IBoundView<Dataset, IDatasetObserver>
	{
		Func<object> FindDatasetCategoryDelegate { get; set; }
	}
}
