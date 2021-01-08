using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class ErrorMultiPatchObject : ErrorVectorObject
	{
		internal ErrorMultiPatchObject([NotNull] IFeature feature,
		                               [NotNull] ErrorMultiPatchDataset dataset,
		                               [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache) { }

		[CLSCompliant(false)]
		public IMultiPatch MultiPatch
		{
			get { return (IMultiPatch) Feature.Shape; }
			set { Feature.Shape = value; }
		}
	}
}
