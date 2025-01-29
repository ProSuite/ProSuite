using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI;
using GeometryUtils = ProSuite.Commons.AGP.Core.Spatial.GeometryUtils;
using Application = System.Windows.Application;
using DisplayUtils = ProSuite.Commons.UI.WPF.DisplayUtils;

namespace ProSuite.Commons.AGP.Framework;

public class WindowPositioner : IWindowPositioner
{
	private readonly List<Rect> _preferredAreas = [];
	private readonly List<Rect> _areasToAvoid = [];
	private readonly EvaluationMethod _method;
	private Window _window;
	private Point _initialPosition;

	// Preferred area of the screen into which the window should be placed, if possible.
	public enum PreferredPlacement
	{
		MapView,
		MainWindow,
		CurrentMonitor
	}

	// The distance to which part of the window should be considered. If e.g. DistanceToTopLeft
	// is chosen, then the top left corner of the window will be placed as close as possible
	// to the desired position (as opposed to e.g. considering the distance to the complete
	// rectangle of the window).
	public enum EvaluationMethod
	{
		DistanceToTopLeft,
		DistanceToRect
	}

	public WindowPositioner([NotNull] Feature featureToAvoid, [NotNull] Layer layer,
	                        PreferredPlacement placement,
	                        EvaluationMethod method)
	{
		_method = method;
		Rect mapViewArea = GetMapViewScreenRect();
		var boundingRects = GetBoundingRectsScreen(featureToAvoid, layer as FeatureLayer);
		foreach (Rect rect in boundingRects)
		{
			// Restrict geometries to MapView area
			rect.Intersect(mapViewArea);
			if (! rect.IsEmpty)
			{
				_areasToAvoid.Add(rect);
			}
		}

		if (placement == PreferredPlacement.MapView)
		{
			_preferredAreas.Add(mapViewArea);
		}
	}

	public WindowPositioner([NotNull] List<Geometry> geometriesToAvoid,
	                        PreferredPlacement placement,
	                        EvaluationMethod method, int lineOffset = 4)
	{
		_method = method;
		Rect mapViewArea = GetMapViewScreenRect();
		foreach (var item in geometriesToAvoid)
		{
			var boundingRects = GetBoundingRectsScreen(item, lineOffset);
			foreach (Rect rect in boundingRects)
			{
				// Restrict geometries to MapView area
				rect.Intersect(mapViewArea);
				if (! rect.IsEmpty)
				{
					_areasToAvoid.Add(rect);
				}
			}
		}

		if (placement == PreferredPlacement.MapView)
		{
			_preferredAreas.Add(mapViewArea);
		}
	}

	public WindowPositioner(List<Rect> preferredAreas,
	                        List<Rect> areasToAvoid,
	                        EvaluationMethod method)
	{
		_method = method;
		_areasToAvoid.AddRange(areasToAvoid);
		_preferredAreas.AddRange(preferredAreas);
	}

	// Sets the window to be positioned and changes its position on SizeChanged events.
	// The initial position of the window is used as the desired position, i.e. the window
	// is kept as close to its initial position as possible.
	public void SetWindow([NotNull] Window window, Point desiredPosition)
	{
		_window = window;
		_initialPosition = desiredPosition;
		_window.SizeChanged += (sender, args) =>
		{
			if (window.Width is double.NaN || window.Height is double.NaN)
			{
				return;
			}

			var result = FindSuitablePosition(_initialPosition, _window.RenderSize.Width,
			                                  _window.RenderSize.Height);
			_window.Left = result.X;
			_window.Top = result.Y;
		};
	}

