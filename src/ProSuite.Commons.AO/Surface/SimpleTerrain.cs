using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public class SimpleTerrain : TerrainReference, IDataset, IGeoDataset
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

			Tiling = tiling ?? SuggestTiling(pointDensity);

			IFeatureClass firstClass = DataSources[0].FeatureClass;

			_spatialReference = DatasetUtils.GetSpatialReference(firstClass);
		}

		private RectangularTilingStructure SuggestTiling(double pointDensity)
		{
			Assert.True(pointDensity > 0, "Point density must be > 0");

			double tileArea = 200000 / pointDensity;

			double tileWidth = Math.Sqrt(tileArea);

			return new RectangularTilingStructure(Extent.XMin, Extent.YMin, tileWidth, tileWidth,
			                                      BorderPointTileAllocationPolicy.BottomLeft,
			                                      SpatialReference);
		}

		public double PointDensity { get; set; }
		public Func<IEnvelope, int> EstimatePointsFunc { get; set; }

		public IList<SimpleTerrainDataSource> DataSources { get; }

		public override IGeoDataset Dataset => this;

		public override RectangularTilingStructure Tiling { get; }

		public override string Name { get; }

		public override ITin CreateTin(IEnvelope extent, double resolution)
		{
			double expansion = Tiling.TileWidth / 2;
			FeatureTinGenerator tinGenerator = new FeatureTinGenerator(this, expansion);

			return tinGenerator.GenerateTin(extent);
		}

		#region IDataset Members

		bool IDataset.CanCopy()
		{
			return false;
		}

		IDataset IDataset.Copy(string copyName, IWorkspace copyWorkspace)
		{
			throw new NotImplementedException();
		}

		bool IDataset.CanDelete()
		{
			return false;
		}

		void IDataset.Delete()
		{
			throw new NotImplementedException();
		}

		bool IDataset.CanRename()
		{
			return false;
		}

		void IDataset.Rename(string name)
		{
			throw new NotImplementedException();
		}

		[NotNull]
		string IDataset.Name => Name;

		IName IDataset.FullName
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

		string IDataset.BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => esriDatasetType.esriDTTerrain;

		string IDataset.Category => throw new NotImplementedException();

		IEnumDataset IDataset.Subsets => throw new NotImplementedException();

		IWorkspace IDataset.Workspace => ((IDataset) DataSources[0]).Workspace;

		IPropertySet IDataset.PropertySet => throw new NotImplementedException();

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
			if (this == terrainReference)
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
			[NotNull] private readonly IDataset _simpleTerrain;

			public SimpleFeatureTerrainName([NotNull] IDataset simpleTerrain)
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

			public esriDatasetType Type => _simpleTerrain.Type;

			public string Category { get; set; }

			public IWorkspaceName WorkspaceName { get; set; }

			public IEnumDatasetName SubsetNames => throw new NotImplementedException();

			#endregion
		}

		#endregion
	}
}
