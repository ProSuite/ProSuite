using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class LinPair3D : LinPair
	{
		public LinPair3D([NotNull] Lin3D l0, [NotNull] Lin3D l1)
			: base(l0, l1) { }

		protected override bool GetIsParallel()
		{
			bool isParallel = (L0xl12 == 0);

			return isParallel;
		}

		private Pnt3D _l0xl1;

		[NotNull]
		public Pnt3D L0xl1
		{
			get { return _l0xl1 ?? (_l0xl1 = ((Pnt3D) L0.L).VectorProduct((Pnt3D) L1.L)); }
		}

		private double? _l0xl12;

		public double L0xl12
		{
			get { return _l0xl12 ?? (_l0xl12 = L0xl1 * L0xl1).Value; }
		}
	}
}
