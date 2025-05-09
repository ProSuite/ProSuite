using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.DataModel.Harvesting;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public abstract class VerifiedDatasetHarvesterBase : IDatasetListBuilder
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private IList<Dataset> _datasets = new List<Dataset>();

		protected VerifiedDatasetHarvesterBase()
		{
			GeometryTypes = GeometryTypeFactory.CreateGeometryTypes().ToList();
		}

		public bool HarvestObjectTypes { get; set; }
		public bool HarvestAttributes { get; set; }

		protected List<GeometryType> GeometryTypes { get; set; }

		public abstract bool IgnoreDataset(IDatasetName datasetName, out string reason);

		public void UseDataset(IDatasetName datasetName)
		{
			Dataset dataset = CreateDataset(datasetName);

			if (dataset != null)
			{
				_datasets.Add(dataset);
			}
		}

		public void AddDatasets(DdxModel model)
		{
			foreach (Dataset dataset in _datasets)
			{
				_msg.DebugFormat("Including dataset {0} in model", dataset.Name);

				model.AddDataset(dataset);
			}
		}

		public void ResetDatasets()
		{
			_datasets = new List<Dataset>();
		}

		[CanBeNull]
		protected abstract Dataset CreateDataset([NotNull] IDatasetName datasetName);

		protected Dataset GetVectorDataset(IDatasetName datasetName)
		{
			IFeatureClassName fcName = (IFeatureClassName) datasetName;

			GeometryTypeShape geometryType = null;

			if (fcName != null)
			{
				geometryType = GetGeometryType(DatasetUtils.GetShapeType(fcName));
			}

			if (geometryType == null)
			{
				_msg.Warn($"Ignoring vector dataset '{datasetName.Name}' due to undefined geometry type");
				return null;
			}
			var verifiedVectorDataset = new VerifiedVectorDataset(datasetName.Name)
			                            {
				                            GeometryType = geometryType
			                            };

			return verifiedVectorDataset;
		}

		protected Dataset GetTableDataset(IDatasetName datasetName)
		{
			VerifiedTableDataset dataset =
				new VerifiedTableDataset(datasetName.Name)
				{
					GeometryType = GetGeometryType<GeometryTypeNoGeometry>()
				};

			return dataset;
		}

		protected Dataset GetRasterMosaicDataset(IDatasetName datasetName)
		{
			return new VerifiedRasterMosaicDataset(datasetName.Name)
			       {
				       GeometryType = GetGeometryType<GeometryTypeRasterMosaic>()
			       };
		}

		protected Dataset GetRasterDataset(IDatasetName datasetName)
		{
			return new VerifiedRasterDataset(datasetName.Name)
			       {
				       GeometryType = GetGeometryType<GeometryTypeRasterDataset>()
			       };
		}

		protected Dataset GetTopologyDataset(IDatasetName datasetName)
		{
			return new VerifiedTopologyDataset(datasetName.Name)
			       {
				       GeometryType = GetGeometryType<GeometryTypeTopology>()
			       };
		}

		[CanBeNull]
		protected T GetGeometryType<T>() where T : GeometryType
		{
			return GeometryTypes.OfType<T>()
			                    .FirstOrDefault();
		}

		[CanBeNull]
		private GeometryTypeShape GetGeometryType(esriGeometryType esriGeometryType)
		{
			return GeometryTypes.OfType<GeometryTypeShape>()
			                    .FirstOrDefault(gt => gt.IsEqual(esriGeometryType));
		}

		public void HarvestChildren()
		{
			foreach (var dataset in _datasets)
			{
				if (dataset is ObjectDataset objectDataset)
				{
					HarvestChildren(objectDataset);
				}
			}
		}

		public void HarvestChildren([NotNull] ObjectDataset dataset)
		{
			// TODO harvest some attribute roles selectively (mainly ObjectID, based on heuristics for what might be a suitable OID in case of non-SDE-geodatabases)

			if (HarvestAttributes)
			{
				AttributeHarvestingUtils.HarvestAttributes(dataset);
			}

			if (HarvestObjectTypes)
			{
				ObjectTypeHarvestingUtils.HarvestObjectTypes(dataset);
			}
		}
	}
}
