using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ModelSimpleTerrainDataset : SimpleTerrainDataset { }

	public abstract class SimpleTerrainDataset : Dataset, ISimpleTerrainDataset
	{
		public class Comparer : IEqualityComparer<SimpleTerrainDataset>
		{
			bool IEqualityComparer<SimpleTerrainDataset>.Equals(
				SimpleTerrainDataset x, SimpleTerrainDataset y)
			{
				if (x == null || y == null)
				{
					return false;
				}

				if (x == y)
				{
					return true;
				}

				//if (x.Name != y.Name)
				//{
				//	return false;
				//}
				if (x.PointDensity != y.PointDensity)
				{
					return false;
				}

				if (x.Sources.Count != y.Sources.Count)
				{
					return false;
				}

				for (int iSource = 0; iSource < x.Sources.Count; iSource++)
				{
					TerrainSourceDataset xSource = x.Sources[iSource];
					TerrainSourceDataset ySource = y.Sources[iSource];

					if (xSource.SurfaceFeatureType != ySource.SurfaceFeatureType)
					{
						return false;
					}

					if (xSource.Dataset != ySource.Dataset)
					{
						return false;
					}

					if (xSource.WhereClause != ySource.WhereClause)
					{
						return false;
					}
				}

				return true;
			}

			int IEqualityComparer<SimpleTerrainDataset>.GetHashCode(SimpleTerrainDataset obj)
			{
				if (obj.Sources.Count > 0)
				{
					return obj.Sources[0].Dataset.GetHashCode() +
					       29 * obj.Sources.Count.GetHashCode();
				}

				return obj.PointDensity.GetHashCode();
			}
		}

		private static readonly string _geometryTypeName = "Terrain";

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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

		protected SimpleTerrainDataset(IList<TerrainSourceDataset> sources)
		{
			_sources = new List<TerrainSourceDataset>(sources);
			GeometryType = new GeometryTypeTerrain(_geometryTypeName);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		protected SimpleTerrainDataset([NotNull] string name)
			: this(name, name) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTerrainDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="abbreviation">The dataset abbreviation.</param>
		/// <param name="aliasName">Alias name for the dataset.</param>
		protected SimpleTerrainDataset([NotNull] string name,
		                               [CanBeNull] string abbreviation,
		                               [CanBeNull] string aliasName = null)
			: base(name, abbreviation, aliasName)
		{
			GeometryType = new GeometryTypeTerrain(_geometryTypeName);
		}

		#endregion

		public override string TypeDescription => "SimpleTerrain";

		#region ISimpleTerrainDataset Members

		public int TerrainId => Id;

		//TODO : set Abbreviation
		public string NameAndAbbr
		{
			get => Name;
			set
			{
				Name = value;
				Abbreviation = value;
			}
		}

		public double PointDensity
		{
			get => _pointDensity;
			set => _pointDensity = value;
		}

		public IReadOnlyList<TerrainSourceDataset> Sources =>
			_sources as IReadOnlyList<TerrainSourceDataset> ??
			new List<TerrainSourceDataset>(_sources);

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

		public int ModelId => Model?.Id ?? -1;

		#endregion

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
	}

	public class XmlSimpleTerrainDataset
	{
		public string Name { get; set; }
		public double PointDensity { get; set; }
		public List<XmlTerrainSource> Sources { get; set; }
	}

	public class XmlTerrainSource
	{
		public string Dataset { get; set; }
		public TinSurfaceType Type { get; set; }
	}
}
