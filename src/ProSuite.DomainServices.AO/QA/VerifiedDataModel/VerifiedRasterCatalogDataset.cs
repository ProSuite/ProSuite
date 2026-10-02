using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	/// <summary>
	/// A harvested polygon feature class that acts as a raster catalog: one feature per tile, with
	/// the tile's raster file path in a field. The standalone counterpart of the DDX's elevation
	/// raster dataset - see <see cref="VerifiedFileCatalogDataset"/> for why it exists.
	/// </summary>
	public class VerifiedRasterCatalogDataset : VerifiedFileCatalogDataset, IRasterCatalogDataset
	{
		public VerifiedRasterCatalogDataset([NotNull] string name,
		                                    [NotNull] IList<DatasetFieldRole> fieldRoles)
			: base(name, fieldRoles) { }

		#region IRasterCatalogDataset

		IVectorDataset IRasterCatalogDataset.BoundaryDataset => null;

		string IRasterCatalogDataset.ZOrderFieldName => null;

		bool IRasterCatalogDataset.ZOrderDescending => false;

		string IRasterCatalogDataset.CellSizeFieldName => null;

		#endregion
	}
}