	public Point FindSuitablePosition(Point desiredPosition, double windowWidth,
	                                  double windowHeight)
	{
		List<Rect> preferredAreasDeviceIndependent = [];
		List<Rect> areasToAvoidDeviceIndependent = [];
		List<Rect> monitorsDeviceIndependent = [];

		Window ownerWindow = Application.Current?.MainWindow;

		if (ownerWindow != null)
		{
			desiredPosition = DisplayUtils.ToDeviceIndependentPixels(desiredPosition, ownerWindow);
			
			var monitors = GetMonitorExtends(desiredPosition);
			monitorsDeviceIndependent.AddRange(
				monitors.Select(
					rect => DisplayUtils.ToDeviceIndependentPixels(rect, ownerWindow)));

			preferredAreasDeviceIndependent.AddRange(
				_preferredAreas.Select(
					rect => DisplayUtils.ToDeviceIndependentPixels(rect, ownerWindow)));

			areasToAvoidDeviceIndependent.AddRange(
				_areasToAvoid.Select(
					rect => DisplayUtils.ToDeviceIndependentPixels(rect, ownerWindow)));

			if (ownerWindow.WindowState != WindowState.Maximized)
			{
				// Prefer to put the window inside the client area of the main window
				// Note that the window sizes are not accurate for maximized windows. However, they are
				// not needed in this case because the monitor sizes are taken into account as well anyway.
				preferredAreasDeviceIndependent.Add(new Rect(ownerWindow.Left, ownerWindow.Top,
				                                             ownerWindow.ActualWidth,
				                                             ownerWindow.ActualHeight));
			}
		}
		else
		{
			monitorsDeviceIndependent = GetMonitorExtends(desiredPosition);
			preferredAreasDeviceIndependent.AddRange(_preferredAreas);
			areasToAvoidDeviceIndependent.AddRange(_areasToAvoid);
		}

		// The window should be close to the initial position of the window.
		var result =
			FindPositionForObject(desiredPosition, windowWidth, windowHeight,
			                      preferredAreasDeviceIndependent,
			                      areasToAvoidDeviceIndependent, monitorsDeviceIndependent,
			                      _method);
		return result;
	}

	// Note that preferredAreas are assumed to be ordered such that the best area comes first (e.g. mapView area first, then client window)
	private Point FindPositionForObject(Point desiredPosition, double objectWidth,
	                                    double objectHeight, List<Rect> preferredAreas,
	                                    List<Rect> areasToAvoid, List<Rect> monitors,
	                                    EvaluationMethod method)
	{
		// Find a suitable position while ensuring that the object is kept completely on one monitor and inside a preferred area.
		foreach (Rect monitor in monitors)
		{
			foreach (Rect area in preferredAreas)
			{
				if (! area.IntersectsWith(monitor))
				{
					continue;
				}

				area.Intersect(monitor);

				if (TryPlaceObjectInRect(desiredPosition, objectWidth, objectHeight, area,
				                         areasToAvoid, out var pos))
				{
					return pos;
				}
			}
		}

		foreach (Rect monitor in monitors)
		{
			// No viable position found inside a preferred area (or no preferred area given). Try to simply
			// put it somewhere on a monitor while still respecting areasToAvoid.
			if (TryPlaceObjectInRect(desiredPosition, objectWidth, objectHeight, monitor,
			                         areasToAvoid, out var result))
			{
				return result;
			}
		}

		// No viable position found which respects areasToAvoid. Just make sure that the object is completely on the current monitor.
		Rect objRect = new Rect(desiredPosition.X, desiredPosition.Y, objectWidth, objectHeight);
		MoveInside(monitors[0], ref objRect);
		return objRect.TopLeft;
	}

	private bool TryPlaceObjectInRect(Point desiredPosition, double objectWidth,
	                                  double objectHeight,
	                                  Rect targetRect,
	                                  List<Rect> areasToAvoid,
	                                  out Point result)
	{
		Rect startingRect =
			new Rect(desiredPosition.X, desiredPosition.Y, objectWidth, objectHeight);
		int firstIteration = 0;

		// Find the optimal starting distance.
		switch (_method)
		{
			case EvaluationMethod.DistanceToTopLeft:
			{
				if (! targetRect.Contains(startingRect))
				{
					// Move the starting position away from desiredPosition so that the resulting rectangle is completely within targetRect.
					MoveInside(targetRect, ref startingRect);

					if (! targetRect.Contains(startingRect))
					{
						// The object does not fit in targetRect.
						result = desiredPosition;
						return false;
					}

					// Use the moved rect to estimate the minimal distance needed.
					var distanceX = Math.Min(Math.Abs(desiredPosition.X - startingRect.Left),
					                         Math.Abs(desiredPosition.X + objectWidth -
					                                  startingRect.Right));
					var distanceY = Math.Min(Math.Abs(desiredPosition.Y - startingRect.Top),
					                         Math.Abs(
						                         desiredPosition.Y + objectHeight -
						                         startingRect.Bottom));
					firstIteration = (int) Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
				}

				break;
			}

			case EvaluationMethod.DistanceToRect:
			{
				if (! targetRect.Contains(desiredPosition))
				{
					// Use the target rect to estimate the minimal distance needed.
					firstIteration =
						(int) Math.Sqrt(DistanceToRectSquared(desiredPosition, targetRect));
				}

				break;
			}

			default:
				throw new NotImplementedException("Unknown evaluation method");
		}

		// Look for the best solution: The algorithm starts at the desired position and works outward from there until a suitable position is found.
		Point bestPosition = desiredPosition;
		bool positionFound = false;
		for (int iteration = firstIteration;; ++iteration)
		{
			Queue<Point> pointsToLookAt = [];
			AddNextPoints(desiredPosition, objectWidth, objectHeight, iteration,
			              ref pointsToLookAt);

			// If no points are found, then we still do at least 50 iterations before giving up.
			bool pointsWithinTargetFound = iteration < firstIteration + 50;
			while (pointsToLookAt.Count > 0)
			{
				Point pos = pointsToLookAt.Dequeue();
				Rect objRect = new Rect(pos.X, pos.Y, objectWidth, objectHeight);

				if (! targetRect.Contains(objRect))
				{
					continue;
				}

				pointsWithinTargetFound = true;

				if (IntersectsWithAny(objRect, areasToAvoid))
				{
					continue;
				}

				if (! positionFound ||
				    IsNewPositionPreferred(pos, bestPosition, objectWidth, objectHeight,
				                           desiredPosition))
				{
					bestPosition = pos;
					positionFound = true;
				}
			}

			if (positionFound || ! pointsWithinTargetFound)
			{
				break;
			}
		}

		result = bestPosition;
		return positionFound;
	}

