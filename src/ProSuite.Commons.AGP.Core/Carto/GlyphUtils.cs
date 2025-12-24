using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Geom;
using Geometry = System.Windows.Media.Geometry;
using LineSegment = System.Windows.Media.LineSegment;

namespace ProSuite.Commons.AGP.Core.Carto;

public static class GlyphUtils
{
	// See https://learn.microsoft.com/en-us/typography/about
	// See https://freetype.org/freetype2/docs/glyphs/glyphs-3.html
	// A font-related bibliography may be found at
	// https://learn.microsoft.com/en-us/typography/develop/character-design-standards/
	//
	// Note: For outlining a string, use System.Windows.Media.FormattedText,
	// which has some metrics and BuildGeometry() to get outline geometry.
	//
	// var all = Fonts.SystemFontFamilies; // see also: SystemFonts
	// var more = Fonts.GetFontFamilies(baseUri, location);
	// var fiat = new FontFamily("ESRI Pipeline US 1");
	// var typeface = new Typeface(fiat, FontStyles.Normal, FontWeights.Regular, FontStretches.Normal);
	// var formattedText = new FormattedText("Hello", ..., typeface, ...);
	// var origin = new Point(0, 0);
	// var geom = formattedText.BuildGeometry(origin);
	// var path = geom.GetOutlinedPathGeometry(0.0001, ToleranceType.Relative);

	/// <summary>
	/// Given a font family name (like "Helvetica") and a style name
	/// (like "normal"), find a typeface that contains glyphs.
	/// </summary>
	/// <remarks>
	/// A <see cref="Typeface"/> is just a combination of family, style
	/// (Normal|Oblique|Italic), weight (Light|Regular|Bold|etc.), and
	/// stretch (Condensed|Medium|Expanded|etc); a <see cref="GlyphTypeface"/>
	/// corresponds to a real font file and contains the actual glyphs.
	/// </remarks>
	public static GlyphTypeface FindGlyphTypeface(string familyName, string styleName)
	{
		var family = new FontFamily(familyName);

		var faces = family.GetTypefaces();

		var ordinal = StringComparer.Ordinal;
		var ignoreCase = StringComparer.OrdinalIgnoreCase;

		// TODO more elaborate logic to match styleName to an actual FontStyle and FontWeight

		var face = faces.FirstOrDefault(
			           tf => tf.FaceNames.Values?.Contains(styleName, ordinal) ?? false) ??
		           faces.FirstOrDefault(
			           tf => tf.FaceNames.Values?.Contains(styleName, ignoreCase) ?? false) ??
		           faces.First();

		return face.TryGetGlyphTypeface(out GlyphTypeface glyphs) ? glyphs : null;
	}

	public static void GetFontHeights(
		GlyphTypeface typeface, double fontSize,
		out double baseline, out double height,
		out double xHeight, out double capsHeight)
	{
		baseline = typeface.Baseline * fontSize;
		height = typeface.Height * fontSize;
		xHeight = typeface.XHeight * fontSize;
		capsHeight = typeface.CapsHeight * fontSize;
	}

	/// <summary>
	/// Get the glyph (geometry) for the given Unicode code point
	/// from the given typeface at the given font size. Also return
	/// the horizontal advance width (x coordinate increment) for
	/// this glyph. Returns null if the typeface contains no glyph
	/// for the given code point.
	/// </summary>
	public static Geometry GetGlyph(
		int codePoint, GlyphTypeface typeface, double fontSize, out double advance)
	{
		advance = 0.0;

		if (! typeface.CharacterToGlyphMap.TryGetValue(codePoint, out ushort index))
		{
			return null; // font has no glyph for given code point
		}

		advance = typeface.AdvanceWidths[index] * fontSize;
		//lsb = typeface.LeftSideBearings[index] * fontSize;
		//rsb = typeface.RightSideBearings[index] * fontSize;
		// NB. expect advance = lsb + glyph.Bounds.Width + rsb

		//var tsb = typeface.TopSideBearings[index] * fontSize;
		//var bsb = typeface.BottomSideBearings[index] * fontSize;
		//var bot = typeface.DistancesFromHorizontalBaselineToBlackBoxBottom[index] * fontSize;
		//var vert = typeface.AdvanceHeights[index] * fontSize;

		return typeface.GetGlyphOutline(index, fontSize, fontSize);
	}

	/// <summary>
	/// Convert a glyph geometry to an Esri Polygon.Optionally scale
	/// the coordinates from the glyph's origin by the given factor.
	/// Flip the Y axis (because WPF has positive down). The returned
	/// polygon has no spatial reference.
	/// </summary>
	public static Polygon ToEsriPolygon(
		Geometry glyph, bool flipY = true, double scaleFactor = 1.0)
	{
		if (glyph is null) return null;

		if (flipY && glyph.IsFrozen)
		{
			glyph = glyph.Clone(); // clone is not frozen
		}

		var saved = glyph.Transform;

		try
		{
			if (flipY)
			{
				// WPF has positive Y down, ArcGIS has positive Y up
				glyph.Transform = new MatrixTransform(1, 0, 0, -1, 0, 0);
			}

			// Luckily, GetOutlinedPathGeometry() applies the Transform (empirical)
			var outline = glyph.GetOutlinedPathGeometry(0.0001, ToleranceType.Relative);

			return ToEsriPolygon(outline, scaleFactor);
		}
		finally
		{
			if (flipY)
			{
				glyph.Transform = saved;
			}
		}
	}

