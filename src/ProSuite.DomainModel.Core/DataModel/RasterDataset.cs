using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class RasterDataset : Dataset, IDdxRasterDataset
	{
		protected RasterDataset() { }

		protected RasterDataset([NotNull] string name) : base(name) { }

		protected RasterDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected RasterDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation,
		                        [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		public override string TypeDescription => "Raster Dataset";
	}
}
