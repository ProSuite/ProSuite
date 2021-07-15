using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class Lin3D : Lin
	{
		public Lin3D([NotNull] Pnt ps, [NotNull] Pnt pe)
			: base(ps, pe) { }
	}
}
