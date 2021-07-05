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

		// TODO: Harvested or manually configure the following prperties in the DDX
		// -> No separate DTO modelling is needed for VirtualModelContext!
		// TODO: Separate hierachies Simple vs GdbMosaic, move these properties up to interface

		//public VectorDataset CatalogDataset { get; set; }

		//public VectorDataset BoundaryDataset { get; set; }

		//public string MosaicRuleZOrderFieldName { get; set; }

		//public bool MosaicRuleDescending { get; set; }

		//public string CellSizeFieldName { get; set; }

		//public string RasterPathFieldName { get; set; }
	}
}
