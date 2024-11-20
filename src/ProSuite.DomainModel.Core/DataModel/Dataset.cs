using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	// TODO: Consider renaming to DdxDataset to avoid conflict with ArcGIS.Core.Data.Dataset
	public abstract class Dataset : ModelElement, IDdxDataset, IDatasetDef
	{
		[UsedImplicitly] private string _aliasName;
		[UsedImplicitly] private string _abbreviation;
		[UsedImplicitly] private DatasetCategory _datasetCategory;
		[UsedImplicitly] private GeometryType _geometryType;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Dataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected Dataset() { }

		protected Dataset([NotNull] string name) : this(name, GetDefaultAbbreviation(name)) { }

		protected Dataset([NotNull] string name,
		                  [CanBeNull] string abbreviation)
			: this(name, abbreviation, string.Empty) { }

		protected Dataset([NotNull] string name,
		                  [CanBeNull] string abbreviation,
		                  [CanBeNull] string aliasName)
		{
			Name = name;
			Abbreviation = abbreviation;

			_aliasName = aliasName;
		}

		#endregion

		public virtual string TypeDescription => "Abstract Dataset";

		public override string DisplayName => string.IsNullOrEmpty(AliasName) ? Name : AliasName;

		[Required]
		public string Abbreviation
		{
			get { return _abbreviation; }
			set { _abbreviation = value; }
		}

		public string AliasName
		{
			get { return _aliasName; }
			set { _aliasName = value; }
		}

		public DatasetCategory DatasetCategory
		{
			get { return _datasetCategory; }
			set { _datasetCategory = value; }
		}

		public GeometryType GeometryType
		{
			get { return _geometryType; }
			set { _geometryType = value; }
		}

		#region Non-public members

		[NotNull]
		private static string GetDefaultAbbreviation([NotNull] string fullName)
		{
			// return ModelElementUtils.GetUnqualifiedName(fullName);
			return fullName;
			// otherwise the abbreviation is not guaranteed to be unique within the model
		}

		#endregion

		#region Implementation of IDbDataset

		public IDatasetContainer DbContainer => Model;

		public abstract DatasetType DatasetType { get; }

		/// <summary>
		/// An optional type code indicating the actual dataset implementation in systems with
		/// more branched dataset hierarchies.
		/// </summary>
		[CanBeNull]
		public virtual DatasetImplementationType ImplementationType => null;

		public bool Equals(IDatasetDef otherDataset)
		{
			if (otherDataset is Dataset otherObjectDataset)
			{
				return base.Equals(otherObjectDataset);
			}

			return false;
		}

		#endregion
	}
}
