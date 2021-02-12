using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class ErrorLineObject : ErrorVectorObject
	{
		internal ErrorLineObject([NotNull] IFeature feature,
		                         [NotNull] ErrorLineDataset dataset,
		                         [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache) { }

		public IPolyline Line
		{
			get { return (IPolyline) Feature.Shape; }
			set { Feature.Shape = value; }
		}
	}
}
