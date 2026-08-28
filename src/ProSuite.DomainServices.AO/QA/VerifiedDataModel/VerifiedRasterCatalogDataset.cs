using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	/// <summary>
	/// A harvested polygon feature class that acts as a raster catalog: one feature per tile, with
	/// the tile's raster file path in a field.
	/// <para>
	/// This is the standalone counterpart of the DDX's elevation raster dataset. A harvested model
	/// cannot tell a catalog from any other polygon feature class, so the fact - and the name of
	/// the file-path field - is transported with the dataset parameter value and applied here. The
	/// field names come straight from the transported roles rather than from harvested attributes,
	/// because the standalone paths do not necessarily harvest attributes at all.
	/// </para>
	/// </summary>
	public class VerifiedRasterCatalogDataset : VerifiedVectorDataset, IRasterCatalogDataset
	{
		[NotNull] private readonly IList<DatasetFieldRole> _fieldRoles;

		public VerifiedRasterCatalogDataset([NotNull] string name,
		                                    [NotNull] IList<DatasetFieldRole> fieldRoles)
			: base(name)
		{
			Assert.ArgumentNotNull(fieldRoles, nameof(fieldRoles));

			Assert.ArgumentCondition(
				fieldRoles.Any(fieldRole => fieldRole.Role.Equals(AttributeRole.FilePath)),
				$"No field with the {AttributeRole.GetSimpleName(AttributeRole.FilePath)} role " +
				$"for raster catalog {name}", nameof(fieldRoles));

			_fieldRoles = fieldRoles;
		}

		/// <summary>
		/// The roles this dataset was configured with, for diagnostics and for the roles that do
		/// not (yet) have a corresponding property on <see cref="IRasterCatalogDataset"/>.
		/// </summary>
		[NotNull]
		public IEnumerable<DatasetFieldRole> FieldRoles => _fieldRoles;

		#region IRasterCatalogDataset

		IVectorDataset IRasterCatalogDataset.CatalogDataset => this;

		string IRasterCatalogDataset.FilePathFieldName =>
			Assert.NotNull(GetFieldName(AttributeRole.FilePath));

		IVectorDataset IRasterCatalogDataset.BoundaryDataset => null;

		string IRasterCatalogDataset.ZOrderFieldName => null;

		bool IRasterCatalogDataset.ZOrderDescending => false;

		string IRasterCatalogDataset.CellSizeFieldName => null;

		#endregion

		[CanBeNull]
		private string GetFieldName([NotNull] AttributeRole role)
		{
			return _fieldRoles.FirstOrDefault(fieldRole => fieldRole.Role.Equals(role))?.FieldName;
		}
	}
}
