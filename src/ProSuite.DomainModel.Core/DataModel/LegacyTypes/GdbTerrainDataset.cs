using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.DataModel.LegacyTypes
{
	public class GdbTerrainDataset : Dataset, IGdbTerrainDataset
	{
		private string _featureDatasetName;

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected GdbTerrainDataset() { }

		protected GdbTerrainDataset([NotNull] string name,
		                            [NotNull] string featureDatasetName)
			: this(name, featureDatasetName, name) { }

		protected GdbTerrainDataset([NotNull] string name,
		                            [NotNull] string featureDatasetName,
		                            [CanBeNull] string abbreviation)
			: base(name, abbreviation)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		protected GdbTerrainDataset([NotNull] string name,
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
