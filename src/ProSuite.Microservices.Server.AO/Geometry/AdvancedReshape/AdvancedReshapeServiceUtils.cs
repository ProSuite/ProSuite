using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Geometry.LinearNetwork;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.AdvancedReshape
{
	public static class AdvancedReshapeServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static AdvancedReshapeResponse Reshape(
			[NotNull] AdvancedReshapeRequest request)
		{
			var polyline = (IPolyline) ProtobufGeometryUtils.FromShapeMsg(request.ReshapePaths);

			List<IPath> reshapePaths = GeometryUtils.GetPaths(Assert.NotNull(polyline)).ToList();

			GeometryReshaperBase reshaper = CreateReshaper(request, reshapePaths);

			bool useNonDefaultReshapeSide = request.UseNonDefaultReshapeSide;

			var notifications = new NotificationCollection();

			IDictionary<IGeometry, NotificationCollection> reshapedGeometries =
				reshaper.Reshape(reshapePaths, useNonDefaultReshapeSide, notifications);

			Assert.NotNull(reshapedGeometries, "No reshaped geometries");

			if (reshapedGeometries.Count == 0)
			{
				return NoReshapeResponse(notifications);
			}

			//foreach (KeyValuePair<IGeometry, NotificationCollection> reshapedGeometry in reshapedGeometries)
			//{
			//	IGeometry geometry = reshapedGeometry.Key;
			//	DebugHelper.StoreNewFeature(geometry, "RingSegmentsTest.gdb", "PreResult");
			//}

			var response = new AdvancedReshapeResponse();

			if (reshaper.ResultWithinOtherResultButNotInOriginal(
				    reshapedGeometries.Keys, out IPolygon containedPolygon))
			{
				response.OverlapPolygon = ProtobufGeometryUtils.ToShapeMsg(containedPolygon);
			}

			// Messages regarding some of the features that were not reshaped:
			if (notifications.Count > 0 && request.Features.Count > 1)
			{
				string overallMessage = notifications.Concatenate(". ");

				_msg.Info(overallMessage);
				response.WarningMessage = overallMessage;
			}

			// Junction-move, updating of adjacent lines is performed in Save:
			//hier wird die geometry final geändert
			IList<IFeature> storedFeatures = reshaper.Save(reshapedGeometries);

			//foreach (IFeature storedFeature in storedFeatures)
			//{
			//	DebugHelper.StoreNewFeature(storedFeature.ShapeCopy, "RingSegmentsTest.gdb", "PreResult2");
			//}

			response.OpenJawReshapeHappened = reshaper.OpenJawReshapeOcurred;
			response.OpenJawIntersectionCount = reshaper.OpenJawIntersectionPointCount;

			PackReshapeResponseFeatures(response, storedFeatures, reshapedGeometries,
			                            reshaper);

			return response;
		}

		public static ShapeMsg GetOpenJawReshapeReplaceEndPoint(
			[NotNull] OpenJawReshapeLineReplacementRequest request,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			var polylineToReshape =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(request.Feature.Shape);
			var reshapeLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(request.ReshapePath);

			IPoint endPoint = null;

			if (polylineToReshape != null && reshapeLine != null)
			{
				endPoint = ReshapeUtils.GetOpenJawReshapeLineReplaceEndPoint(
					polylineToReshape, reshapeLine, request.UseNonDefaultReshapeSide);
			}

			if (endPoint == null)
			{
				return new ShapeMsg();
			}

			ShapeMsg result = ProtobufGeometryUtils.ToShapeMsg(endPoint);

			return result;
		}

		private static GeometryReshaperBase CreateReshaper(
			AdvancedReshapeRequest request, IList<IPath> reshapePaths)
		{
			GeometryReshaperBase result;

			bool allowOpenJaw = request.AllowOpenJawReshape;
			bool moveOpenJawEndJunction = request.MoveOpenJawEndJunction;

			IList<IFeature> featuresToReshape =
				GetFeaturesToReshape(request, out GdbTableContainer container);

			if (featuresToReshape.Count == 1)
			{
				IFeature firstFeature = featuresToReshape[0];

				var singleGeometryReshaper =
					new GeometryReshaper(firstFeature)
					{
						AllowOpenJawReshape = allowOpenJaw
					};

				result = singleGeometryReshaper;
			}
			else
			{
				var stickyIntersections = new StickyIntersections(featuresToReshape);

				foreach (SourceTargetPointPair pair in request.StickyIntersections)
				{
					stickyIntersections.SourceTargetPairs.Add(
						new KeyValuePair<IPoint, IPoint>(
							(IPoint) ProtobufGeometryUtils.FromShapeMsg(
								pair.SourcePoint),
							(IPoint) ProtobufGeometryUtils.FromShapeMsg(
								pair.TargetPoint)));
				}

				result = new MultipleGeometriesReshaper(featuresToReshape)
				         {
					         MultipleSourcesTreatIndividually = true,
					         MultipleSourcesTreatAsUnion =
						         request.MultipleSourcesTryUnion,
					         MaxProlongationLengthFactor = 4,
					         StickyIntersectionPoints = stickyIntersections
				         };

				// Conditions for closed reshape paths being removed:
				// - multiple source geometries, at least two of which polygons
				// - multiple reshape paths (or a single closed path)
				// ... consider checking that the ring-path is completely inside the outermost ring of the source polygon's union
				//     or at least that several polygons are intersected by the sketch geometry
				if ((reshapePaths.Count > 1) || reshapePaths.All(path => path.IsClosed))
				{
					if (ContainsMultiplePolygonFeatures(featuresToReshape))
					{
						result.RemoveClosedReshapePathAreas = true;
					}
				}
			}

			if (moveOpenJawEndJunction)
			{
				IList<IFeature> targetCandidates = ProtobufConversionUtils.FromGdbObjectMsgList(
					request.PotentiallyConnectedFeatures, container);

				result.NetworkFeatureFinder =
					new LinearNetworkGdbFeatureFinder(targetCandidates);

				result.NetworkFeatureUpdater =
					new LinearNetworkNodeUpdater(result.NetworkFeatureFinder);
			}

			result.MoveLineEndJunction = moveOpenJawEndJunction;

			// TODO: Add to admin-options (i.e. central defaults only, no GUI) together with the threshold
			// _useSimplifiedReshapeSideDeterminationVertexThreshold in ReshapeInfo
			result.AllowSimplifiedReshapeSideDetermination = true;

			return result;
		}

		private static IList<IFeature> GetFeaturesToReshape(
			[NotNull] AdvancedReshapeRequest request,
			out GdbTableContainer container)
		{
			container = ProtobufConversionUtils.CreateGdbTableContainer(
				request.ClassDefinitions, null, out _);

			foreach (VirtualTable dataset in container.GetDatasets(esriDatasetType.esriDTAny))
			{
				if (dataset is IObjectClass objectClass)
				{
					objectClass.AddField(FieldUtils.CreateOIDField());
				}
			}

			IList<IFeature> featuresToReshape =
				ProtobufConversionUtils.FromGdbObjectMsgList(request.Features, container);

			return featuresToReshape;
		}

		private static AdvancedReshapeResponse NoReshapeResponse(
			NotificationCollection notifications)
		{
			string notificationMessage =
				notifications.Count > 0
					? $"{Environment.NewLine}{notifications.Concatenate(Environment.NewLine)}"
					: string.Empty;

			string noReshapeMessage = $"Unable to perform reshape{notificationMessage}";

			_msg.WarnFormat(noReshapeMessage);

			return new AdvancedReshapeResponse() { WarningMessage = noReshapeMessage };
		}

		private static void PackReshapeResponseFeatures(
			AdvancedReshapeResponse result,
			[NotNull] IEnumerable<IFeature> storedFeatures,
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			GeometryReshaperBase reshaper)
		{
			foreach (IFeature storedFeature in storedFeatures)
			{
				IGeometry newGeometry = storedFeature.Shape;

				var resultFeature = new ResultObjectMsg();

				GdbObjectMsg resultFeatureMsg =
					ProtobufGdbUtils.ToGdbObjectMsg(storedFeature, newGeometry,
					                                storedFeature.Class.ObjectClassID);

				resultFeature.Update = resultFeatureMsg;

				if (reshapedGeometries.TryGetValue(newGeometry,
				                                   out NotificationCollection notifications) &&
				    notifications != null)
				{
					if (notifications.Count == 0)
					{
						esriUnits units = esriUnits.esriMeters;
						string message =
							reshaper.GetSizeChangeMessage(newGeometry, storedFeature, units, units);

						resultFeature.Notifications.Add(message);
					}
					else
					{
						foreach (INotification notification in notifications)
						{
							resultFeature.Notifications.Add(notification.Message);
							resultFeature.HasWarning = reshaper.NotificationIsWarning;
						}
					}
				}

				result.ResultFeatures.Add(resultFeature);
			}
		}

		private static bool ContainsMultiplePolygonFeatures(
			IEnumerable<IFeature> selectedFeatures)
		{
			int polygonFeatureCount = selectedFeatures.Count(
				feature =>
					((IFeatureClass) feature.Class).ShapeType ==
					esriGeometryType.esriGeometryPolygon);

			return polygonFeatureCount >= 2;
		}
	}
}
