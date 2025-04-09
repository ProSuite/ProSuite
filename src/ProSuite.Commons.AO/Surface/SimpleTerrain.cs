using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Surface
{
	public class SimpleTerrain : TerrainReference, IReadOnlyDataset, IReadOnlyGeoDataset,
	                             ITerrainDef
	{
		private IName _fullName;

		private readonly ISpatialReference _spatialReference;
		private IEnvelope _extent;

		public SimpleTerrain([NotNull] string name,
		                     [NotNull] IList<SimpleTerrainDataSource> dataSources,
		                     double pointDensity,
		                     [CanBeNull] RectangularTilingStructure tiling)
		{
			Assert.NotNullOrEmpty(name, nameof(name));
			Assert.ArgumentCondition(dataSources.Count > 0, "No data sources");

			Name = name;
			DataSources = dataSources;
			PointDensity = pointDensity;

			Assert.ArgumentCondition(dataSources.Count > 0,
			                         "No data sources provided for terrain {0}", name);

			IFeatureClass firstClass = DataSources[0].FeatureClass;

			_spatialReference = DatasetUtils.GetSpatialReference(firstClass);

			Tiling = tiling ?? SuggestTiling(pointDensity);
		}

		private RectangularTilingStructure SuggestTiling(double pointDensity)
		{
			Assert.True(pointDensity > 0, "Point density must be > 0");

			double tileArea = 200000 / pointDensity;

			double tileWidth = Math.Sqrt(tileArea);

			double xMin, yMin;

			if (! Extent.IsEmpty)
			{
				xMin = Extent.XMin;
				yMin = Extent.YMin;
			}
			else
			{
				SpatialReference.GetDomain(out xMin, out yMin, out _, out _);
			}

			return new RectangularTilingStructure(xMin, yMin, tileWidth, tileWidth,
			                                      BorderPointTileAllocationPolicy.BottomLeft,
			                                      SpatialReference);
		}

		public double PointDensity { get; set; }

		public Func<IEnvelope, int> EstimatePointsFunc { get; set; }

		public IList<SimpleTerrainDataSource> DataSources { get; }

		public override IReadOnlyGeoDataset Dataset => this;

		public override RectangularTilingStructure Tiling { get; }

		public override string Name { get; }

		public override ITin CreateTin(IEnvelope extent, double resolution)
		{
			double expansion = Tiling.TileWidth / 2;
			FeatureTinGenerator tinGenerator = new FeatureTinGenerator(this, expansion);

			tinGenerator.AllowIncompleteInterpolationDomainAtBoundary = true;

			return tinGenerator.GenerateTin(extent);
		}

		#region IDataset Members

		[NotNull]
		string IReadOnlyDataset.Name => Name;

		IName IReadOnlyDataset.FullName
		{
			get
			{
				if (_fullName == null)
				{
					_fullName = new SimpleFeatureTerrainName(this);
				}

				return _fullName;
			}
		}

		IWorkspace IReadOnlyDataset.Workspace => ((IDataset) DataSources[0].FeatureClass).Workspace;

		#endregion

		#region IGeoDataset Members

		public ISpatialReference SpatialReference => _spatialReference;

		public IEnvelope Extent
		{
			get
			{
				if (_extent == null)
				{
					foreach (var featureClass in DataSources.Select(ds => ds.FeatureClass))
					{
						if (_extent == null)
						{
							_extent = ((IGeoDataset) featureClass).Extent;
						}
						else
						{
							_extent.Union(((IGeoDataset) featureClass).Extent);
						}
					}
				}

				return _extent;
			}
		}

		#endregion

		#region Implementation of IDbDataset

		public IDatasetContainer DbContainer
		{
			get
			{
				IWorkspace workspace = ((IReadOnlyDataset) this).Workspace;
				return new GeoDbWorkspace(workspace);
			}
		}

		public DatasetType DatasetType => DatasetType.Terrain;

		public bool Equals(IDatasetDef otherDataset)
		{
			if (otherDataset is SimpleTerrain simpleTerrain)
			{
				return EqualsCore(simpleTerrain);
			}

			return false;
		}

		#endregion

		public int GetPointCount(IEnvelope aoi)
		{
			if (EstimatePointsFunc != null)
			{
				return EstimatePointsFunc(aoi);
			}

			return (int) (PointDensity * aoi.Width * aoi.Height);
		}

		public override string ToString()
		{
			string result = $"SimpleTerrain:{Environment.NewLine}" +
			                $"Name: {Name}{Environment.NewLine}" +
			                $"Point Density: {PointDensity}";

			foreach (var dataSource in DataSources)
			{
				result += $"{Environment.NewLine}{dataSource}";
			}

			return result;
		}

		public override bool EqualsCore(TerrainReference terrainReference)
		{
			if (ReferenceEquals(this, terrainReference))
			{
				return true;
			}

			if (! (terrainReference is SimpleTerrain other))
			{
				return false;
			}

			if (DataSources.Count != other.DataSources.Count)
			{
				return false;
			}

			for (int i = 0; i < DataSources.Count; i++)
			{
				if (! DataSources[i].Equals(other.DataSources[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCodeCore()
		{
			return DataSources[0].GetHashCode();
		}

		#region Nested class GdbTableName

		private class SimpleFeatureTerrainName : IName, IDatasetName
		{
			[NotNull] private readonly IReadOnlyDataset _simpleTerrain;

			public SimpleFeatureTerrainName([NotNull] IReadOnlyDataset simpleTerrain)
			{
				_simpleTerrain = simpleTerrain;
				Name = simpleTerrain.Name;

				IWorkspace workspace = _simpleTerrain.Workspace;
				WorkspaceName = (IWorkspaceName) ((IDataset) workspace).FullName;
			}

			#region IName members

			public object Open()
			{
				return _simpleTerrain;
			}

			public string NameString { get; set; }

			#endregion

			#region IDatasetName members

			public string Name { get; set; }

			public esriDatasetType Type => esriDatasetType.esriDTTerrain;

			public string Category { get; set; }

			public IWorkspaceName WorkspaceName { get; set; }

			public IEnumDatasetName SubsetNames => throw new NotImplementedException();

			#endregion
		}

		#endregion
	}
}
