using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	public abstract class Lin
	{
		public Pnt Ps { get; }
		public Pnt Pe { get; }

		protected Lin([NotNull] Pnt ps, [NotNull] Pnt pe)
		{
			Ps = ps;
			Pe = pe;
		}

		private Pnt _l;

		public Pnt L
		{
			get { return _l ?? (_l = Pe - Ps); }
		}
	}
}
