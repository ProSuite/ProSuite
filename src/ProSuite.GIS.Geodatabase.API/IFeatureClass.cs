using System.Collections.Generic;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IFeatureClass : IObjectClass
	{
		esriGeometryType ShapeType { get; }

		//esriFeatureType FeatureType { get; }

		string ShapeFieldName { get; }

		IField AreaField { get; }

		IField LengthField { get; }

		//IFeatureDataset FeatureDataset { get; }

		IFeature CreateFeature();

		IFeature GetFeature(long id);

		//IFeatureCursor GetFeatures(object fids, [In] bool Recycling);

		long FeatureClassID { get; }

		//IFeatureBuffer CreateFeatureBuffer();

		long FeatureCount(IQueryFilter queryFilter);

		IEnumerable<IFeature> Search(IQueryFilter filter, bool recycling);

		IEnumerable<IFeature> Update(IQueryFilter filter, bool recycling);

		IEnumerable<IFeature> Insert(bool useBuffering);

		ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer);
	}
}
