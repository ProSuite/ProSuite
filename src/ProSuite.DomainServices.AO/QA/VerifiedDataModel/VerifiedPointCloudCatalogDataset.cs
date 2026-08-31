using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	/// <summary>
	/// A harvested polygon feature class that acts as a point cloud catalog: one feature per tile,
	/// with the tile's LAS file path in a field. The standalone counterpart of the DDX's point
	/// cloud dataset - see <see cref="VerifiedFileCatalogDataset"/> for why it exists.
	/// </summary>
	/// <remarks>
	/// Identical in shape to <see cref="VerifiedRasterCatalogDataset"/>, and deliberately a
	/// separate type: the marker interface it carries is what decides which test parameters it may
	/// be bound to, and a LAS catalog is not a valid raster mosaic source.
	/// </remarks>
	public class VerifiedPointCloudCatalogDataset : VerifiedFileCatalogDataset,
	                                                IPointCloudCatalogDataset
	{
		public VerifiedPointCloudCatalogDataset([NotNull] string name,
		                                        [NotNull] IList<DatasetFieldRole> fieldRoles)
			: base(name, fieldRoles) { }
	}
}
