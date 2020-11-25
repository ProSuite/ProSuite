using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	public abstract class LinPair
	{
		public Lin L0 { get; }
		public Lin L1 { get; }

		protected LinPair([NotNull] Lin l0, [NotNull] Lin l1)
		{
			L0 = l0;
			L1 = l1;
		}

		private bool? _isParallel;

		public bool IsParallel
		{
			get { return _isParallel ?? (_isParallel = GetIsParallel()).Value; }
		}

		protected abstract bool GetIsParallel();
	}
}
