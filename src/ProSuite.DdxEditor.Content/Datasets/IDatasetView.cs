using System;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public interface IDatasetView : IBoundView<Dataset, IDatasetObserver>
	{
		Func<object> FindDatasetCategoryDelegate { get; set; }
	}
}