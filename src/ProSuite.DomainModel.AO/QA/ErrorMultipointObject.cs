using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class ErrorMultipointObject : ErrorVectorObject
	{
		internal ErrorMultipointObject([NotNull] IFeature feature,
		                               [NotNull] ErrorMultipointDataset dataset,
		                               [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base(feature, dataset, fieldIndexCache) { }

		[CLSCompliant(false)]
		public IMultipoint Points
		{
			get { return (IMultipoint) Feature.Shape; }
			set { Feature.Shape = value; }
		}
	}
}
