using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class SimpleRasterMosaicDataset : Dataset, ISimpleRasterMosaicDataset
	{
		protected SimpleRasterMosaicDataset() { }

		protected SimpleRasterMosaicDataset([NotNull] string name) : base(name) { }

		protected SimpleRasterMosaicDataset([NotNull] string name,
		                                    [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected SimpleRasterMosaicDataset([NotNull] string name,
		                                    [CanBeNull] string abbreviation,
		                                    [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		public override string TypeDescription => "Mosaic Dataset";
	}
}
