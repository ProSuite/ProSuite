using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface ISimpleDatasetContext
	{
		bool CanOpen([NotNull] IDdxDataset dataset);

		[CanBeNull]
		IFeatureClass OpenFeatureClass([NotNull] IVectorDataset dataset);

		[CanBeNull]
		ITable OpenTable([NotNull] IObjectDataset dataset);

		[CanBeNull]
		IObjectClass OpenObjectClass([NotNull] IObjectDataset dataset);

		[CanBeNull]
		IRasterDataset OpenRasterDataset([NotNull] IDdxRasterDataset dataset);

		[CanBeNull]
		IRelationshipClass OpenRelationshipClass([NotNull] Association association);
	}
}
