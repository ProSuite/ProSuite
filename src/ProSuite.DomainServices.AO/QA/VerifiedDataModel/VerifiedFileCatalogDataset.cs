using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	/// <summary>
	/// A harvested polygon feature class that acts as a file catalog: one feature per tile, with
	/// the tile's file path in a field.
	/// <para>
	/// This is the standalone counterpart of the DDX's catalog datasets. A harvested model cannot
	/// tell a catalog from any other polygon feature class, so the fact - and the name of the
	/// file-path field - is transported with the dataset parameter value and applied here. The
	/// field names come straight from the transported roles rather than from harvested attributes,
	/// because the standalone paths do not necessarily harvest attributes at all.
	/// </para>
	/// </summary>
	public abstract class VerifiedFileCatalogDataset : VerifiedVectorDataset, IFileCatalogDataset
	{
		[NotNull] private readonly IList<DatasetFieldRole> _fieldRoles;

		protected VerifiedFileCatalogDataset([NotNull] string name,
		                                     [NotNull] IList<DatasetFieldRole> fieldRoles)
			: base(name)
		{
			Assert.ArgumentNotNull(fieldRoles, nameof(fieldRoles));

			Assert.ArgumentCondition(
				fieldRoles.Any(fieldRole => fieldRole.Role.Equals(AttributeRole.FilePath)),
				$"No field with the {AttributeRole.GetSimpleName(AttributeRole.FilePath)} role " +
				$"for file catalog {name}", nameof(fieldRoles));

			_fieldRoles = fieldRoles;
		}

		/// <summary>
		/// The roles this dataset was configured with, for diagnostics and for the roles that do
		/// not (yet) have a corresponding property on the catalog interfaces.
		/// </summary>
		[NotNull]
		public IEnumerable<DatasetFieldRole> FieldRoles => _fieldRoles;

		#region IFileCatalogDataset

		IVectorDataset IFileCatalogDataset.CatalogDataset => this;

		string IFileCatalogDataset.FilePathFieldName =>
			Assert.NotNull(GetFieldName(AttributeRole.FilePath));

		#endregion

		[CanBeNull]
		protected string GetFieldName([NotNull] AttributeRole role)
		{
			return _fieldRoles.FirstOrDefault(fieldRole => fieldRole.Role.Equals(role))?.FieldName;
		}
	}
}