	private void AddNextPoints(Point origin, double objectWidth,
	                           double objectHeight, int iteration,
	                           ref Queue<Point> pointsToLookAt)
	{
		switch (_method)
		{
			case EvaluationMethod.DistanceToTopLeft:
				AddNextPointsRadial(origin, iteration, ref pointsToLookAt);
				break;

			case EvaluationMethod.DistanceToRect:
				AddNextPointsRectangle(origin, objectWidth, objectHeight, iteration,
				                       ref pointsToLookAt);
				break;

			default:
				throw new NotImplementedException("Unknown evaluation method");
		}
	}

	private static void AddNextPointsRadial(Point origin, int iteration,
	                                        ref Queue<Point> pointsToLookAt)
	{
		int nrPoints = iteration switch
		{
			0 => 1,
			< 10 => 10,
			_ => 100
		};

		double deltaAngle = 2 * Math.PI / nrPoints;
		double angle = 0.0;

		for (int i = 0; i < nrPoints; ++i)
		{
			pointsToLookAt.Enqueue(new Point(origin.X + iteration * Math.Cos(-angle),
			                                 origin.Y + iteration * Math.Sin(-angle)));
			angle += deltaAngle;
		}
	}

	private static void AddNextPointsRectangle(Point origin, double objectWidth,
	                                           double objectHeight, int iteration,
	                                           ref Queue<Point> pointsToLookAt)
	{
		if (iteration == 0)
		{
			pointsToLookAt.Enqueue(origin);
			return;
		}

		int nrPointsPerSide = iteration switch
		{
			< 10 => 10,
			_ => 100
		};

		double deltaX = (objectWidth + 2 * iteration) / nrPointsPerSide;
		double deltaY = (objectHeight + 2 * iteration) / nrPointsPerSide;
		for (int i = 0; i < nrPointsPerSide; ++i)
		{
			pointsToLookAt.Enqueue(new Point(origin.X - objectWidth - iteration + i * deltaX,
			                                 origin.Y + iteration));
			pointsToLookAt.Enqueue(new Point(origin.X - objectWidth - iteration + i * deltaX,
			                                 origin.Y - objectHeight - iteration));

			pointsToLookAt.Enqueue(new Point(origin.X - objectWidth - iteration,
			                                 origin.Y - objectHeight - iteration + i * deltaY));
			pointsToLookAt.Enqueue(new Point(origin.X + iteration,
			                                 origin.Y - objectHeight - iteration + i * deltaY));
		}
	}

	private bool IsNewPositionPreferred(Point newPos, Point prevPos, double objectWidth,
	                                    double objectHeight, Point desiredPosition)
	{
		return _method switch
		{
			EvaluationMethod.DistanceToTopLeft => IsNewPositionPreferredRadial(
				newPos, prevPos, desiredPosition),
			EvaluationMethod.DistanceToRect => IsNewPositionPreferredRect(
				newPos, prevPos, objectWidth, objectHeight, desiredPosition),
			_ => throw new NotImplementedException("Unknown evaluation method"),
		};
	}

