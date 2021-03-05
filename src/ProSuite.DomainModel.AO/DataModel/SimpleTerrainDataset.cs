using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class SimpleTerrainDataset : Dataset, ISimpleTerrainDataset
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[UsedImplicitly] private LayerFile _defaultSymbology;
		[UsedImplicitly] private string _featureDatasetName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected SimpleTerrainDataset() { }

		protected SimpleTerrainDataset(IList<ITerrainSoure> sources)
		{
			Sources = sources;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="featureDatasetName">Name of the feature dataset that contains the terrain.</param>
		protected SimpleTerrainDataset([NotNull] string name, [NotNull] string featureDatasetName)
			: this(name, featureDatasetName, name) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="featureDatasetName">Name of the feature dataset that contains the terrain.</param>
		/// <param name="abbreviation">The dataset abbreviation.</param>
		protected SimpleTerrainDataset([NotNull] string name,
		                               [NotNull] string featureDatasetName,
		                               [CanBeNull] string abbreviation)
			: base(name, abbreviation)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="featureDatasetName">Name of the feature dataset.</param>
		/// <param name="abbreviation">The dataset abbreviation.</param>
		/// <param name="aliasName">Alias name for the dataset.</param>
		protected SimpleTerrainDataset([NotNull] string name,
		                               [NotNull] string featureDatasetName,
		                               [CanBeNull] string abbreviation,
		                               [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName)
		{
			Assert.ArgumentNotNullOrEmpty(featureDatasetName, nameof(featureDatasetName));

			FeatureDatasetName = featureDatasetName;
		}

		#endregion

		public override string TypeDescription => "Terrain";
		public int TerrainDefId { get; protected set; } = -1;

		public string FeatureDatasetName
		{
			get { return _featureDatasetName; }
			set
			{
				Assert.ArgumentNotNullOrEmpty(value, nameof(value));
				_featureDatasetName = value;
			}
		}

		public IList<ITerrainSoure> Sources { get; }

		#region ISpatialDataset Members

		public LayerFile DefaultLayerFile
		{
			get { return _defaultSymbology; }
			set { _defaultSymbology = value; }
		}

		[UsedImplicitly]
		private LayerFile DefaultSymbology => _defaultSymbology;

		#endregion
	}
}
