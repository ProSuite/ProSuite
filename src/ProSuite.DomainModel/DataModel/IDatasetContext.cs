using System;
using ArcGIS.Core.Data;

namespace ProSuite.DomainModel.DataModel
{
	public interface IDatasetContext : IDisposable
	{
		FeatureClass OpenFeatureClass(IVectorDataset dataset);
		Table OpenTable(IObjectDataset dataset);
	}
}
