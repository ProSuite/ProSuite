using System;
using System.IO;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Carto;

public static class Extensions
{
	public static Pair ToPair(this MapPoint point)
	{
		if (point is null) throw new ArgumentNullException(nameof(point));
		return new Pair(point.X, point.Y);
	}

	public static Pair ToPair(this Coordinate2D coord)
	{
		return new Pair(coord.X, coord.Y);
	}

	public static Coordinate2D ToCoordinate2D(this Pair pair)
	{
		return new Coordinate2D(pair.X, pair.Y);
	}

	/// <summary>
	/// Serialize given CIM object to XML and write to
	/// a file (overwrite or append) (file is created).
	/// </summary>
	public static void DumpToFile(this CIMObject cim, string filePath, bool append = false)
	{
		// Use the Pro SDK XmlUtil; using XmlWriter directly gives weird error

		var text = XmlUtil.ToXml(cim);

		if (append)
		{
			File.AppendAllText(filePath, text);
		}
		else
		{
			File.WriteAllText(filePath, text);
		}
	}
}
