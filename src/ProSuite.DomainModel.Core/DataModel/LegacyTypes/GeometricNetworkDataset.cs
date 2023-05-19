using ProSuite.Commons.Db;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.LegacyTypes
{
	/// <summary>
	/// Base class for the legacy geometric network. It must remain as entity because in the data
	/// dictionary datasets are not deleted but only flagged as deleted. Therefore even legacy
	/// entities must be mapped to an existing class even if it is never used any more.
	/// </summary>
	public abstract class GeometricNetworkDataset : Dataset, IGeometricNetworkDataset
	{
		private string _featureDatasetName;

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected GeometricNetworkDataset() { }

		protected GeometricNetworkDataset([NotNull] string name,
		                                  [NotNull] string featureDatasetName)
			: this(name, featureDatasetName, name) { }

		protected GeometricNetworkDataset([NotNull] string name,
		                                  [NotNull] string featureDatasetName,
		                                  [CanBeNull] string abbreviation)
			: base(name, abbreviation)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		protected GeometricNetworkDataset([NotNull] string name,
		                                  [NotNull] string featureDatasetName,
		                                  [CanBeNull] string abbreviation,
		                                  [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		#endregion

		public override string TypeDescription => "Geometric Network";

		public override DatasetType DatasetType => DatasetType.Unknown;

		public string FeatureDatasetName
		{
			get { return _featureDatasetName; }
			set
			{
				Assert.ArgumentNotNullOrEmpty(value, nameof(value));
				_featureDatasetName = value;
			}
		}
	}
}
