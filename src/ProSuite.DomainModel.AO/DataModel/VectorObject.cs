using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class VectorObject : ObjectObject
	{
		[CLSCompliant(false)]
		protected VectorObject([NotNull] IFeature feature,
		                       [NotNull] VectorDataset dataset,
		                       [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache)
		{
			Feature = feature;
		}

		[CLSCompliant(false)]
		[NotNull]
		public IFeature Feature { get; }

		[CLSCompliant(false)]
		public IGeometry ShapeCopy => Feature.ShapeCopy;

		[CLSCompliant(false)]
		public IGeometry Shape
		{
			get { return Feature.Shape; }
			set { Feature.Shape = value; }
		}
	}
}
