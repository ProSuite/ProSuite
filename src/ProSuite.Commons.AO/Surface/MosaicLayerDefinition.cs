using System;
using System.Collections.Generic;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public class MosaicLayerDefinition
	{
		public MosaicLayerDefinition([NotNull] string definitionString, IList<IRaster> rasters)
		{
			DefinitionString = definitionString;
			MosaicDataset = new Rds(definitionString, rasters);
		}

		[NotNull]
		public string DefinitionString { get; }

		public IRasterDataset MosaicDataset { get; }

		private class Rds : IRasterDataset, IDataset, IGeoDataset
		{
			[NotNull] private readonly string _definitionString;
			[NotNull] private readonly IList<IRaster> _rasters;
			private R _defaultRaster;

			public Rds([NotNull] string definitionString, [NotNull] IList<IRaster> rasters)
			{
				_definitionString = definitionString;
				_rasters = rasters;
			}

			IRaster IRasterDataset.CreateDefaultRaster()
			{
				return DefaultRaster;
			}

			public R DefaultRaster
				=> _defaultRaster ?? (_defaultRaster = new R(_definitionString, _rasters));

			public bool CanCopy()
			{
				return false;
			}

			public IDataset Copy(string copyName, IWorkspace copyWorkspace)
			{
				throw new NotImplementedException();
			}

			void IRasterDataset.OpenFromFile(string Path)
			{
				throw new NotImplementedException();
			}

			void IRasterDataset.PrecalculateStats(object index_list)
			{
				throw new NotImplementedException();
			}

			void IRasterDataset.BasicOpenFromFile(string Path)
			{
				throw new NotImplementedException();
			}

			string IRasterDataset.Format => throw new NotImplementedException();

			string IRasterDataset.SensorType => throw new NotImplementedException();

			string IRasterDataset.CompressionType => throw new NotImplementedException();

			string IRasterDataset.CompleteName => throw new NotImplementedException();

			bool IDataset.CanDelete()
			{
				throw new NotImplementedException();
			}

			void IDataset.Delete()
			{
				throw new NotImplementedException();
			}

			bool IDataset.CanRename()
			{
				return false;
			}

			void IDataset.Rename(string Name)
			{
				throw new NotImplementedException();
			}

			string IDataset.Name => _definitionString;

			IName IDataset.FullName => throw new NotImplementedException();

			string IDataset.BrowseName
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			esriDatasetType IDataset.Type => throw new NotImplementedException();

			string IDataset.Category => throw new NotImplementedException();

			IEnumDataset IDataset.Subsets => throw new NotImplementedException();

			IWorkspace IDataset.Workspace => throw new NotImplementedException();

			IPropertySet IDataset.PropertySet => throw new NotImplementedException();

			ISpatialReference IGeoDataset.SpatialReference =>
				((IGeoDataset) _rasters[0]).SpatialReference;

			IEnvelope IGeoDataset.Extent => DefaultRaster?.Extent;
		}

		private class R : IRaster, IRasterProps, IGeoDataset, ISimpleSurfaceProvider
		         //         , IRaster2, IRawBlocks, ISupportErrorInfo, IClone
		         //, IPixelOperation, IRasterBandCollection, IRasterEdit, IRasterResamplingControl, ISaveAs, ISaveAs2
		{
			[NotNull] private readonly string _def;
			[NotNull] private readonly IList<IRaster> _rasters;

			public R([NotNull] string def, [NotNull] IList<IRaster> rasters)
			{
				_def = def;
				_rasters = rasters;
			}

			void IRaster.Read(IPnt tlc, IPixelBlock block)
			{
				throw new NotImplementedException();
			}

			IPixelBlock IRaster.CreatePixelBlock(IPnt Size)
			{
				throw new NotImplementedException();
			}

			IRasterCursor IRaster.CreateCursor()
			{
				throw new NotImplementedException();
			}

			rstResamplingTypes IRaster.ResampleMethod
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			private IPnt _meanCellSize;

			//TODO: implement
			IPnt IRasterProps.MeanCellSize()
			{
				return _meanCellSize ?? (_meanCellSize = GetMeanCellSize());
			}

			private IPnt GetMeanCellSize()
			{
				foreach (IRaster raster in _rasters)
				{
					IRasterProps props = (IRasterProps) raster;
					return props.MeanCellSize();
				}

				throw new InvalidOperationException("No rasters available");
			}

			int IRasterProps.Width
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			int IRasterProps.Height
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			rstPixelType IRasterProps.PixelType
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			IRasterMapModel IRasterProps.MapModel
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			object IRasterProps.NoDataValue
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			bool IRasterProps.IsInteger => throw new NotImplementedException();

			ISpatialReference IRasterProps.SpatialReference
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			private IEnvelope _extent;

			IEnvelope IRasterProps.Extent
			{
				get => Extent;
				set => throw new NotImplementedException();
			}

			public IEnvelope Extent => _extent ?? (_extent = GetExtent());

			private IEnvelope GetExtent()
			{
				IEnvelope fullEnv = null;
				foreach (IRaster raster in _rasters)
				{
					IRasterProps props = (IRasterProps) raster;
					if (fullEnv == null)
					{
						fullEnv = GeometryFactory.Clone(props.Extent);
					}
					else
					{
						fullEnv.Union(props.Extent);
					}
				}

				return fullEnv;
			}

			ISimpleSurface ISimpleSurfaceProvider.GetSurface(IEnvelope extent)
			{
				throw new NotImplementedException();
			}

			ISpatialReference IGeoDataset.SpatialReference => throw new NotImplementedException();

		}
	}
}
