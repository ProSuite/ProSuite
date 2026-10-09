using System;
using System.Collections.Generic;
using System.Text;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Cracker;

/// <summary>
/// Label texts, label symbols and geometry access shared by the display modes of the
/// cracking tools. Ported from the ArcObjects CrackPointFeedback.
/// </summary>
public static class VertexLabelUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public const double SmallTextSize = 8;
	public const double LargeTextSize = 10;

	/// <summary>
	/// A vertex, together with the index of the part it belongs to and its index within
	/// that part.
	/// </summary>
	public struct VertexInfo
	{
		public VertexInfo(int partIndex, int vertexIndex, [NotNull] MapPoint point)
		{
			PartIndex = partIndex;
			VertexIndex = vertexIndex;
			Point = point;
		}

		public int PartIndex { get; }

		public int VertexIndex { get; }

		[NotNull]
		public MapPoint Point { get; }
	}

	#region Vertex access

	/// <summary>
	/// All vertices of the geometry, part by part. For a multipatch the parts are its
	/// patches (rings, triangle fans, ...), for a multipoint each point is its own part.
	/// </summary>
	[NotNull]
	public static IEnumerable<VertexInfo> GetVertices([CanBeNull] Geometry geometry)
	{
		if (geometry is null || geometry.IsEmpty)
		{
			yield break;
		}

		if (geometry is Multipatch multipatch)
		{
			for (var partIndex = 0; partIndex < multipatch.PartCount; partIndex++)
			{
				foreach (VertexInfo vertex in GetPatchVertices(multipatch, partIndex))
				{
					yield return vertex;
				}
			}

			yield break;
		}

		if (geometry is Multipart multipart)
		{
			for (var partIndex = 0; partIndex < multipart.PartCount; partIndex++)
			{
				foreach (VertexInfo vertex in GetMultipartVertices(multipart, partIndex))
				{
					yield return vertex;
				}
			}

			yield break;
		}

		if (geometry is Multipoint multipoint)
		{
			ReadOnlyPointCollection points = multipoint.Points;

			// Convention (as in GeometryUtils.GetPartCount): the i-th point is part i
			for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
			{
				yield return new VertexInfo(pointIndex, 0, points[pointIndex]);
			}

			yield break;
		}

		if (geometry is MapPoint mapPoint)
		{
			yield return new VertexInfo(0, 0, mapPoint);
		}
	}

	/// <summary>All vertices of a single part of the geometry.</summary>
	[NotNull]
	public static IEnumerable<VertexInfo> GetPartVertices([CanBeNull] Geometry geometry,
	                                                      int partIndex)
	{
		if (geometry is null || geometry.IsEmpty || partIndex < 0)
		{
			return new List<VertexInfo>();
		}

		if (geometry is Multipatch multipatch)
		{
			return partIndex < multipatch.PartCount
				       ? GetPatchVertices(multipatch, partIndex)
				       : new List<VertexInfo>();
		}

		if (geometry is Multipart multipart)
		{
			return partIndex < multipart.PartCount
				       ? GetMultipartVertices(multipart, partIndex)
				       : new List<VertexInfo>();
		}

		return partIndex == 0 ? GetVertices(geometry) : new List<VertexInfo>();
	}

	[NotNull]
	private static IEnumerable<VertexInfo> GetPatchVertices([NotNull] Multipatch multipatch,
	                                                        int partIndex)
	{
		int startIndex = multipatch.GetPatchStartPointIndex(partIndex);
		int pointCount = multipatch.GetPatchPointCount(partIndex);

		ReadOnlyPointCollection points = multipatch.Points;

		for (var i = 0; i < pointCount; i++)
		{
			yield return new VertexInfo(partIndex, i, points[startIndex + i]);
		}
	}

	[NotNull]
	private static IEnumerable<VertexInfo> GetMultipartVertices([NotNull] Multipart multipart,
	                                                            int partIndex)
	{
		ReadOnlySegmentCollection segments = multipart.Parts[partIndex];

		var vertexIndex = 0;

		foreach (Segment segment in segments)
		{
			yield return new VertexInfo(partIndex, vertexIndex++, segment.StartPoint);
		}

		if (segments.Count > 0)
		{
			// The to-point of the last segment is a vertex of its own. For a closed ring it
			// coincides with the from-point of the first segment, just like the point
			// collection of an ArcObjects ring.
			yield return new VertexInfo(partIndex, vertexIndex,
			                            segments[segments.Count - 1].EndPoint);
		}
	}

	#endregion

	#region Label texts

	/// <summary>
	/// The vertex label, e.g. "x=2 600 000.00000 | y=1 200 000.00000 | z=412.50000 |
	/// id=3 | part 1, vertex 7". Which components are included depends on the awareness
	/// of the geometry the vertex belongs to.
	/// </summary>
	[NotNull]
	public static string GetVertexLabel([NotNull] MapPoint vertex, int partIndex, int vertexIndex,
	                                    bool includeZ, bool includeM, bool includeIds)
	{
		var label = new StringBuilder();

		label.AppendFormat("x={0:N5} | y={1:N5}", vertex.X, vertex.Y);

		if (includeZ && ! double.IsNaN(vertex.Z))
		{
			label.AppendFormat(" | z={0:N5}", vertex.Z);
		}

		if (includeM && ! double.IsNaN(vertex.M))
		{
			label.AppendFormat(" | m={0:N5}", vertex.M);
		}

		if (includeIds)
		{
			label.AppendFormat(" | id={0}", vertex.ID);
		}

		label.AppendFormat(" | part {0}, vertex {1}", partIndex, vertexIndex);

		return label.ToString();
	}

	/// <summary>
	/// The patch type as displayed to the user. Note that the ArcGIS Pro API only
	/// distinguishes the five <see cref="PatchType"/> values; unlike ArcObjects it does
	/// not report whether a ring is an inner or an outer ring.
	/// </summary>
	[NotNull]
	public static string GetPatchTypeText(PatchType patchType)
	{
		switch (patchType)
		{
			case PatchType.FirstRing:
				return "First Ring";
			case PatchType.Ring:
				return "Ring";
			case PatchType.TriangleStrip:
				return "Triangle Strip";
			case PatchType.TriangleFan:
				return "Triangle Fan";
			case PatchType.Triangles:
				return "Triangles";
			default:
				return patchType.ToString();
		}
	}

	#endregion

	#region Symbols

	/// <summary>
	/// A text symbol with a white halo, corresponding to the halo mask of the ArcObjects
	/// label symbols. The offsets are in points and therefore independent of the map scale.
	/// </summary>
	[NotNull]
	public static CIMSymbolReference CreateLabelSymbol(
		double size = SmallTextSize,
		double offsetXPoints = 0,
		double offsetYPoints = 0,
		[CanBeNull] CIMColor color = null,
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment verticalAlignment = VerticalAlignment.Center)
	{
		CIMTextSymbol textSymbol = SymbolFactory.Instance.ConstructTextSymbol(
			color ?? ColorUtils.CreateRGB(100, 100, 100), size, "Arial", "Bold");

		textSymbol.HorizontalAlignment = horizontalAlignment;
		textSymbol.VerticalAlignment = verticalAlignment;

		textSymbol.OffsetX = offsetXPoints;
		textSymbol.OffsetY = offsetYPoints;

		textSymbol.HaloSize = 1;
		textSymbol.HaloSymbol =
			SymbolUtils.CreatePolygonSymbol(ColorUtils.CreateRGB(255, 255, 255));

		return textSymbol.MakeSymbolReference();
	}

	#endregion

	#region Ring geometry

	/// <summary>
	/// The patch at <paramref name="patchIndex"/> as a polygon, for display purposes.
	/// Returns null if the patch has too few points to form a ring.
	/// </summary>
	[CanBeNull]
	public static Polygon GetRingPolygon([NotNull] Multipatch multipatch, int patchIndex)
	{
		int startIndex = multipatch.GetPatchStartPointIndex(patchIndex);
		int pointCount = multipatch.GetPatchPointCount(patchIndex);

		if (pointCount < 3)
		{
			return null;
		}

		ReadOnlyPointCollection points = multipatch.Points;

		var ringPoints = new List<MapPoint>(pointCount);

		for (var i = 0; i < pointCount; i++)
		{
			ringPoints.Add(points[startIndex + i]);
		}

		AttributeFlags flags = AttributeFlags.None;

		if (multipatch.HasZ)
		{
			flags |= AttributeFlags.HasZ;
		}

		if (multipatch.HasM)
		{
			flags |= AttributeFlags.HasM;
		}

		if (multipatch.HasID)
		{
			flags |= AttributeFlags.HasID;
		}

		return PolygonBuilderEx.CreatePolygon(ringPoints, flags, multipatch.SpatialReference);
	}

	/// <summary>
	/// A point inside the geometry's extent to place a label at.
	/// </summary>
	/// <remarks>
	/// The centroid of a vertical wall is typically far outside its extent (in ArcObjects
	/// accessing it could even corrupt the geometry), hence the fall-back to the extent
	/// centre.
	/// </remarks>
	[CanBeNull]
	public static MapPoint GetLabelPoint([CanBeNull] Geometry geometry)
	{
		Envelope extent = geometry?.Extent;

		if (extent is null || extent.IsEmpty)
		{
			return null;
		}

		MapPoint labelPoint = null;

		try
		{
			labelPoint = GeometryEngine.Instance.Centroid(geometry);
		}
		catch (Exception e)
		{
			// A degenerate ring must not prevent the remaining rings from being labelled:
			// fall back to the centre of the extent below
			_msg.Debug("Error reading the label point (ignored)", e);
		}

		if (labelPoint is null || labelPoint.IsEmpty ||
		    labelPoint.X < extent.XMin || labelPoint.X > extent.XMax ||
		    labelPoint.Y < extent.YMin || labelPoint.Y > extent.YMax)
		{
			labelPoint = MapPointBuilderEx.CreateMapPoint(
				extent.XMin + extent.Width / 2, extent.YMin + extent.Height / 2,
				extent.SpatialReference);
		}

		return labelPoint;
	}

	#endregion
}
