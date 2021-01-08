#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	[CLSCompliant(false)]
	public class SimpleDatasetOpener : IOpenDataset
	{
		private readonly IDatasetContext _datasetContext;

		[CLSCompliant(false)]
		public SimpleDatasetOpener(IDatasetContext datasetContext)
		{
			_datasetContext = datasetContext;
		}

		public bool CanOpen(IDdxDataset dataset)
		{
			if (dataset is IObjectDataset ||
			    dataset is IDdxRasterDataset)

			{
				return _datasetContext.CanOpen(dataset);
			}

			return false;
		}

		public object OpenDataset(IDdxDataset dataset, Type dataType)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			if (typeof(IFeatureClass) == dataType)
				return _datasetContext.OpenFeatureClass((IVectorDataset) dataset);

			if (typeof(ITable) == dataType)
				return _datasetContext.OpenTable((IObjectDataset) dataset);

			if (typeof(IMosaicDataset) == dataType)
				return (IMosaicDataset) _datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset) == dataType)
				return _datasetContext.OpenRasterDataset((IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset2) == dataType)
				return (IRasterDataset2) _datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			throw new ArgumentException($"Unsupported data type {dataType}");
		}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return _datasetContext.OpenRelationshipClass(association);
		}
	}
}
