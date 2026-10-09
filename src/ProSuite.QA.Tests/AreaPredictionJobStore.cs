using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using IOPath = System.IO.Path;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Persists raw, unreconciled prediction service shapefiles as soon as individual jobs finish.
	/// Submission and completion details are written through the regular ProSuite logger.
	/// The final prediction shapefile remains independent from these recovery artifacts.
	/// </summary>
	public class AreaPredictionJobStore
	{
		private readonly string _directory;
		private readonly string _modelName;

		public AreaPredictionJobStore([NotNull] string finalShapefilePath,
		                              [NotNull] string modelName)
		{
			Assert.ArgumentNotNullOrEmpty(finalShapefilePath,
			                              nameof(finalShapefilePath));
			Assert.ArgumentNotNullOrEmpty(modelName, nameof(modelName));

			string fullPath = IOPath.GetFullPath(finalShapefilePath.Trim());
			string parentDirectory = Assert.NotNullOrEmpty(
				IOPath.GetDirectoryName(fullPath),
				"The prediction shapefile path must include a directory");
			string name = IOPath.GetFileNameWithoutExtension(fullPath);
			_directory = IOPath.Combine(parentDirectory, name + "_tiles");
			_modelName = modelName;
		}

		[NotNull]
		public string DirectoryPath => _directory;

		[CanBeNull]
		public string StoreResult([NotNull] AreaPredictionJobResult job,
		                          [CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(job, nameof(job));

			string shapefilePath = null;
			if (job.Succeeded)
			{
				shapefilePath = GetJobShapefilePath(job);
				var rawPredictions = new List<PredictionResult>(job.Predictions.Count);
				try
				{
					foreach (AreaPrediction prediction in job.Predictions)
					{
						IPolygon polygon = AreaPredictionReconciler.CreatePolygon(
							prediction, spatialReference);
						if (polygon.IsEmpty)
						{
							ComUtils.ReleaseComObject(polygon);
							continue;
						}

						double area = GeometryProperties.GetArea(polygon);
						if (area > 0)
						{
							rawPredictions.Add(
								new PredictionResult(polygon, area, false));
						}
						else
						{
							ComUtils.ReleaseComObject(polygon);
						}
					}

					PredictionShapefileWriter.Write(
						shapefilePath, rawPredictions, spatialReference,
						recreate: true, _modelName);
				}
				finally
				{
					foreach (PredictionResult prediction in rawPredictions)
					{
						ComUtils.ReleaseComObject(prediction.Polygon);
					}
				}
			}

			return shapefilePath;
		}

		[NotNull]
		private string GetJobShapefilePath([NotNull] AreaPredictionJobResult job)
		{
			string imageName = IOPath.GetFileNameWithoutExtension(job.ImagePath);
			string jobId = string.IsNullOrEmpty(job.JobId)
				? "no_job_" + Guid.NewGuid().ToString("N")
				: job.JobId;
			string fileName = MakeSafeFileName(imageName + "_" + jobId) + ".shp";
			return IOPath.Combine(_directory, fileName);
		}

		[NotNull]
		private static string MakeSafeFileName([NotNull] string value)
		{
			char[] invalidCharacters = IOPath.GetInvalidFileNameChars();
			return new string(
				value.Select(character =>
					             invalidCharacters.Contains(character) ? '_' : character)
				     .ToArray());
		}
	}
}
