using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.DataModel.LegacyTypes
{
	/// <summary>
	/// Base class for the Geodatabase terrain. It must remain as entity because in the data
	/// dictionary datasets are not deleted but only flagged as deleted.
	/// Additionally, starting with ArcGIS 3.5/11.5, terrain datasets are supported again and
	/// existing terrain datasets are honored again (with limited functionality).
	/// </summary>
	public class GdbTerrainDataset : Dataset, IGdbTerrainDataset
	{
		private string _featureDatasetName;

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected GdbTerrainDataset() { }

		public GdbTerrainDataset([NotNull] string name,
		                         [NotNull] string featureDatasetName)
			: this(name, featureDatasetName, name) { }

		public GdbTerrainDataset([NotNull] string name,
		                         [NotNull] string featureDatasetName,
		                         [CanBeNull] string abbreviation)
			: base(name, abbreviation)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		public GdbTerrainDataset([NotNull] string name,
		                         [NotNull] string featureDatasetName,
		                         [CanBeNull] string abbreviation,
		                         [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		#endregion

		public override string TypeDescription => "Geodatabase Terrain";

		public string FeatureDatasetName
		{
			get { return _featureDatasetName; }
			set
			{
				Assert.ArgumentNotNullOrEmpty(value, nameof(value));
				_featureDatasetName = value;
			}
		}

		public override DatasetType DatasetType => DatasetType.Terrain;

		public override DatasetImplementationType ImplementationType =>
			new DatasetImplementationType((int) DatasetType);
	}
}
