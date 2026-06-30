using System;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using UnitType = ArcGIS.Core.Geometry.UnitType;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// The distance unit label and numeric precision to use for map-unit tool settings shown for
/// example in a NumericSpinner. Values are map units, so no conversion is involved; only the
/// label and the number of decimals (so that small values down to ~0.1 mm on the ground remain
/// representable).
/// </summary>
public class DisplayUnitInfo
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>The smallest increment that must remain representable, in meters (0.1 mm).</summary>
	private const double _targetGroundResolutionMeters = 0.0001;

	/// <summary>Nominal meters per degree, used only to derive the decimal count for a GCS.</summary>
	private const double _nominalMetersPerDegree = 111_320;

	private DisplayUnitInfo(string label, int decimals, double step)
	{
		Label = label;
		Decimals = decimals;
		Step = step;
	}

	/// <summary>Unit label to show next to the spinner (e.g. "m", "ft", "dd").</summary>
	public string Label { get; }

	public int Decimals { get; }

	public double Step { get; }

	/// <summary>
	/// Builds the display unit info from a map's spatial reference. Accesses only the
	/// (immutable) spatial reference and its unit, which is safe to call off the MCT.
	/// </summary>
	[NotNull]
	public static DisplayUnitInfo FromMap([CanBeNull] Map map)
	{
		try
		{
			SpatialReference sref = map?.SpatialReference;
			Unit mapUnit = sref?.Unit;

			if (sref == null || mapUnit == null)
			{
				return Default();
			}

			if (sref.IsGeographic || mapUnit.UnitType == UnitType.Angular)
			{
				// Per product decision: in a GCS the tolerance is entered and stored in
				// degrees (the CRS angular unit).
				double radiansPerDegree = mapUnit.ConversionFactor; // angular: radians per unit
				double metersPerDegree = radiansPerDegree > 0
					                         ? radiansPerDegree * 6_378_137 // equatorial radius
					                         : _nominalMetersPerDegree;

				return Create("dd", metersPerDegree);
			}

			// Projected / linear CRS: the distance unit is the spatial reference's unit.
			return Create(GetLinearUnitLabel(mapUnit), mapUnit.ConversionFactor);
		}
		catch (Exception e)
		{
			_msg.Debug("Could not determine map display units; using defaults", e);
			return Default();
		}
	}

	private static DisplayUnitInfo Create(string label, double metersPerUnit)
	{
		int decimals = ComputeDecimals(metersPerUnit);

		double step = Math.Pow(10, -(decimals - 2));

		return new DisplayUnitInfo(label ?? string.Empty, decimals, step);
	}

	private static int ComputeDecimals(double metersPerUnit)
	{
		if (! (metersPerUnit > 0))
		{
			return 4;
		}

		return Math.Max(
			2, (int) Math.Ceiling(Math.Log10(metersPerUnit / _targetGroundResolutionMeters)));
	}

	/// <summary>
	/// Maps a linear unit to a short distance abbreviation (e.g. "m", "ft", "km"),
	/// falling back to the unit's name when unknown.
	/// </summary>
	private static string GetLinearUnitLabel([CanBeNull] Unit unit)
	{
		string name = unit?.Name;

		if (string.IsNullOrEmpty(name))
		{
			return "m";
		}

		string lower = name.ToLowerInvariant();

		// Order matters: check the more specific prefixes before "met"/"foot".
		if (lower.Contains("kilomet")) return "km";
		if (lower.Contains("centimet")) return "cm";
		if (lower.Contains("millimet")) return "mm";
		if (lower.Contains("met")) return "m"; // Meter / Metre
		if (lower.Contains("nautical")) return "NM";
		if (lower.Contains("mile")) return "mi";
		if (lower.Contains("yard")) return "yd";
		if (lower.Contains("foot") || lower.Contains("feet")) return "ft";
		if (lower.Contains("inch")) return "in";

		return name;
	}

	private static DisplayUnitInfo Default()
	{
		// Meters-like default: 4 decimals (0.1 mm), step 0.01.
		return new DisplayUnitInfo("m", decimals: 4, step: 0.01);
	}
}
