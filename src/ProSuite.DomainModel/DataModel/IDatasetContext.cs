using System;
using ArcGIS.Core.Data;
using EsriDE.ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.DataModel
{
	[CLSCompliant(false)]
	public interface IDatasetContext : IDisposable
	{
		FeatureClass OpenFeatureClass(IVectorDataset dataset);
		Table OpenTable(IObjectDataset dataset);
	}
}
