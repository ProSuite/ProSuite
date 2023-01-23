using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class IssueDatasetName
	{
		public IssueDatasetName([NotNull] string name,
		                        [NotNull] Type datasetType,
		                        esriGeometryType? geometryType = null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(datasetType, nameof(datasetType));
			Assert.ArgumentCondition(typeof(IErrorDataset).IsAssignableFrom(datasetType),
			                         "dataset type must implement IErrorDataset");
			Assert.ArgumentCondition(geometryType == null ||
			                         IsHighLevelGeometryType(geometryType.Value),
			                         "Must be a high-level geometry type");

			Name = name;
			GeometryType = geometryType;
			DatasetType = datasetType;
		}

		[NotNull]
		public string Name { get; }

		public esriGeometryType? GeometryType { get; }

		[NotNull]
		public Type DatasetType { get; }

		private static bool IsHighLevelGeometryType(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryMultipoint:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryMultiPatch:
					return true;

				default:
					return false;
			}
		}
	}
}
