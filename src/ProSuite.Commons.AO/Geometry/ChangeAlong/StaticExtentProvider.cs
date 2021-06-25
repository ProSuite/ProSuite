using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class StaticExtentProvider : IExtentProvider
	{
		private readonly List<IEnvelope> _extents;

		public StaticExtentProvider([NotNull] IEnvelope visibleExtent)
		{
			_extents = new List<IEnvelope> {visibleExtent};
		}

		public IEnvelope GetCurrentExtent()
		{
			return _extents[0];
		}

		public IEnumerable<IEnvelope> GetVisibleLensWindowExtents()
		{
			return _extents;
		}
	}
}
