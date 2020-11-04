using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Geometry.ChangeAlong
{
	public class ExtentProviderMock : IExtentProvider
	{
		private readonly ISpatialReference _spatialReference;
		private readonly IEnvelope _fixedExtent;

		public ExtentProviderMock([NotNull] ISpatialReference spatialReference)
		{
			_spatialReference = spatialReference;
		}

		public ExtentProviderMock([NotNull] IEnvelope fixedExtent)
		{
			_fixedExtent = fixedExtent;
		}

		[NotNull]
		private IEnvelope GetDomain()
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			_spatialReference.GetDomain(out xMin, out xMax, out yMin, out yMax);

			return GeometryFactory.CreateEnvelope(xMin, yMin, xMax, yMax);
		}

		#region Implementation of IExtentProvider

		public IEnvelope GetCurrentExtent()
		{
			return _fixedExtent ?? GetDomain();
		}

		public IEnumerable<IEnvelope> GetVisibleLensWindowExtents()
		{
			yield return GetCurrentExtent();
		}

		#endregion
	}
}
