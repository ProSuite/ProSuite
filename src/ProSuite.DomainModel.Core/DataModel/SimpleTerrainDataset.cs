using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class SimpleTerrainDataset : Dataset, ISimpleTerrainDataset
	{
		private static readonly string _geometryTypeName = "Terrain";

		[UsedImplicitly] private LayerFile _defaultSymbology;
		[UsedImplicitly] private double _pointDensity;

		private readonly IList<TerrainSourceDataset> _sources = new List<TerrainSourceDataset>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected SimpleTerrainDataset()
		{
			GeometryType = new GeometryTypeTerrain(_geometryTypeName);
		}

		protected SimpleTerrainDataset([NotNull] string name,
		                               [NotNull] IEnumerable<TerrainSourceDataset> sources)
			: base(name)
		{
			_sources = new List<TerrainSourceDataset>(sources);
			GeometryType = new GeometryTypeTerrain(_geometryTypeName);
		}

		#endregion

		public override string TypeDescription => "SimpleTerrain";

		public override DatasetType DatasetType => DatasetType.Terrain;

		#region ISimpleTerrainDataset Members

		[GreaterThanZero]
		public double PointDensity
		{
			get => _pointDensity;
			set => _pointDensity = value;
		}

		public IReadOnlyList<TerrainSourceDataset> Sources =>
			_sources as IReadOnlyList<TerrainSourceDataset> ??
			new List<TerrainSourceDataset>(_sources);

		public override DdxModel Model
		{
			get
			{
				if (base.Model != null)
				{
					return base.Model;
				}

				DdxModel result = null;
				foreach (var dataset in _sources.Select(nd => nd.Dataset))
				{
					if (result == null)
					{
						result = dataset.Model;
					}
					else
					{
						Assert.AreEqual(result, dataset.Model,
						                "The surface {0} contains datasets from different models",
						                Name);
					}
				}

				base.Model = result;
				return result;
			}
			set => base.Model = value;
		}

		#endregion

		public void AddSourceDataset(TerrainSourceDataset sourceDataset)
		{
			Assert.ArgumentNotNull(sourceDataset, nameof(sourceDataset));

			if (Sources.Count > 0 && sourceDataset.Dataset.Model.Id != ModelId)
			{
				throw new ArgumentException(
					"Cannot add a dataset to the surface from a different model than the existing datasets.");
			}

			_sources.Add(sourceDataset);
			Assert.AreEqual(ModelId, sourceDataset.Dataset.Model.Id, "Invalid ModelId");
		}

		public bool RemoveSourceDataset([NotNull] IVectorDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			TerrainSourceDataset sourceDataset =
				_sources.FirstOrDefault(ds => ds.Dataset.Equals(dataset));
			if (sourceDataset != null)
			{
				_sources.Remove(sourceDataset);
				return true;
			}

			return false;
		}

		public int ModelId => Model?.Id ?? -1;

		public IEnumerable<IDdxDataset> ContainedDatasets => Sources.Select(s => s.Dataset);

		#region ISpatialDataset Members

		public LayerFile DefaultLayerFile
		{
			get => _defaultSymbology;
			set => _defaultSymbology = value;
		}

		[UsedImplicitly]
		private LayerFile DefaultSymbology => _defaultSymbology;

		#endregion

		#region Equality members

		protected bool Equals(SimpleTerrainDataset other)
		{
			return base.Equals(other) &&
			       _pointDensity.Equals(other._pointDensity) &&
			       SourcesAreEqual(_sources, other._sources);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((SimpleTerrainDataset) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = base.GetHashCode();
				hashCode = (hashCode * 397) ^ _pointDensity.GetHashCode();
				hashCode = (hashCode * 397) ^ (_sources != null ? _sources.GetHashCode() : 0);
				return hashCode;
			}
		}

		private static bool SourcesAreEqual([NotNull] IList<TerrainSourceDataset> a,
		                                    [NotNull] IList<TerrainSourceDataset> b)
		{
			if (a.Count != b.Count)
			{
				return false;
			}

			for (int iSource = 0; iSource < a.Count; iSource++)
			{
				TerrainSourceDataset sourceA = a[iSource];
				TerrainSourceDataset sourceB = b[iSource];

				if (sourceA.SurfaceFeatureType != sourceB.SurfaceFeatureType)
				{
					return false;
				}

				if (! sourceA.Dataset.Equals(sourceB.Dataset))
				{
					return false;
				}

				if (sourceA.WhereClause != sourceB.WhereClause)
				{
					return false;
				}
			}

			return true;
		}

		#endregion
	}
}
