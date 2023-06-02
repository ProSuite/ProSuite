using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class RasterMosaicDataset : Dataset, IRasterMosaicDataset
	{
		protected RasterMosaicDataset() { }

		protected RasterMosaicDataset([NotNull] string name) : base(name) { }

		protected RasterMosaicDataset([NotNull] string name,
		                              [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected RasterMosaicDataset([NotNull] string name,
		                              [CanBeNull] string abbreviation,
		                              [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		public override string TypeDescription => "Mosaic Dataset";

		public override DatasetType DatasetType => DatasetType.RasterMosaic;

		// TODO: Harvested or manually configure the following properties in the DDX
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
