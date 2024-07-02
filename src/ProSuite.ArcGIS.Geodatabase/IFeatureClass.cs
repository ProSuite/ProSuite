using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;

namespace ESRI.ArcGIS.Geodatabase
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

		IFeature GetFeature(int id);

		//IFeatureCursor GetFeatures(object fids, [In] bool Recycling);

		int FeatureClassID { get; }

		//IFeatureBuffer CreateFeatureBuffer();

		int FeatureCount(IQueryFilter queryFilter);

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
