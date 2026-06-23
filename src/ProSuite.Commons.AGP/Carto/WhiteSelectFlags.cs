using System;

namespace ProSuite.Commons.AGP.Carto;

[Flags]
public enum WhiteSelectFlags
{
	Normal = 0,
	MustBoundarySelectUnfilledPolygons = 1
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
}
