using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Carto;

public static class DrawingOutline
{
	public class Options
	{
		public bool IgnoreErrors { get; set; } // and outline may be wrong
		public bool IgnoreDashing { get; set; } // and assume solid lines
		public bool IgnoreLineMarkers { get; set; } // TODO point markers still honored
		public bool FillGlyphCounters { get; set; } // TODO dt. Punzen entfernen
		public bool FillHoles { get; set; } // TODO probably want (relative) size limit
		public double MaxBufferDeviationPoints { get; set; } // see (Graphic)Buffer operation
		public int MaxVerticesInFullCircle { get; set; } // see (Graphic)Buffer operation
		public double ScaleFactor { get; set; } // to convert points to map units at reference scale!

		public Options()
		{
			// Set defaults:
			IgnoreErrors = false;
			IgnoreDashing = false;
			IgnoreLineMarkers = false;
			FillGlyphCounters = false;
			FillHoles = false;
			MaxBufferDeviationPoints = 0.1; // about 0.03mm
			MaxVerticesInFullCircle = 120;
			ScaleFactor = 1.0;
		}

		public Options(double scaleFactor, Options options)
		{
			// Copy values:
			IgnoreErrors = options.IgnoreErrors;
			IgnoreDashing = options.IgnoreDashing;
			IgnoreLineMarkers = options.IgnoreLineMarkers;
			FillGlyphCounters = options.FillGlyphCounters;
			FillHoles = options.FillHoles;
			MaxBufferDeviationPoints = options.MaxBufferDeviationPoints;
			MaxVerticesInFullCircle = options.MaxVerticesInFullCircle;
			ScaleFactor = scaleFactor;
		}
	}

	// Error granularity is: each individual effect and each symbol layer (incl marker placement)
	// On error, the affected effect is ignored, or the affected symbol layer omitted, and a message logged

	// TODO Ideas/improvements:
	// - just collect (properly oriented (inner/outer)) rings, union all in one last step (to replace the Simplify() sometimes used)
	// - Union() linearizes non-linear segments... keep non-linear segments?

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public static Options GetDefaultOptions(double scaleFactor = 1.0)
	{
		return new Options { ScaleFactor = scaleFactor };
	}