	private static bool IsNewPositionPreferredRadial(Point newPos, Point prevPos,
	                                                 Point desiredPosition)
	{
		bool prevToTheLeft = (int) prevPos.X < (int) desiredPosition.X;
		bool newToTheLeft = (int) newPos.X < (int) desiredPosition.X;

		if (prevToTheLeft == newToTheLeft)
		{
			return Math.Abs(newPos.Y - desiredPosition.Y) < Math.Abs(prevPos.Y - desiredPosition.Y);
		}

		return ! newToTheLeft;
	}

	private static bool IsNewPositionPreferredRect(Point newPos, Point prevPos, double objectWidth,
	                                               double objectHeight, Point desiredPosition)
	{
		bool prevBelow = prevPos.Y > desiredPosition.Y;
		bool newBelow = newPos.Y > desiredPosition.Y;
		bool prevAbove = prevPos.Y - objectHeight < desiredPosition.Y;
		bool newAbove = newPos.Y - objectHeight < desiredPosition.Y;

		if ((prevBelow && newBelow) || (prevAbove && newAbove))
		{
			return Math.Abs(newPos.X - desiredPosition.X) < Math.Abs(prevPos.X - desiredPosition.X);
		}

		if ((newBelow && prevAbove) || (newAbove && prevBelow))
		{
			return newBelow;
		}

		if (! newBelow && ! newAbove && ! prevBelow && ! prevAbove)
		{
			return (newPos.X > desiredPosition.X && prevPos.X < desiredPosition.X) ||
			       newPos.Y > prevPos.Y;
		}

		return newBelow;
	}

	private static void MoveInside(Rect areaToMoveInto, ref Rect rectToBeMoved)
	{
		if (rectToBeMoved.Left < areaToMoveInto.Left)
		{
			rectToBeMoved.X = areaToMoveInto.X;
		}

		if (rectToBeMoved.Top < areaToMoveInto.Top)
		{
			rectToBeMoved.Y = areaToMoveInto.Y;
		}

		if (rectToBeMoved.Right > areaToMoveInto.Right)
		{
			rectToBeMoved.X = areaToMoveInto.Right - rectToBeMoved.Width;
		}

		if (rectToBeMoved.Bottom > areaToMoveInto.Bottom)
		{
			rectToBeMoved.Y = areaToMoveInto.Bottom - rectToBeMoved.Height;
		}
	}

	private static bool IntersectsWithAny(Rect objRect, List<Rect> areasToAvoid)
	{
		return areasToAvoid.Any(rect => objRect.IntersectsWith(rect));
	}

	// The resulting list is sorted by proximity to focusPoint
	private static List<Rect> GetMonitorExtends(Point focusPoint)
	{
		List<Rect> screens = [];
		foreach (var screen2 in Screen.AllScreens)
		{
			var rect = screen2.WorkingArea;

			screens.Add(new Rect(rect.X, rect.Y, rect.Width, rect.Height));
		}

		screens.Sort((a, b) =>
		{
			double distanceA = DistanceToRectSquared(focusPoint, a);
			double distanceB = DistanceToRectSquared(focusPoint, b);
			return distanceA.CompareTo(distanceB);
		});

		return screens;
	}

	private static Rect GetMapViewScreenRect()
	{
		try
		{
			var extent = MapView.Active.Extent;

			MapPoint upperRight = GeometryUtils.GetUpperRight(extent);
			MapPoint lowerLeft = GeometryUtils.GetLowerLeft(extent);
			var upperRightScreen = MapView.Active.MapToScreen(upperRight);
			var lowerLeftScreen = MapView.Active.MapToScreen(lowerLeft);
			return new Rect(lowerLeftScreen, upperRightScreen);
		}
		catch
		{
			return Rect.Empty;
		}
	}

	// Returns the bounding rects in screen coordinates of the given feature. If layer is not null,
	// then the extent of the symbolization is taken into account as well. Line geometries are split
	// if they are longer than maxRectLengthForLines, i.e. they are then approximated by multiple
	// rectangles. Each envelope rectangle of a line geometry is enlarged by lineOffset.
	private static List<Rect> GetBoundingRectsScreen(Feature feature, FeatureLayer layer,
	                                                 int maxRectLengthForLines = 25)
	{
		Geometry geometry = feature.GetShape();

		List<Rect> boundingRects =
			GetBoundingRectsScreen(geometry, 0, maxRectLengthForLines);

		if (layer == null || ! layer.CanLookupSymbol())
		{
			return boundingRects;
		}

		var symbol = layer.LookupSymbol(feature.GetObjectID(), MapView.Active);
		double size = symbol.GetSize();
		double offset = size / (MapView.Active.Camera.Scale / MapView.Active.Map.ReferenceScale);

		return boundingRects.Select(r =>
		{
			r.Inflate(offset, offset);
			return r;
		}).ToList();
	}

