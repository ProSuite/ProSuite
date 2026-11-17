using System;

namespace ProSuite.Commons.AGP.Carto;

[Flags]
public enum WhiteSelectFlags
{
	Normal = 0,
	MustBoundarySelectUnfilledPolygons = 1, // managed by situation analysis
	MustBoundarySelectFilledPolygons = 2, // user preference (settings/defaults)
	FilledPolygonClickSelectsAllVertices = 4 // user preference (settings/defaults)
}

public static class WhiteSelectFlagsExtensions
{
	private static bool HasFlag(this WhiteSelectFlags flags, WhiteSelectFlags flag)
	{
		return (flags & flag) == flag;
	}

	public static bool MustBoundarySelectUnfilledPolygons(this WhiteSelectFlags flags)
	{
		return HasFlag(flags, WhiteSelectFlags.MustBoundarySelectUnfilledPolygons);
	}

	public static bool MustBoundarySelectFilledPolygons(this WhiteSelectFlags flags)
	{
		return HasFlag(flags, WhiteSelectFlags.MustBoundarySelectFilledPolygons);
	}

	public static bool FilledPolygonClickSelectsAllVertices(this WhiteSelectFlags flags)
	{
		return HasFlag(flags, WhiteSelectFlags.FilledPolygonClickSelectsAllVertices);
	}
}
