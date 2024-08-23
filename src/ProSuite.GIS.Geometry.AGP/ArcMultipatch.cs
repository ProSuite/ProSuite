using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;

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
	}
}