	// Returns the bounding rects in screen coordinates of the given feature. Line geometries are split if they are longer than
	// maxRectLengthForLines, i.e. they are then approximated by multiple rectangles. Each envelope rectangle of a line geometry
	// is enlarged by lineOffset.
	private static List<Rect> GetBoundingRectsScreen(Geometry geometry,
	                                                 int lineOffset,
	                                                 int maxRectLengthForLines = 25)
	{
		List<Rect> boundingRects = [];

		switch (geometry.GeometryType)
		{
			case GeometryType.Polyline:
			{
				var line = (Polyline) geometry;
				boundingRects.AddRange(
					GetSplitLineExtentScreen(line, lineOffset, maxRectLengthForLines));
				break;
			}
			case GeometryType.Point:
			case GeometryType.Multipoint:
			case GeometryType.Envelope:
			case GeometryType.Polygon:
			case GeometryType.Multipatch:
			case GeometryType.GeometryBag:
			case GeometryType.Unknown:
			default:
			{
				boundingRects.Add(GetExtentScreen(geometry, lineOffset));
				break;
			}
		}

		return boundingRects;
	}

	private static Rect GetExtentScreen(Geometry geometry, int offset)
	{
		Geometry clipped = geometry;
		if (geometry is Polygon polygon)
		{
			clipped = GetPolygonGeometry(polygon);
		}

		MapPoint upperRight = GeometryUtils.GetUpperRight(clipped.Extent);
		MapPoint lowerLeft = GeometryUtils.GetLowerLeft(clipped.Extent);
		var upperRightScreen = MapView.Active.MapToScreen(upperRight);
		var lowerLeftScreen = MapView.Active.MapToScreen(lowerLeft);
		Rect r = new Rect(lowerLeftScreen, upperRightScreen);
		r.Inflate(offset, offset);
		return r;
	}

	private static List<Rect> GetSplitLineExtentScreen(Polyline line,
	                                                   int lineOffset,
	                                                   int maxRectLengthForLines)
	{
		List<Rect> boundingRects = [];
		foreach (var part in line.Parts)
		{
			foreach (var segment in part)
			{
				MapPoint startPoint = segment.StartPoint;
				MapPoint endPoint = segment.EndPoint;
				var startPointScreen = MapView.Active.MapToScreen(startPoint);
				var endPointScreen = MapView.Active.MapToScreen(endPoint);

				// Subdivide the bounding rect to more precisely approximate the shape of the line
				int length = (int) (startPointScreen - endPointScreen).Length;
				int numberOfPoints = length / maxRectLengthForLines;
				Point prevPoint = startPointScreen;
				for (int i = 0; i < numberOfPoints - 1; ++i)
				{
					Point pointNext =
						startPointScreen + (endPointScreen - startPointScreen) * i /
						numberOfPoints;

					var geometryExtent = new Rect(prevPoint, pointNext);
					geometryExtent.Inflate(lineOffset, lineOffset);
					boundingRects.Add(geometryExtent);

					prevPoint = pointNext;
				}

				var lastGeometryExtent = new Rect(prevPoint, endPointScreen);
				lastGeometryExtent.Inflate(lineOffset, lineOffset);
				boundingRects.Add(lastGeometryExtent);
			}
		}

		return boundingRects;
	}

	private static Polygon GetPolygonGeometry(Polygon polygon)
	{
		Envelope clipExtent = MapView.Active?.Extent;

		if (clipExtent == null)
		{
			return polygon;
		}

		double mapRotation = MapView.Active.NotNullCallback(mv => mv.Camera.Heading);

		return GeometryUtils.GetClippedPolygon(polygon, clipExtent, mapRotation);
	}

	private static double DistanceToRectSquared(Point pos, Rect rect)
	{
		var dx = Math.Max(Math.Max(rect.Left - pos.X, 0),
		                  pos.X - (rect.Right));
		var dy = Math.Max(Math.Max(rect.Top - pos.Y, 0),
		                  pos.Y - (rect.Bottom));
		return dx * dx + dy * dy;
	}
}
