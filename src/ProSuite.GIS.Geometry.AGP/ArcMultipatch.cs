using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcMultipatch : ArcGeometry, IMultiPatch
	{
		private readonly Multipatch _proMultipatch;

		public ArcMultipatch(Multipatch proMultipatch) : base(proMultipatch)
		{
			_proMultipatch = proMultipatch;
		}

		#region Implementation of IGeometryCollection

		public int GeometryCount => _proMultipatch.PartCount;

		public IGeometry get_Geometry(int index)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Implementation of IMultiPatch

		public IGeometry XYFootprint { get; set; }

		public void InvalXYFootprint()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Overrides of ArcGeometry

		public override IGeometry Clone()
		{
			return new ArcMultipatch((Multipatch) _proMultipatch.Clone());
		}

		#endregion
	}
}
