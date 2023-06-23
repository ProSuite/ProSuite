using System.Threading;
using System.Windows;
using System.Windows.Media;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GlyphUtilsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanFindGlyphTypeface()
	{
		// Arial should exist on most if not all Windows systems
		// and in all four common styles/weights:

		var ar = GlyphUtils.FindGlyphTypeface("Arial", "Regular");
		Assert.NotNull(ar);
		Assert.AreEqual(FontStyles.Normal, ar.Style);
		Assert.AreEqual(FontWeights.Normal, ar.Weight);

		var ai = GlyphUtils.FindGlyphTypeface("Arial", "Italic");
		Assert.NotNull(ai);
		Assert.AreEqual(FontStyles.Italic, ai.Style);
		Assert.AreEqual(FontWeights.Regular, ai.Weight);

		var ab = GlyphUtils.FindGlyphTypeface("Arial", "Bold");
		Assert.NotNull(ab);
		Assert.AreEqual(FontStyles.Normal, ab.Style);
		Assert.AreEqual(FontWeights.Bold, ab.Weight);

		var ax = GlyphUtils.FindGlyphTypeface("Arial", "Bold Italic");
		Assert.NotNull(ax);
		Assert.AreEqual(FontStyles.Italic, ax.Style);
		Assert.AreEqual(FontWeights.Bold, ax.Weight);

		var xx = GlyphUtils.FindGlyphTypeface("MostLikelyNoSuchFont", "Regular");
		Assert.IsNull(xx);
	}

	[Test]
	public void CanGetGlyph()
	{
		const int codePoint = 0x41; // A in Unicode

		var face = GlyphUtils.FindGlyphTypeface("Arial", "Regular");
		Assert.NotNull(face);

		var glyph = GlyphUtils.GetGlyph(codePoint, face, 10.0, out double advance);
		Assert.NotNull(glyph);
		Assert.False(glyph.IsEmpty());
		// save to assume advance width is positive and less than 1em:
		Assert.True(advance is > 0.0 and <= 10.0);

		var noGlyph = GlyphUtils.GetGlyph(99999, face, 10.0, out _);
		Assert.IsNull(noGlyph);
	}

	[Test]
	public void CanToEsriPolygon()
	{
		// NB. Geometries returned by Parse() are frozen (read-only)

		var geom1 = Geometry.Parse("M 0 0 L 5 5 C 10 10 10 5 10 0"); // open
		var path1 = geom1.GetOutlinedPathGeometry(); // not frozen
		var poly1 = GlyphUtils.ToEsriPolygon(path1);
		Assert.NotNull(poly1);
		Assert.AreEqual(1, poly1.PartCount);
		Assert.AreEqual(4, poly1.PointCount); // NB: closed by builder
		Assert.AreEqual(geom1.Bounds.Width, poly1.Extent.Width, 0.001);
		Assert.AreEqual(geom1.Bounds.Height, poly1.Extent.Height, 0.001);

		var geom2 = Geometry.Parse("M 0 0 L 5 5 C 10 10 10 5 10 0 Z"); // closed
		var path2 = geom2.GetOutlinedPathGeometry(); // not frozen
		var poly2 = GlyphUtils.ToEsriPolygon(path2);
		Assert.NotNull(poly2);
		Assert.AreEqual(1, poly2.PartCount);
		Assert.AreEqual(4, poly2.PointCount);
		Assert.AreEqual(geom2.Bounds.Width, poly2.Extent.Width, 0.001);
		Assert.AreEqual(geom2.Bounds.Height, poly2.Extent.Height, 0.001);

		// two 5x5 squares, the second with a 3x3 hole
		var geom3 =
			Geometry.Parse("M 0 0 H 5 V 5 H 0 Z M 10 0 H 15 V 5 H 10 Z M 11 1 H 14 V 4 H 11 Z");
		var poly3 = GlyphUtils.ToEsriPolygon(geom3);
		Assert.NotNull(poly3);
		Assert.AreEqual(3, poly3.PartCount); // 3 rings (but 2 contiguous parts)
		Assert.AreEqual(25.0 + 25.0 - 9.0, poly3.Area, 0.001);

		var geom4 = Geometry.Parse("M 0 0 Q 5 5 10 0"); // one quadratic bezier segment
		var poly4 = GlyphUtils.ToEsriPolygon(geom4, false);
		Assert.NotNull(poly4);
		Assert.True(poly4.HasCurves);
		Assert.AreEqual(1, poly4.PartCount);
		Assert.AreEqual(3, poly4.PointCount);
		// Bezier is within hull of control points
		Assert.Less(poly4.Extent.YMax, 5.0);

		Assert.IsNull(GlyphUtils.ToEsriPolygon(null));
	}
}