	/// <summary>
	/// Get a Polygon that represents the outline of the given <paramref name="shape"/>
	/// when drawing it with the given <paramref name="symbol"/>. Primitive overrides
	/// (aka property mappings, field overrides, etc.) are assumed to have already
	/// been applied to the given <paramref name="symbol"/>. Geometric effects, if any,
	/// are considered. Within <paramref name="options"/>, the scale factor is required
	/// to scale symbol dimensions (points) to desired map units at the reference scale:
	/// ScaleFactor = ReferenceScaleDenom / PointsPerMillimeter / MetersPerMapLinearUnit,
	/// where PointsPerMillimeter ≈ 2.83.
	/// </summary>
	[CanBeNull]
	public static Geometry GetOutline(Geometry shape, CIMSymbol symbol, Options options)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));
		if (symbol is null)
			throw new ArgumentNullException(nameof(symbol));
		if (options is null)
			throw new ArgumentNullException(nameof(options));

		// All MultiLayerSymbols have:
		//  .Effects: array of GeometricEffect (global effects)
		//  .SymbolLayers: array of SymbolLayer
		//  .UseRealWorldSymbolSizes: bool // size properties represent real world units (true) or page units (false)
		// Known MultiLayerSymbol subtypes are:
		//  PointSymbol (has extra properties: Angle, Callout, etc.)
		//  LineSymbol (no extra properties)
		//  PolygonSymbol (no extra properties)
		//  MeshSymbol

		switch (symbol)
		{
			case CIMPointSymbol pointSymbol:
				return GetOutline(shape, pointSymbol, options);

			case CIMLineSymbol lineSymbol:
				return GetOutline(shape, lineSymbol, options);

			case CIMPolygonSymbol polygonSymbol:
				return GetOutline(shape, polygonSymbol, options);

			case CIMTextSymbol:
				// We've no text string here and thus no outline!
				// TextSymbol may meaningfully occur in CIMMarkerSymbol
				return null;

			default:
				throw new NotSupportedException($"Symbol type not supported: {symbol.GetType().Name}");
		}
	}

	[CanBeNull]
	private static Polygon GetOutline(Geometry shape, CIMPointSymbol symbol, Options options)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));
		if (symbol is null)
			throw new ArgumentNullException(nameof(symbol));
		if (options is null)
			throw new ArgumentNullException(nameof(options));

		if (symbol.Effects is { Length: > 0 })
		{
			shape = ApplyEffects(shape, symbol.Effects, options);
		}

		if (shape is null) return null; // extinguished by effects

		// PointSymbol has these extra properties:
		//symbol.Angle: double (degrees)
		//symbol.AngleAlignment: Display|Map
		//symbol.Callout; // throw/ignore
		//symbol.HaloSize; // extends beyond symbol shape
		//symbol.HaloSymbol; // a PolygonSymbol
		//symbol.ScaleX; // a ratio

		if (symbol.Angle > 0 || symbol.Angle < 0)
			throw new NotImplementedException(
				"Point symbol has non-zero Angle, which is not yet implemented");

		if (symbol.Callout != null)
			throw new NotImplementedException(
				"Point symbol has a Callout, which is not yet implemented"); // TODO or silently ignore?

		if (symbol.HaloSymbol != null && symbol.HaloSize > 0)
			throw new NotImplementedException(
				"Point symbol has non-zero HaloSize, which is not yet implemented"); // TODO or silently ignore?
		// How does Halo work? --> Buffer symbol by HaloSize and draw this shape with HaloSymbol?

		if (symbol.ScaleX > 1 || symbol.ScaleX < 1)
			throw new NotImplementedException(
				"Point symbol has non-unit ScaleX, which is not yet implemented");

		return RenderLayers(shape, symbol.SymbolLayers, options);
	}

	[CanBeNull]
	private static Polygon GetOutline(Geometry shape, CIMMultiLayerSymbol symbol, Options options)
	{
		if (shape is null)
			throw new ArgumentNullException(nameof(shape));
		if (symbol is null)
			throw new ArgumentNullException(nameof(symbol));
		if (options is null)
			throw new ArgumentNullException(nameof(options));

		if (symbol.Effects is { Length: > 0 })
		{
			shape = ApplyEffects(shape, symbol.Effects, options);
		}

		if (shape is null) return null; // extinguished by effects

		return RenderLayers(shape, symbol.SymbolLayers, options);
	}

	private static Polygon RenderLayers(Geometry shape, CIMSymbolLayer[] layers, Options options)
	{
		var list = new List<Polygon>();

		foreach (var symbolLayer in layers.Where(l => l.Enable))
		{
			try
			{
				var layerOutline = GetLayerOutline(shape, symbolLayer, options);
				// TODO simplify layerOutline --> fix ring orientation, or the Union below may create empty geoms
				list.Add(layerOutline);
			}
			catch (Exception ex)
			{
				if (!options.IgnoreErrors) throw;
				_msg.Warn($"Cannot {nameof(GetLayerOutline)}: {ex.Message}", ex);
			}
		}

		var outline = GeometryEngine.Instance.Union(list.Where(g => g != null));
		outline = GeometryUtils.Simplify(outline, true);
		return outline as Polygon;
	}

	[CanBeNull]
	private static Polygon GetLayerOutline(Geometry shape, CIMSymbolLayer layer, Options options)
	{
		// All SymbolLayers have:
		//  .Effects: array of GeometricEffect (local effects)
		//  (and a few others, which are irrelevant here)
		// Known SymbolLayer subtypes are:
		// - Fill, subclasses: Solid, Gradient, Hatch, ...
		// - Stroke, subclasses: Solid, Gradient, Picture, ...
		// - Marker, subclasses: Vector, Character, Picture, ...
		// (and a few others)

		if (layer.Effects is { Length: > 0 })
		{
			shape = ApplyEffects(shape, layer.Effects, options);
		}

		if (shape is null) return null; // extinguished by effects

		double scaleFactor = options.ScaleFactor;

		if (layer is CIMFill)
		{
			// Specific subtype (Solid, Gradient, etc) is irrelevant here:
			// outline is always the input polygon shape
			return shape as Polygon;
		}

		if (layer is CIMStroke stroke)
		{
			// Specific subtype (Solid, Gradient, Picture) is irrelevant here
			// Buffer all paths (if any) of the given shape:
			double distance = stroke.Width * scaleFactor / 2;
			var joinType = GetJoinType(stroke.JoinStyle);
			var capType = GetCapType(stroke.CapStyle);
			double miterLimit = stroke.MiterLimit;
			double maxDeviation = options.MaxBufferDeviationPoints * scaleFactor;
			int maxVerticesInFullCircle = options.MaxVerticesInFullCircle;
			var strokeOutline = GeometryEngine.Instance.GraphicBuffer(
				shape, distance, joinType, capType, miterLimit,
				maxDeviation, maxVerticesInFullCircle);
			return strokeOutline as Polygon;
		}

		if (layer is CIMCharacterMarker charMarker)
		{
			// CIMMarker properties include:
			// .AnchorPoint: MapPoint
			// .AnchorPointUnits: SymbolUnits (Relative|Absolute)
			// .OffsetX, OffsetY, OffsetZ: double (applied after rotation; AnchorPoint applies before rotation)
			// .RotateClockwise: bool
			// .Rotation: double (degrees, around AnchorPoint)
			// .Size: double (marker height; width adjusted proportionally)
			// .MarkerPlacement: CIMMarkerPlacement (about 12 subtypes)

			// CIMCharacterMarker properties include:
			// .CharacterIndex: int (Unicode value)
			// .FontFamilyName: string
			// .FontStyleName: string
			// .FontType: FontType (Unspecified,TrueType,PSOpenType,TTOpenType,Type1)
			// .FontVariationSettings: CIMFontVariation[] (TagName/Value pairs, depends on font, don't understand)
			// .ScaleX: double (width as a ratio, leaving height unmodified)
			// .Symbol: CIMPolygonSymbol
			// .ScaleSymbolsProportionally: bool (iff true, scale outline and fill strokes proportionally to marker size)
			// .RespectFrame: bool (don't understand)

			int codePoint = charMarker.CharacterIndex;
			double fontSize = charMarker.Size;
			string familyName = charMarker.FontFamilyName;
			string styleName = charMarker.FontStyleName;

			var typeface = GlyphUtils.FindGlyphTypeface(familyName, styleName);
			if (typeface is null)
			{
				VerboseDebug("No font found for family '{0}', style '{1}", familyName, styleName);
				return null;
			}

			var glyph = GlyphUtils.GetGlyph(codePoint, typeface, fontSize, out var advance);
			if (glyph is null)
			{
				VerboseDebug($"Code point U+{codePoint:X4} has no glyph in font");
				return null;
			}

			Geometry outline = GlyphUtils.ToEsriPolygon(glyph);

			// empirical: use metrics for horizontal, glyph box for vertical
			var magic1 = glyph.Bounds with { X = 0, Width = advance };

			double ax = magic1.X + magic1.Width / 2;
			double ay = -magic1.Y - magic1.Height / 2;

			var anchor = charMarker.AnchorPoint;
			var anchorUnits = charMarker.AnchorPointUnits;
			if (anchor != null)
			{
				// empirical: Pro seems to add a 1pt margin on all sides
				var magic2 = glyph.Bounds;
				magic2.Inflate(1, 1);

				switch (anchorUnits)
				{
					case SymbolUnits.Relative:
						ax += anchor.X * magic2.Width;
						ay += anchor.Y * magic2.Height;
						break;
					case SymbolUnits.Absolute:
						ax += anchor.X;
						ay += anchor.Y;
						break;
					default:
						throw new ArgumentOutOfRangeException($"Unknown {nameof(SymbolUnits)}: {anchorUnits}");
				}
			}

			// NB. Cannot combine ax/ay with dx/dy below, because ax/ay occur before scale+rotate
			outline = GeometryUtils.Move(outline, -ax, -ay);

			double sx = charMarker.ScaleX * scaleFactor;
			double sy = 1.0 * scaleFactor;
			double radians = charMarker.Rotation * Math.PI / 180;
			if (charMarker.RotateClockwise) radians *= -1;
			double dx = charMarker.OffsetX * scaleFactor;
			double dy = charMarker.OffsetY * scaleFactor;

			outline = TransformMarker(outline, sx, sy, radians, dx, dy);

			if (charMarker.Symbol is { SymbolLayers: { } })
			{
				bool scaleStrokes = charMarker.ScaleSymbolsProportionally; // relative to Size=10pt (?)

				// Effects seem to be ignored for embedded symbols (at least not accessible in UI).
				// Here: account for outline strokes but ignore markers and effects.
				// TODO ALTERNATIVELY (and more correctly), recurse GetOutline(outline, charMarker.Symbol)
				// (as we do with Vector markers)
				var strokes = charMarker.Symbol.SymbolLayers.OfType<CIMStroke>().ToArray();
				if (strokes.Any())
				{
					var maxStroke = strokes.MaxBy(s => s.Width);
					if (maxStroke.Width > 0)
					{
						var distance = maxStroke.Width * scaleFactor / 2;
						if (scaleStrokes) distance *= charMarker.Size / 10.0; // empirical
						var joinType = GetJoinType(maxStroke.JoinStyle);
						var capType = GetCapType(maxStroke.CapStyle);
						var miterLimit = maxStroke.MiterLimit * scaleFactor;
						double maxDeviation = options.MaxBufferDeviationPoints;
						int maxVerticesInFullCircle = options.MaxVerticesInFullCircle;
						outline = GeometryEngine.Instance.GraphicBuffer(
							outline, distance, joinType, capType, miterLimit,
							maxDeviation, maxVerticesInFullCircle);
					}
				}
			}

			return PlaceMarker(outline, charMarker.MarkerPlacement, shape, options);
		}

		if (layer is CIMVectorMarker vectorMarker)
		{
			// CIMMarker properties: see above (Anchor, Offset, Rotate, Size, Placement)
			// CIMVectorMarker properties:
			// .Frame: Envelope (Core.Geometry, not CIM.Geometry)
			// .MarkerGraphics: CIMMarkerGraphic[]
			// .ScaleSymbolsProportionally:
			// .RespectFrame: bool
			// .ClippingPath: CIMClippingPath (essentially a Polygon)
			var graphics = vectorMarker.MarkerGraphics;
			if (graphics is not { Length: > 0 })
				return null; // marker has no graphics
			var frame = vectorMarker.Frame;
			bool respectFrame = vectorMarker.RespectFrame; // TODO don't understand
			bool scaleStrokes = vectorMarker.ScaleSymbolsProportionally; // relative to Size=10pt ???
			if (vectorMarker.ClippingPath?.Path != null)
				throw new NotImplementedException(
					$"{nameof(vectorMarker.ClippingPath)} on vector marker is not yet supported");

			var list = new List<Geometry>();
			foreach (var graphic in graphics)
			{
				var innerOptions = GetInnerOptions(options);
				// simplify graphic's geometry to fix ring orientations
				var graphicShape = GeometryUtils.Simplify(graphic.Geometry);
				var graphicOutline = GetOutline(graphicShape, graphic.Symbol, innerOptions);
				if (graphicOutline is Polygon polygon)
				{
					var withSRef = PolygonBuilderEx.CreatePolygon(polygon, shape.SpatialReference);
					list.Add(withSRef);
				}
			}

			var outline = GeometryEngine.Instance.Union(list);

			var box = respectFrame ? frame : outline.Extent;
			outline = GeometryEngine.Instance.Move(outline, -box.Center.X, -box.Center.Y);

			var anchor = vectorMarker.AnchorPoint;
			var anchorUnits = vectorMarker.AnchorPointUnits;
			if (anchor != null)
			{
				double ax = anchor.X;
				double ay = anchor.Y;

				switch (anchorUnits)
				{
					case SymbolUnits.Relative:
						ax *= box.Height; // sic (empirical)
						ay *= box.Height;
						break;
					case SymbolUnits.Absolute:
						// will scale back to Size later
						ax *= box.Height / vectorMarker.Size;
						ay *= box.Height / vectorMarker.Size;
						break;
					default:
						throw new NotSupportedException($"Unknown {nameof(vectorMarker.AnchorPointUnits)}: {anchorUnits}");
				}

				outline = GeometryEngine.Instance.Move(outline, -ax, -ay);
			}

			double sx = scaleFactor * vectorMarker.Size / box.Height;
			double sy = scaleFactor * vectorMarker.Size / box.Height;
			double radians = vectorMarker.Rotation * Math.PI / 180;
			if (vectorMarker.RotateClockwise) radians *= -1;
			double dx = vectorMarker.OffsetX * scaleFactor;
			double dy = vectorMarker.OffsetY * scaleFactor;

			outline = TransformMarker(outline, sx, sy, radians, dx, dy);

			return PlaceMarker(outline, vectorMarker.MarkerPlacement, shape, options);
		}

		throw new NotSupportedException(
			$"Symbol layer of type {layer.GetType().Name} is not supported");
	}

	private static Geometry TransformMarker(Geometry outline, double sx, double sy,
	                                        double angleRadCcw, double dx, double dy)
	{
		var origin = MapPointBuilderEx.CreateMapPoint(0, 0);
		// Pro SDK badly lacks an affine transformation method!
		outline = GeometryEngine.Instance.Scale(outline, origin, sx, sy);
		outline = GeometryEngine.Instance.Rotate(outline, origin, angleRadCcw);
		outline = GeometryEngine.Instance.Move(outline, dx, dy);
		return outline;
	}

	#region Geometric Effects

	private static Geometry ApplyEffects(
		Geometry shape, IEnumerable<CIMGeometricEffect> effects, Options options)
	{
		foreach (var effect in effects ?? Enumerable.Empty<CIMGeometricEffect>())
		{
			if (effect is CIMGeometricEffectDashes && options.IgnoreDashing) continue;

			try
			{
				shape = ApplyEffect(shape, effect, options);
			}
			catch (Exception ex)
			{
				if (! options.IgnoreErrors) throw;
				_msg.Warn($"Cannot {nameof(ApplyEffect)}: {ex.Message}", ex);
			}
		}

		return shape;
	}

	private static Geometry ApplyEffect(
		Geometry shape, CIMGeometricEffect effect, Options options)
	{
		if (shape is null) return null;
		if (effect is null) return shape;

		// GeometricEffect properties:
		// .PrimitiveName: string (for overrides) (ignored here: we assume overrides to have been materialized)
		// Some 25 subclasses...

		double scaleFactor = options.ScaleFactor;

		switch (effect)
		{
			case CIMGeometricEffectAddControlPoints addCPs:
				return GeometricEffects.AddControlPoints(shape, addCPs.AngleTolerance);

			case CIMGeometricEffectCut cut:
				var beginCut = cut.BeginCut * scaleFactor;
				var endCut = cut.EndCut * scaleFactor;
				var middleCut = cut.MiddleCut * scaleFactor;
				return GeometricEffects.Cut(shape, beginCut, endCut, cut.Invert, middleCut);

			case CIMGeometricEffectDashes dashes:
				var pattern = ScaleArray(dashes.DashTemplate, scaleFactor);
				var endingsType = GetDashEndings(dashes.LineDashEnding);
				var controlPointType = GetDashEndings(dashes.ControlPointEnding);
				var offsetAlong = dashes.OffsetAlongLine * scaleFactor;
				var customEndOffset = dashes.CustomEndingOffset * scaleFactor;
				return GeometricEffects.Dashes(shape, pattern, offsetAlong, endingsType, controlPointType, customEndOffset);

			case CIMGeometricEffectOffset offset:
				var distance = offset.Offset * scaleFactor;
				var method = GetOffsetMethod(offset.Method);
				// ignore offset.Option (Fast|Accurate)
				return GeometricEffects.Offset(shape, distance, method);

			case CIMGeometricEffectReverse reverse:
				return reverse.Reverse
					       ? GeometricEffects.Reverse(shape)
					       : shape;

			case CIMGeometricEffectSuppress suppress:
				return suppress.Suppress
					       ? GeometricEffects.Suppress(shape, suppress.Invert)
					       : shape;

			default:
				throw new NotSupportedException(
					$"Geometric effect {effect.GetType().Name} is not supported");
		}
	}

	private static GeometricEffects.DashEndings GetDashEndings(LineDashEnding ending)
	{
		switch (ending)
		{
			case LineDashEnding.NoConstraint:
				return GeometricEffects.DashEndings.Unconstrained;
			case LineDashEnding.HalfPattern:
				return GeometricEffects.DashEndings.HalfDash;
			case LineDashEnding.HalfGap:
				return GeometricEffects.DashEndings.HalfGap;
			case LineDashEnding.FullPattern:
				return GeometricEffects.DashEndings.FullDash;
			case LineDashEnding.FullGap:
				return GeometricEffects.DashEndings.FullGap;
			case LineDashEnding.Custom:
				return GeometricEffects.DashEndings.Custom;
			default:
				throw new ArgumentOutOfRangeException(nameof(ending), ending, null);
		}
	}

	private static OffsetType GetOffsetMethod(GeometricEffectOffsetMethod method)
	{
		switch (method)
		{
			case GeometricEffectOffsetMethod.Mitered:
				return OffsetType.Miter;
			case GeometricEffectOffsetMethod.Bevelled:
				return OffsetType.Bevel;
			case GeometricEffectOffsetMethod.Rounded:
				return OffsetType.Round;
			case GeometricEffectOffsetMethod.Square:
				return OffsetType.Square;
			default:
				throw new ArgumentOutOfRangeException(nameof(method), method, null);
		}
	}

	#endregion

	#region Marker Placement

	private static Polygon PlaceMarker(Geometry marker, CIMMarkerPlacement placement,
	                                   Geometry reference, Options options)
	{
		var list = ApplyPlacement(marker, placement, reference, options).ToList();
		if (list is not { Count: > 0 }) return null; // unplaced or placed away
		var combined = GeometryEngine.Instance.Union(list);

		// If not a polygon, the outline is empty, which we represent with null:
		if (combined is not Polygon polygon) return null;

		// Placed marker's SRef is the reference geometry's SRef:
		if (reference.SpatialReference != null && !SameSRef(polygon, reference))
		{
			polygon = PolygonBuilderEx.CreatePolygon(polygon, reference.SpatialReference);
		}

		return polygon;
	}

	private static IEnumerable<Geometry> ApplyPlacement(
		Geometry marker, CIMMarkerPlacement placement, Geometry reference, Options options)
	{
		// CIMMarkerPlacement: PlacePerPart (bool)
		//   CIMMarkerFillPlacement
		//     CIMMarkerPlacementPolygonCenter: Method (OnPolygon|CenterOfMass|BoundingBoxCenter), OffsetX, OffsetY, ClipAtBoundary
		//     CIMMarkerPlacementInsidePolygon: may be random... and difficult... just ignore (or throw NotSupported)
		//     CIMMarkerPlacementAroundPolygon: Position (Top|Bottom|Left|Right|TopLeft|TopRight|BottomLeft|BottomRight), Offset (double)
		//   CIMMarkerStrokePlacement: AngleToLine (bool), Offset (double)
		//     CIMMarkerPlacementAlongLineSameSize: PlacementTemplate (double[]), Endings (NoConstraint|WithMarkers|WithFullGap|WithHalfGap|Custom), CustomEndingOffset (double), OffsetAlongLine (double)
		//     CIMMarkerPlacementAlongLineRandomSize: same properties, plus: Randomization (Low|Medium|High), Seed (int)
		//     CIMMarkerPlacementAlongLineVariableSize: randomized... and difficult ... just ignore (or throw NotSupported)
		//     CIMMarkerPlacementAtExtremities: ExtremityPlacement (Both|JustBegin|JustEnd|None), OffsetAlongLine (double)
		//     CIMMarkerPlacementAtMeasuredUnits: ... just ignore (or throw NotSupported)
		//     CIMMarkerPlacementAtRatioPositions: BeginPosition, EndPosition, FlipFirst (bool), PositionArray (double[])
		//     CIMMarkerPlacementOnLine: RelativeTo (LineMiddle|LineBeginning|LineEnd|SegmentMidpoint), StartPointOffset (double)
		//     CIMMarkerPlacementOnVertices: PlaceOnControlPoints (bool), PlaceOnEndPoints (bool), PlaceOnRegularVertices (bool)

		if (marker is null) return Enumerable.Empty<Geometry>();
		if (reference is null) return Enumerable.Empty<Geometry>();

		if (placement is null)
		{
			// CIMPointSymbol has no MarkerPlacement (the respective property
			// on the symbol layers is null); the marker draws on the point:
			return MarkerPlacements.OnPoint(marker, reference as MapPoint);
		}

		var scaleFactor = options.ScaleFactor;

		switch (placement)
		{
			// Fill marker placements:

			case CIMMarkerPlacementPolygonCenter polygonCenter:
				return MarkerPlacements.PolygonCenter(
					marker, reference, GetOptions(polygonCenter, scaleFactor));
			case CIMMarkerPlacementAroundPolygon aroundPolygon:
				return MarkerPlacements.AroundPolygon(
					marker, reference, GetOptions(aroundPolygon, scaleFactor));

			// Stroke marker placements:

			case CIMMarkerPlacementAtExtremities atExtremities:
				return MarkerPlacements.AtExtremities(
					marker, reference, GetOptions(atExtremities, scaleFactor));
			case CIMMarkerPlacementOnVertices onVertices:
				return MarkerPlacements.OnVertices(
					marker, reference, GetOptions(onVertices, scaleFactor));
			case CIMMarkerPlacementOnLine onLine:
				return MarkerPlacements.OnLine(
					marker, reference, GetOptions(onLine, scaleFactor));
			case CIMMarkerPlacementAlongLine alongLine:
				return MarkerPlacements.AlongLine(
					marker, reference, GetOptions(alongLine, scaleFactor));

			// All others are randomized, too complicated, or too unlikely in topo carto

			default:
				throw new NotSupportedException(
					$"Marker placement {placement.GetType().Name} is not supported");
		}
	}

	private static MarkerPlacements.PolygonCenterOptions GetOptions(
		CIMMarkerPlacementPolygonCenter cim, double scaleFactor = 1.0)
	{
		return new MarkerPlacements.PolygonCenterOptions
		       {
			       PlacePerPart = cim.PlacePerPart,

			       UseBoundingBox = cim.Method == PlacementPolygonCenterMethod.BoundingBoxCenter,
			       ForceInsidePolygon = cim.Method == PlacementPolygonCenterMethod.OnPolygon,
			       OffsetX = cim.OffsetX * scaleFactor,
			       OffsetY = cim.OffsetY * scaleFactor
		       };
	}

	private static MarkerPlacements.AroundPolygonOptions GetOptions(
		CIMMarkerPlacementAroundPolygon cim, double scaleFactor = 1.0)
	{
		var position = cim.Position switch
		{
			PlacementAroundPolygonPosition.Top =>
				MarkerPlacements.AroundPolygonPosition.Top,
			PlacementAroundPolygonPosition.Bottom =>
				MarkerPlacements.AroundPolygonPosition.Bottom,
			PlacementAroundPolygonPosition.Left =>
				MarkerPlacements.AroundPolygonPosition.Left,
			PlacementAroundPolygonPosition.Right =>
				MarkerPlacements.AroundPolygonPosition.Right,
			PlacementAroundPolygonPosition.TopLeft =>
				MarkerPlacements.AroundPolygonPosition.TopLeft,
			PlacementAroundPolygonPosition.TopRight =>
				MarkerPlacements.AroundPolygonPosition.TopRight,
			PlacementAroundPolygonPosition.BottomLeft =>
				MarkerPlacements.AroundPolygonPosition.BottomLeft,
			PlacementAroundPolygonPosition.BottomRight =>
				MarkerPlacements.AroundPolygonPosition.BottomRight,
			_ => throw new ArgumentOutOfRangeException()
		};

		return new MarkerPlacements.AroundPolygonOptions
		       {
			       Position = position,
			       Offset = cim.Offset * scaleFactor
		       };
	}

	private static MarkerPlacements.AtExtremitiesOptions GetOptions(
		CIMMarkerPlacementAtExtremities cim, double scaleFactor = 1.0)
	{
		MarkerPlacements.Extremity extremities = cim.ExtremityPlacement switch
		{
			ExtremityPlacement.Both => MarkerPlacements.Extremity.Both,
			ExtremityPlacement.JustBegin => MarkerPlacements.Extremity.JustBegin,
			ExtremityPlacement.JustEnd => MarkerPlacements.Extremity.JustEnd,
			ExtremityPlacement.None => MarkerPlacements.Extremity.None,
			_ => throw new ArgumentOutOfRangeException($"Unknown {nameof(ExtremityPlacement)} {cim.ExtremityPlacement}")
		};

		return new MarkerPlacements.AtExtremitiesOptions
		       {
			       PlacePerPart = cim.PlacePerPart,

			       AngleToLine = cim.AngleToLine,
			       PerpendicularOffset = cim.Offset * scaleFactor,

			       Extremity = extremities,
			       OffsetAlongLine = cim.OffsetAlongLine * scaleFactor
		       };
	}

	private static MarkerPlacements.OnVerticesOptions GetOptions(
		CIMMarkerPlacementOnVertices cim, double scaleFactor = 1.0)
	{
		return new MarkerPlacements.OnVerticesOptions
		       {
			       PlacePerPart = cim.PlacePerPart,

			       AngleToLine = cim.AngleToLine,
			       PerpendicularOffset = cim.Offset * scaleFactor,

			       PlaceOnRegularVertices = cim.PlaceOnRegularVertices,
			       PlaceOnControlPoints = cim.PlaceOnControlPoints,
			       PlaceOnEndPoints = cim.PlaceOnEndPoints
		       };
	}

	private static MarkerPlacements.OnLineOptions GetOptions(
		CIMMarkerPlacementOnLine cim, double scaleFactor = 1.0)
	{
		var position = cim.RelativeTo switch
		{
			PlacementOnLineRelativeTo.LineMiddle => MarkerPlacements.OnLinePosition.Middle,
			PlacementOnLineRelativeTo.LineBeginning => MarkerPlacements.OnLinePosition.Start,
			PlacementOnLineRelativeTo.LineEnd => MarkerPlacements.OnLinePosition.End,
			PlacementOnLineRelativeTo.SegmentMidpoint => MarkerPlacements.OnLinePosition
				.SegmentMidpoints,
			_ => throw new ArgumentOutOfRangeException($"Unknown {nameof(PlacementOnLineRelativeTo)} {cim.RelativeTo}")
		};

		return new MarkerPlacements.OnLineOptions
		       {
			       PlacePerPart = cim.PlacePerPart,

			       AngleToLine = cim.AngleToLine,
			       PerpendicularOffset = cim.Offset * scaleFactor,

			       RelativeTo = position,
			       StartPointOffset = cim.StartPointOffset * scaleFactor
		       };
	}

	private static MarkerPlacements.AlongLineOptions GetOptions(
		CIMMarkerPlacementAlongLine cim, double scaleFactor)
	{
		var endings = cim.Endings switch
		{
			PlacementEndings.NoConstraint => MarkerPlacements.EndingsType.Unconstrained,
			PlacementEndings.WithMarkers => MarkerPlacements.EndingsType.Marker,
			PlacementEndings.WithHalfGap => MarkerPlacements.EndingsType.HalfStep,
			PlacementEndings.WithFullGap => MarkerPlacements.EndingsType.FullStep,
			PlacementEndings.Custom => MarkerPlacements.EndingsType.Custom,
			_ => throw new ArgumentOutOfRangeException($"Unknown {nameof(PlacementEndings)} {cim.Endings}")
		};

		return new MarkerPlacements.AlongLineOptions
		       {
			       PlacePerPart = cim.PlacePerPart,

			       AngleToLine = cim.AngleToLine,
			       PerpendicularOffset = cim.Offset * scaleFactor,

			       Pattern = ScaleArray(cim.PlacementTemplate, scaleFactor),
			       Endings = endings,
			       OffsetAlongLine = cim.OffsetAlongLine * scaleFactor,
			       CustomEndingOffset = cim.CustomEndingOffset * scaleFactor
		       };
	}

	#endregion

	private static Options GetInnerOptions(Options options)
	{
		return new Options(1.0, options);
	}

	private static void VerboseDebug(string format, params object[] args)
	{
		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.DebugFormat(format, args);
		}
	}

	private static double[] ScaleArray(double[] pattern, double scaleFactor)
	{
		if (pattern is null) return null;
		if (pattern.Length < 1) return pattern;
		if (!(scaleFactor > 0)) return pattern;
		return pattern.Select(number => number * scaleFactor).ToArray();
	}

	private static bool SameSRef(Geometry a, Geometry b)
	{
		if (a.SpatialReference is null && b.SpatialReference is null) return true;
		if (a.SpatialReference is null || b.SpatialReference is null) return false;
		return SpatialReference.AreEqual(a.SpatialReference, b.SpatialReference);
	}

	private static LineCapType GetCapType(LineCapStyle capStyle)
	{
		switch (capStyle)
		{
			case LineCapStyle.Butt:
				return LineCapType.Butt;
			case LineCapStyle.Round:
				return LineCapType.Round;
			case LineCapStyle.Square:
				return LineCapType.Square;
			default:
				throw new ArgumentOutOfRangeException(nameof(capStyle), capStyle, null);
		}
	}

	private static LineJoinType GetJoinType(LineJoinStyle joinStyle)
	{
		switch (joinStyle)
		{
			case LineJoinStyle.Bevel:
				return LineJoinType.Bevel;
			case LineJoinStyle.Round:
				return LineJoinType.Round;
			case LineJoinStyle.Miter:
				return LineJoinType.Miter;
			default:
				throw new ArgumentOutOfRangeException(nameof(joinStyle), joinStyle, null);
		}
	}
}
