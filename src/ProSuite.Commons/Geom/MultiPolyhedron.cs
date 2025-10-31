using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class MultiPolyhedron : IBoundedXY
	{
		private readonly List<Polyhedron> _polyhedra;
		private readonly EnvelopeXY _boundingBox;

		public MultiPolyhedron(IEnumerable<Polyhedron> polyhedra)
		{
			_polyhedra = polyhedra.ToList();

			_boundingBox = GetBoundingBox();
		}

		public IReadOnlyCollection<Polyhedron> Polyhedra => _polyhedra.AsReadOnly();

		[CanBeNull]
		private EnvelopeXY GetBoundingBox()
		{
			EnvelopeXY env = null;
			foreach (Polyhedron polyhedron in _polyhedra)
			{
				if (env == null)
				{
					env = new EnvelopeXY(polyhedron);
				}
				else
				{
					env.EnlargeToInclude(polyhedron);
				}
			}

			return env;
		}

		#region Implementation of IBoundedXY

		public double XMin => _boundingBox?.XMin ?? double.NaN;
		public double YMin => _boundingBox?.YMin ?? double.NaN;
		public double XMax => _boundingBox?.XMax ?? double.NaN;
		public double YMax => _boundingBox?.YMax ?? double.NaN;

		#endregion
	}
}