	/// <summary>
	/// Convert the given PathGeometry (usually a glyph's outline path) to
	/// an Esri Polygon; optionally scale from the glyph's origin (usually
	/// to go from points to map units). The result has no spatial reference.
	/// </summary>
	public static Polygon ToEsriPolygon(PathGeometry path, double scaleFactor = 1.0)
	{
		// TODO add ToEsriPolyline() which includes unfilled figures

		// PathGeometry:
		// .Figures: list of PathFigure
		// .FillRule: EvenOdd|Nonzero
		// .Bounds: Rect (bounding box)

		// PathFigure:
		// .IsClosed: true iff 1st and last segs are (to be?) connected
		// .IsFilled: true iff contained area is to be used for rendering, clipping, hit-testing
		// .StartPoint: where the PathFigure begins
		// .Segments: list of PathSegment

		// PathSegment: abstract, subclasses:
		// LineSegment: .Point (endpoint of segment)
		// PolyLineSegment: .Points (list of Point)
		// BezierSegment: (cubic) .Point1, .Point2, .Point3
		// PolyBezierSegment: .Points (list of Point)
		// ArcSegment: .Point, .Size, ...
		// QuadraticBezierSegment: .Point1, .Point2
		// PolyQuadraticBezierSegment: .Points (list of Point)

		if (path is null) return null;

		var builder = new PolygonBuilderEx();

		foreach (var figure in path.Figures)
		{
			if (! figure.IsFilled) continue;
			// ignore IsClosed -- the polygon builder will close if needed
			// with the glyphs I've seen, all figures have IsClosed and IsFilled

			var startPoint = figure.StartPoint.ToPair() * scaleFactor;
			bool startNewPart = true;

			foreach (var segment in figure.Segments)
			{
				if (segment is LineSegment line)
				{
					var endPoint = line.Point.ToPair() * scaleFactor;
					var seg = CreateLineSegment(startPoint, endPoint);
					builder.AddSegment(seg, startNewPart);
					startNewPart = false;
					startPoint = endPoint;
				}
				else if (segment is PolyLineSegment lines)
				{
					foreach (var p in lines.Points)
					{
						var endPoint = p.ToPair() * scaleFactor;
						var seg = CreateLineSegment(startPoint, endPoint);
						builder.AddSegment(seg, startNewPart);
						startNewPart = false;
						startPoint = endPoint;
					}
				}
				else if (segment is BezierSegment cubic)
				{
					var control1 = cubic.Point1.ToPair() * scaleFactor;
					var control2 = cubic.Point2.ToPair() * scaleFactor;
					var endPoint = cubic.Point3.ToPair() * scaleFactor;
					var seg = CreateBezierSegment(startPoint, control1, control2, endPoint);
					builder.AddSegment(seg, startNewPart);
					startNewPart = false;
					startPoint = endPoint;
				}
				else if (segment is PolyBezierSegment cubics)
				{
					int count = cubics.Points.Count;
					// assume count is a multiple of 3
					for (int i = 0; i + 2 < count; i += 3)
					{
						var control1 = cubics.Points[i + 0].ToPair() * scaleFactor;
						var control2 = cubics.Points[i + 1].ToPair() * scaleFactor;
						var endPoint = cubics.Points[i + 2].ToPair() * scaleFactor;
						var seg = CreateBezierSegment(startPoint, control1, control2, endPoint);
						builder.AddSegment(seg, startNewPart);
						startNewPart = false;
						startPoint = endPoint;
					}
				}
				else if (segment is QuadraticBezierSegment quadratic)
				{
					var control = quadratic.Point1.ToPair() * scaleFactor;
					var endPoint = quadratic.Point2.ToPair() * scaleFactor;
					var seg = CreateBezierSegment(startPoint, control, endPoint);
					builder.AddSegment(seg, startNewPart);
					startNewPart = false;
					startPoint = endPoint;
				}
				else if (segment is PolyQuadraticBezierSegment quadratics)
				{
					int count = quadratics.Points.Count;
					// assume count is a multiple of 2
					for (int i = 0; i + 1 < count; i += 2)
					{
						var control = quadratics.Points[i + 0].ToPair() * scaleFactor;
						var endPoint = quadratics.Points[i + 1].ToPair() * scaleFactor;
						var seg = CreateBezierSegment(startPoint, control, endPoint);
						builder.AddSegment(seg, startNewPart);
						startNewPart = false;
						startPoint = endPoint;
					}
				}
				else if (segment is ArcSegment)
				{
					throw new NotImplementedException(
						$"Path segments of type {nameof(ArcSegment)} are not yet implemented");
				}
				else
				{
					throw new NotSupportedException(
						$"Unsupported PathSegment subtype: {segment.GetType().Name}");
				}
			}
		}

		var geometry = builder.ToGeometry();
		return GeometryUtils.Simplify(geometry);
	}

	#region Private utils

	private static Pair ToPair(this Point point)
	{
		return new Pair(point.X, point.Y);
	}

	private static ArcGIS.Core.Geometry.LineSegment CreateLineSegment(Pair p0, Pair p1)
	{
		var a = p0.ToCoordinate2D();
		var b = p1.ToCoordinate2D();
		return LineBuilderEx.CreateLineSegment(a, b);
	}

	private static CubicBezierSegment CreateBezierSegment(Pair p0, Pair p1, Pair p2, Pair p3)
	{
		var a = p0.ToCoordinate2D();
		var c1 = p1.ToCoordinate2D();
		var c2 = p2.ToCoordinate2D();
		var b = p3.ToCoordinate2D();
		return CubicBezierBuilderEx.CreateCubicBezierSegment(a, c1, c2, b);
	}

	private static CubicBezierSegment CreateBezierSegment(Pair p0, Pair p1, Pair p2)
	{
		// ArcGIS only does not have quadratic Bezier segments, but
		// for any quadratic there is a cubic that has exactly the same shape:
		CubicBezier.FromQuadratic(p0, p1, p2, out var p11, out var p12);
		return CreateBezierSegment(p0, p11, p12, p2);
	}

	#endregion
}
