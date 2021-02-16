using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class VectorObject : ObjectObject
	{
		protected VectorObject([NotNull] IFeature feature,
		                       [NotNull] VectorDataset dataset,
		                       [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache)
		{
			Feature = feature;
		}

		[NotNull]
		public IFeature Feature { get; }

		public IGeometry ShapeCopy => Feature.ShapeCopy;

		public IGeometry Shape
		{
			get { return Feature.Shape; }
			set { Feature.Shape = value; }
		}
	}
}
