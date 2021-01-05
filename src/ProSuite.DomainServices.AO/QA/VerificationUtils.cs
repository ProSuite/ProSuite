using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using Path = System.IO.Path;

namespace ProSuite.DomainServices.AO.QA
{
	[CLSCompliant(false)]
	public static class VerificationUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IEnumerable<IObjectClass> GetObjectClasses(
			[NotNull] IEnumerable<Dataset> datasets,
			[NotNull] IDatasetContext datasetContext)
		{
			var result = new List<IObjectClass>();

			foreach (Dataset dataset in datasets)
			{
				if (dataset.Deleted || dataset.Model == null)
				{
					continue;
				}

				var objectDataset = dataset as IObjectDataset;

				if (objectDataset == null)
				{
					continue;
				}

				IObjectClass objectClass;
				try
				{
					objectClass = datasetContext.OpenObjectClass(objectDataset);
				}
				catch (Exception e)
				{
					_msg.DebugFormat("Error opening object class for dataset {0} ({1}): {2}",
					                 dataset.Name, dataset.Model.Name, e.Message);

					objectClass = null;
				}

				if (objectClass != null)
				{
					result.Add(objectClass);
				}
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<string> GetCompressibleFgdbDatasetPaths(
			[NotNull] IEnumerable<IObjectClass> objectClasses)
		{
			Assert.ArgumentNotNull(objectClasses, nameof(objectClasses));

			return objectClasses.Where(CanCompress)
			                    .Select(GetFgdbDatasetPath)
			                    .ToList();
		}

		[NotNull]
		private static string GetFgdbDatasetPath([NotNull] IObjectClass objectClass)
		{
			return Path.Combine(DatasetUtils.GetWorkspace(objectClass).PathName,
			                    DatasetUtils.GetName(objectClass));
		}

		private static bool CanCompress([NotNull] IObjectClass objectClass)
		{
			IWorkspace workspace = DatasetUtils.GetWorkspace(objectClass);
			if (! WorkspaceUtils.IsFileGeodatabase(workspace))
			{
				return false;
			}

			var featureClass = objectClass as IFeatureClass;

			return featureClass?.ShapeType != esriGeometryType.esriGeometryMultiPatch;
		}

		#region Verified Objects

		[NotNull]
		internal static IList<IObject> GetFilteredObjects(
			[NotNull] IEnumerable<IObject> candidates,
			[CanBeNull] IGeometry perimeter,
			bool ensureIntersectionWithPerimeter,
			[CanBeNull] out IEnvelope testExtent)
		{
			Assert.ArgumentNotNull(candidates, nameof(candidates));

			var result = new List<IObject>();

			IEnvelope extent = null;
			IEnvelope featureEnvelopeTemplate = new EnvelopeClass();

			foreach (IObject obj in candidates)
			{
				var feature = obj as IFeature;
				if (feature == null)
				{
					result.Add(obj);
					continue;
				}

				IGeometry shape = feature.Shape;
				if (shape == null)
				{
					continue;
				}

				shape.QueryEnvelope(featureEnvelopeTemplate);

				if (featureEnvelopeTemplate.IsEmpty)
				{
					continue;
				}

				if (perimeter != null)
				{
					GeometryUtils.EnsureSpatialReference(featureEnvelopeTemplate,
					                                     perimeter.SpatialReference);

					if (ensureIntersectionWithPerimeter)
					{
						if (((IRelationalOperator) featureEnvelopeTemplate).Disjoint(perimeter))
						{
							// ignore feature disjoint from perimeter
							continue;
						}
					}
				}

				if (extent == null)
				{
					extent = GeometryFactory.Clone(featureEnvelopeTemplate);
				}
				else
				{
					extent.Union(featureEnvelopeTemplate);
				}

				result.Add(feature);
			}

			testExtent = extent == null || extent.IsEmpty
				             ? null
				             : extent;

			return result;
		}

		[NotNull]
		public static IDictionary<IObjectClass, IObjectDataset> GetDatasetsByObjectClass(
			[NotNull] IEnumerable<IObject> objects,
			[NotNull] IDatasetLookup datasetLookup)
		{
			var result = new Dictionary<IObjectClass, IObjectDataset>();

			foreach (IObject obj in objects)
			{
				IObjectClass objectClass = obj.Class;

				IObjectDataset dataset;
				if (! result.TryGetValue(objectClass, out dataset))
				{
					dataset = datasetLookup.GetDataset(objectClass);
					result.Add(objectClass, dataset);
				}
			}

			return result;
		}

		#endregion
	}
}
