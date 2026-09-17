using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing.SplitAndMerge
{
	/// <summary>
	/// Helper class to capsulate <see cref="MergeGeometries"/>
	/// <code>
	/// var splitAndMerge = new SplitAndMergeGeometries(firstFeature, secondFeature, perimeter);
	/// Geometry mergedGeometry = splitAndMerge.MergeGeometriesOnPerimeter();
	/// NotificationCollection mergeNotifications = splitAndMerge.Notifications;
	/// NotificationCollection mergeNotifications = splitAndMerge.Notifications;
	///</code>
	/// </summary>
	public class SplitAndMergeGeometries
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Geometry _firstGeometry;

		private readonly Geometry _secondGeometry;

		private readonly Polygon _perimeter;

		public SplitAndMergeGeometries([NotNull] Geometry firstGeometry,
		                               [NotNull] Geometry secondGeometry,
		                               [NotNull] Polygon perimeter)
		{
			Assert.ArgumentNotNull(firstGeometry, nameof(firstGeometry));
			Assert.ArgumentNotNull(secondGeometry, nameof(secondGeometry));
			Assert.ArgumentNotNull(perimeter, nameof(perimeter));

			_firstGeometry = firstGeometry;
			_secondGeometry = secondGeometry;
			_perimeter = perimeter;

			Assert.True(firstGeometry.GeometryType == secondGeometry.GeometryType,
			            "GeometryTypes differ");

			Notifications = new NotificationCollection();
			MergeDiscontinuities = null;
		}

		[NotNull]
		public NotificationCollection Notifications { get; }

		[CanBeNull]
		public Polyline MergeDiscontinuities { get; private set; }

		[CanBeNull]
		public Geometry MergeGeometriesOnPerimeter(bool removeProcessGeneratedPoints = false)
		{
			_msg.Debug("Using basic split and merge functionality (without stairs).");

			Notifications.Clear();
			MergeDiscontinuities = null;

			Geometry result = null;
			PolylineBuilderEx discontinuityLines = new PolylineBuilderEx().Configure(_firstGeometry);

			switch (_firstGeometry.GeometryType)
			{
				case GeometryType.Point:
					result = _firstGeometry;
					break;

				case GeometryType.Multipoint:
					result = MergeGeometries.MergeStates((Multipoint)_firstGeometry,
														(Multipoint)_secondGeometry,
														_perimeter);
					break;

				case GeometryType.Polyline:
					result = MergeGeometries.MergeStates((Polyline) _firstGeometry,
					                                     (Polyline) _secondGeometry, _perimeter,
					                                     discontinuityLines, Notifications);
					break;

				case GeometryType.Polygon:
					result = MergeGeometries.MergeStates((Polygon)_firstGeometry,
					                                     (Polygon)_secondGeometry, _perimeter,
					                                     discontinuityLines, Notifications, removeProcessGeneratedPoints);
					break;
			}


			if (result == null)
			{
				return null;
			}

			if (ControlPointUtils.HasControlPoints(_firstGeometry) ||
			    ControlPointUtils.HasControlPoints(_secondGeometry))
			{
				// TODO(ConflictEngine): Replace if still needed, remove otherwise
				//bool pairwise =
				//	ControlPointTransferUtils
				//		.HasGeometricEffect<GeometricEffectSuppress>(_firstFeature) ||
				//	ControlPointTransferUtils
				//		.HasGeometricEffect<GeometricEffectSuppress>(_secondFeature);
				const bool pairwise = false;

				if (_firstGeometry is Multipart firstCurve &&
				    _secondGeometry is Multipart secondCurve &&
				    result is Multipart target)
				{
					int noTargetFoundCount = 0;
					result = ControlPointTransferUtils.TransferAfterSplitMerge(
						firstCurve, secondCurve, _perimeter, target, pairwise, ref noTargetFoundCount);

					if (noTargetFoundCount > 0)
					{
						Notifications.Add(
							"Could not transfer all Control Points. {0} Control Points were skipped.",
							noTargetFoundCount);
					}
				}
			}

			MergeDiscontinuities = discontinuityLines.ToGeometry();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Resulting merged geometry: {0}", result.GeometryType == GeometryType.Point?
								 GeometryUtils.ToString((MapPoint)result): GeometryUtils.ToString((Multipart)result));
			}

			//if (discontinuityLines.PartCount <= 0)
			//	//if (((IGeometryCollection)discontinuityLines).GeometryCount <= 0)
			//{
			//	return result;
			//}

			//if(!discontinuityLines.IsEmpty)
			////if (GeometryUtils.IsGeometryValid(discontinuityLines))
			//{
			//	MergeDiscontinuities = discontinuityLines.ToGeometry();
			//}

			return result;
		}

		[NotNull]
		public string GetNotificationsFormatted([NotNull] string text)
		{
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));

			string message = Notifications.Count == 0
				                 ? "No merge notification."
				                 : Notifications.Concatenate(Environment.NewLine + "  ");

			return string.Format("{0}:{1}  {2}", text, Environment.NewLine, message);
		}

	}
}
