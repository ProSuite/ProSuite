using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class ErrorPolygonObject : ErrorVectorObject
	{
		internal ErrorPolygonObject([NotNull] IFeature feature,
		                            [NotNull] ErrorPolygonDataset dataset,
		                            [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache) { }

		public IPolygon Points
		{
			get { return (IPolygon) Feature.Shape; }
			set { Feature.Shape = value; }
		}
	}
}
