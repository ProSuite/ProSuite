using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Draws the areas that a reshape adds to (green hatch) and removes from (red hatch) the
/// original polygons.
/// </summary>
public class AreaDifferencePreview
{
	private CIMPolygonSymbol _addAreaSymbol;
	private CIMPolygonSymbol _removeAreaSymbol;

	private IDisposable _addAreaOverlay;
	private IDisposable _removeAreaOverlay;

	/// <summary>
	/// Must be called on the QueuedTask.
	/// </summary>
	public void Update([NotNull] IEnumerable<ResultFeature> resultFeatures)
	{
		Update(AreaDifferences.Calculate(resultFeatures));
	}

	/// <summary>
	/// Must be called on the QueuedTask.
	/// </summary>
	public void Update([NotNull] AreaDifferences differences)
	{
		Clear();

		if (differences.IsEmpty)
		{
			return;
		}

		// No outline, so that the selection outline remains visible between the hatch lines
		_addAreaSymbol ??=
			SymbolUtils.CreateHatchFillSymbol(0, 255, 0, 90, lineWidth: 1.5, outline: false);
		_removeAreaSymbol ??=
			SymbolUtils.CreateHatchFillSymbol(255, 0, 0, lineWidth: 1.5, outline: false);

		_addAreaOverlay = AddOverlay(differences.Added, _addAreaSymbol);
		_removeAreaOverlay = AddOverlay(differences.Removed, _removeAreaSymbol);
	}

	public void Clear()
	{
		_addAreaOverlay?.Dispose();
		_addAreaOverlay = null;

		_removeAreaOverlay?.Dispose();
		_removeAreaOverlay = null;
	}

	[CanBeNull]
	private static IDisposable AddOverlay([CanBeNull] Geometry geometry,
	                                      [NotNull] CIMSymbol cimSymbol)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return null;
		}

		return MapView.Active.AddOverlay(geometry, cimSymbol.MakeSymbolReference());
	}
}

/// <summary>
/// The areas that reshape results add to and remove from the original polygons.
/// </summary>
public class AreaDifferences
{
	private AreaDifferences([CanBeNull] Geometry added, [CanBeNull] Geometry removed)
	{
		Added = added;
		Removed = removed;
	}

	[CanBeNull]
	public Geometry Added { get; }

	[CanBeNull]
	public Geometry Removed { get; }

	public bool IsEmpty => IsNullOrEmpty(Added) && IsNullOrEmpty(Removed);

	[NotNull]
	public static AreaDifferences Calculate([NotNull] IEnumerable<ResultFeature> resultFeatures)
	{
		var addGeometries = new List<Geometry>();
		var removeGeometries = new List<Geometry>();

		foreach (ResultFeature resultFeature in resultFeatures)
		{
			var sourcePoly = resultFeature.OriginalFeature.GetShape() as Polygon;

			if (sourcePoly == null || sourcePoly.IsEmpty)
			{
				continue;
			}

			var reshapedPoly = (Polygon) resultFeature.NewGeometry;

			addGeometries.Add(GeometryEngine.Instance.Difference(reshapedPoly, sourcePoly));
			removeGeometries.Add(GeometryEngine.Instance.Difference(sourcePoly, reshapedPoly));
		}

		return new AreaDifferences(Union(addGeometries), Union(removeGeometries));
	}

	[NotNull]
	public static AreaDifferences Union([NotNull] ICollection<AreaDifferences> differences)
	{
		var added = new List<Geometry>();
		var removed = new List<Geometry>();

		foreach (AreaDifferences difference in differences)
		{
			if (! IsNullOrEmpty(difference.Added))
			{
				added.Add(difference.Added);
			}

			if (! IsNullOrEmpty(difference.Removed))
			{
				removed.Add(difference.Removed);
			}
		}

		return new AreaDifferences(Union(added), Union(removed));
	}

	public bool IsSameArea([NotNull] AreaDifferences other)
	{
		return IsSameArea(Added, other.Added) && IsSameArea(Removed, other.Removed);
	}

	private static bool IsSameArea([CanBeNull] Geometry geometry1,
	                               [CanBeNull] Geometry geometry2)
	{
		if (IsNullOrEmpty(geometry1) || IsNullOrEmpty(geometry2))
		{
			return IsNullOrEmpty(geometry1) && IsNullOrEmpty(geometry2);
		}

		return GeometryEngine.Instance.Equals(geometry1, geometry2);
	}

	[CanBeNull]
	private static Geometry Union([NotNull] ICollection<Geometry> geometries)
	{
		return geometries.Count == 0 ? null : GeometryEngine.Instance.Union(geometries);
	}

	private static bool IsNullOrEmpty([CanBeNull] Geometry geometry)
	{
		return geometry == null || geometry.IsEmpty;
	}
}
