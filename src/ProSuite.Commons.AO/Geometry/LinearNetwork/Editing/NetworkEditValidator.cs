using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.Editing
{
	public class NetworkEditValidator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public bool AllowLoops { get; set; }

		public bool EnforceFlowDirection { get; set; }

		// TODO: Consider special NetworkIssueHandler that can react with various UIs depending on
		// the issue.
		public bool WeakValidation { get; set; }

		public void ValidateIndividualGeometries(
			[NotNull] Dictionary<IFeature, IGeometry> updates,
			[NotNull] IEnumerable<IFeature> inserts,
			[NotNull] ILinearNetworkFeatureFinder networkFeatureFinder)
		{
			bool valid = true;

			var notifications = new NotificationCollection();

			foreach (var keyValuePair in updates)
			{
				IFeature feature = keyValuePair.Key;

				valid &= ValidateGeometry(feature, networkFeatureFinder, notifications);
			}

			foreach (IFeature newFeature in inserts)
			{
				valid &= ValidateGeometry(newFeature, networkFeatureFinder, notifications);
			}

			HandleValidationResult(valid, notifications);
		}

		public void PerformFinalValidation(
			[NotNull] Dictionary<IFeature, IGeometry> updates,
			[NotNull] IEnumerable<IFeature> inserts,
			[NotNull] ILinearNetworkFeatureFinder networkFeatureFinder)
		{
			bool valid = true;
			NotificationCollection notifications = new NotificationCollection();

			if (EnforceFlowDirection)
			{
				foreach (var update in updates)
				{
					valid &= ValidateFlowDirection(update.Key, networkFeatureFinder, notifications);
				}

				foreach (IFeature insert in inserts)
				{
					valid &= ValidateFlowDirection(insert, networkFeatureFinder, notifications);
				}
			}

			HandleValidationResult(valid, notifications);
		}

		private void HandleValidationResult(bool valid,
		                                    [NotNull] NotificationCollection notifications)
		{
			if (! valid)
			{
				if (WeakValidation)
				{
					_msg.WarnFormat("Invalid linear network features:{0}{1}", Environment.NewLine,
					                NotificationUtils.Concatenate(
						                notifications, Environment.NewLine));
				}
				else
				{
					throw new RuleViolationException(notifications);
				}
			}
		}

		private bool ValidateGeometry([NotNull] IFeature feature,
		                              [NotNull] ILinearNetworkFeatureFinder featureFinder,
		                              [NotNull] NotificationCollection notifications)
		{
			bool result = ValidatePartCount(feature.Shape, GdbObjectUtils.ToString(feature),
			                                notifications);

			if (! AllowLoops)
			{
				result &= ValidateNoLoop(feature.Shape, GdbObjectUtils.ToString(feature),
				                         notifications);
			}

			return result;
		}

		private static bool ValidateFlowDirection(
			[NotNull] IFeature feature,
			[NotNull] ILinearNetworkFeatureFinder featureFinder,
			[NotNull] NotificationCollection notifications)
		{
			if (((IFeatureClass) feature.Class).ShapeType != esriGeometryType.esriGeometryPolyline)
			{
				return true;
			}

			IGeometry newGeometry = feature.Shape;

			IPolyline polyline = (IPolyline) newGeometry;

			bool hasCorrectOrientation = false;
			bool hasWrongOrientation = false;
			int fromEdgeCount = ValidateConnections(feature, LineEnd.From, featureFinder,
			                                        ref hasCorrectOrientation,
			                                        ref hasWrongOrientation);

			int toEdgeCount = ValidateConnections(feature, LineEnd.To, featureFinder,
			                                      ref hasCorrectOrientation,
			                                      ref hasWrongOrientation);

			if (! hasWrongOrientation)
			{
				return true;
			}

			if (! hasCorrectOrientation)
			{
				// all other connections are different -> flip 
				polyline.ReverseOrientation();
				feature.Shape = polyline;
				feature.Store();

				_msg.InfoFormat("Feature {0} was flipped to enforce flow direction",
				                GdbObjectUtils.ToString(feature));
				return true;
			}

			// Mixed situation:
			if (fromEdgeCount <= 1 && toEdgeCount <= 1)
			{
				// No bifurcation or confluence but still incorrect at one end -> error?
				NotificationUtils.Add(notifications,
				                      "Feature {0} does not have a consistent flow direction with respect to its connected edges",
				                      GdbObjectUtils.ToString(feature));
				return false;
			}

			// Confluence or bifurcation -> probably ok
			return true;
		}

		private static int ValidateConnections([NotNull] IFeature feature,
		                                       LineEnd atLineEnd,
		                                       [NotNull] ILinearNetworkFeatureFinder featureFinder,
		                                       ref bool hasCorrectOrientation,
		                                       ref bool hasWrongOrientation)
		{
			IPolyline polyline = (IPolyline) feature.Shape;

			IList<IFeature> connectedEdgeFeatures =
				featureFinder.GetConnectedEdgeFeatures(feature, polyline, atLineEnd);

			IPoint thisLineEndPoint =
				atLineEnd == LineEnd.From ? polyline.FromPoint : polyline.ToPoint;

			foreach (IFeature connectedAtFrom in
			         connectedEdgeFeatures)
			{
				IPolyline connectedPolyline = (IPolyline) connectedAtFrom.Shape;

				IPoint otherLineEndPoint = atLineEnd == LineEnd.From
					                           ? connectedPolyline.ToPoint
					                           : connectedPolyline.FromPoint;

				if (GeometryUtils.AreEqual(otherLineEndPoint, thisLineEndPoint))
				{
					// correct orientation
					hasCorrectOrientation = true;
				}
				else
				{
					hasWrongOrientation = true;
				}
			}

			return connectedEdgeFeatures.Count;
		}

		private static bool ValidateNoLoop([NotNull] IGeometry newGeometry,
		                                   [NotNull] string errorPrefix,
		                                   [NotNull] NotificationCollection notifications)
		{
			if (newGeometry.GeometryType != esriGeometryType.esriGeometryPolyline)
			{
				return true;
			}

			IPolyline polyline = (IPolyline) newGeometry;

			if (polyline.IsClosed)
			{
				NotificationUtils.Add(notifications,
				                      $"{errorPrefix}: A network edge must not be a closed loop.");
				return false;
			}

			return true;
		}

		private static bool ValidatePartCount([NotNull] IGeometry newGeometry,
		                                      [NotNull] string errorPrefix,
		                                      [NotNull] NotificationCollection notifications)
		{
			if (newGeometry.GeometryType != esriGeometryType.esriGeometryPolyline)
			{
				return true;
			}

			if (GeometryUtils.GetPartCount(newGeometry) == 0)
			{
				NotificationUtils.Add(notifications,
				                      $"{errorPrefix}: A network edge cannot be empty.");
				return false;
			}

			if (GeometryUtils.GetPartCount(newGeometry) > 1)
			{
				NotificationUtils.Add(notifications,
				                      $"{errorPrefix}: A network edge must be single part.");
				return false;
			}

			return true;
		}
	}
}
