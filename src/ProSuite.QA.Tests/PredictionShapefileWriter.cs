using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using IOPath = System.IO.Path;

namespace ProSuite.QA.Tests
{
	internal static class PredictionShapefileWriter
	{
		private const string _areaFieldName = "AREA_M2";
		private const string _isNewFieldName = "IS_NEW";
		private const string _modelFieldName = "MODEL";
		private static readonly object _sync = new object();

		public static void Write([NotNull] string shapefilePath,
		                         [NotNull] IEnumerable<PredictionResult> predictions,
		                         [CanBeNull] ISpatialReference spatialReference,
		                         bool recreate,
		                         [NotNull] string modelName)
		{
			Assert.ArgumentNotNullOrEmpty(shapefilePath, nameof(shapefilePath));
			Assert.ArgumentNotNull(predictions, nameof(predictions));
			Assert.ArgumentNotNullOrEmpty(modelName, nameof(modelName));

			lock (_sync)
			{
				string normalizedPath = NormalizeShapefilePath(shapefilePath);
				string directory = IOPath.GetDirectoryName(normalizedPath);
				string featureClassName =
					IOPath.GetFileNameWithoutExtension(normalizedPath);

				Directory.CreateDirectory(directory);

				if (recreate)
				{
					DeleteShapefile(normalizedPath);
				}

				IFeatureWorkspace workspace =
					WorkspaceUtils.OpenShapefileWorkspace(directory);
				IFeatureClass featureClass = null;
				IFeatureCursor insertCursor = null;

				try
				{
					featureClass = OpenOrCreateFeatureClass(
						workspace, featureClassName, spatialReference);

					insertCursor = featureClass.Insert(true);

					int areaFieldIndex = featureClass.FindField(_areaFieldName);
					int isNewFieldIndex = featureClass.FindField(_isNewFieldName);
					int modelFieldIndex = featureClass.FindField(_modelFieldName);

					foreach (PredictionResult prediction in predictions)
					{
						IFeatureBuffer buffer = featureClass.CreateFeatureBuffer();
						try
						{
							buffer.Shape = prediction.Polygon;
							buffer.Value[areaFieldIndex] = prediction.Area;
							buffer.Value[isNewFieldIndex] = prediction.IsNew ? 1 : 0;
							buffer.Value[modelFieldIndex] = modelName;

							insertCursor.InsertFeature(buffer);
						}
						finally
						{
							ComUtils.ReleaseComObject(buffer);
						}
					}

					insertCursor.Flush();
				}
				finally
				{
					ComUtils.ReleaseComObject(insertCursor);
					ComUtils.ReleaseComObject(featureClass);
					ComUtils.ReleaseComObject(workspace);
				}
			}
		}

		[NotNull]
		private static IFeatureClass OpenOrCreateFeatureClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string featureClassName,
			[CanBeNull] ISpatialReference spatialReference)
		{
			try
			{
				return workspace.OpenFeatureClass(featureClassName);
			}
			catch
			{
				IFields fields = FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
					                            spatialReference),
					FieldUtils.CreateDoubleField(_areaFieldName),
					FieldUtils.CreateIntegerField(_isNewFieldName),
					FieldUtils.CreateTextField(_modelFieldName, 128));

				return DatasetUtils.CreateSimpleFeatureClass(
					workspace, featureClassName, fields);
			}
		}

		[NotNull]
		private static string NormalizeShapefilePath([NotNull] string shapefilePath)
		{
			string result = shapefilePath.Trim();

			if (! string.Equals(IOPath.GetExtension(result), ".shp",
			                    StringComparison.OrdinalIgnoreCase))
			{
				result += ".shp";
			}

			string fullPath = IOPath.GetFullPath(result);
			Assert.NotNullOrEmpty(IOPath.GetDirectoryName(fullPath),
			                      "The prediction shapefile path must include a directory");

			return fullPath;
		}

		private static void DeleteShapefile([NotNull] string shapefilePath)
		{
			string directory = IOPath.GetDirectoryName(shapefilePath);
			string name = IOPath.GetFileNameWithoutExtension(shapefilePath);

			foreach (string extension in new[]
			         {
				         ".shp", ".shx", ".dbf", ".prj", ".sbn", ".sbx", ".cpg",
				         ".xml"
			         })
			{
				string path = IOPath.Combine(directory, name + extension);
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
		}
	}

	internal class PredictionResult
	{
		public PredictionResult([NotNull] IPolygon polygon,
		                        double area,
		                        bool isNew)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			Polygon = polygon;
			Area = area;
			IsNew = isNew;
		}

		[NotNull]
		public IPolygon Polygon { get; }

		public double Area { get; }

		public bool IsNew { get; }
	}
}
