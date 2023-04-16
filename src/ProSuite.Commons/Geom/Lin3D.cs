
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class Lin3D : Lin
	{
		public Lin3D([NotNull] Pnt ps, [NotNull] Pnt pe)
			: base(ps, pe) { }
	}
}
