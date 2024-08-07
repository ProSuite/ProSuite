
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
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
