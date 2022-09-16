using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor;

internal static class BlazorImageUtils
{
	public const string _keyDeleted = "deleted";
	private const string _keyGeometryNetwork = "network";
	private const string _keyMultiPath = "multipatch";
	private const string _keyPoint = "point";
	private const string _keyPolygon = "polygon";
	private const string _keyPolyline = "polyline";
	private const string _keyTable = "table";
	private const string _keyTerrain = "terrain";
	private const string _keyTopology = "topology";
	private const string _keyMosaicDataset = "mosaicdataset";
	private const string _keyRasterDataset = "rasterdataset";
	private const string _keyUnknown = "unknown";
	private const string _keyTransform = "transform";
	private const string _keyRowFilter = "rowfilter";
	private const string _keyIssueFilter = "issuefilter";

	public static string GetImageSource(GeometryType geometryType)
	{
		if (geometryType is GeometryTypeTerrain)
		{
			return GetImageSource(_keyTerrain);
		}

		if (geometryType is GeometryTypeGeometricNetwork)
		{
			return GetImageSource(_keyGeometryNetwork);
		}

		if (geometryType is GeometryTypeTopology)
		{
			return GetImageSource(_keyTopology);
		}

		if (geometryType is GeometryTypeNoGeometry)
		{
			return GetImageSource(_keyTable);
		}

		if (geometryType is GeometryTypeShape geometryTypeShape)
		{
			return GetImageSource(geometryTypeShape.ShapeType);
		}

		if (geometryType is GeometryTypeRasterMosaic)
		{
			return GetImageSource(_keyMosaicDataset);
		}

		if (geometryType is GeometryTypeRasterDataset)
		{
			return GetImageSource(_keyRasterDataset);
		}

		return GetImageSource(_keyUnknown);
	}

	[CanBeNull]
	public static string GetImageSource([CanBeNull] IDdxDataset dataset)
	{
		if (dataset == null)
		{
			return null;
		}

		return dataset.Deleted
			       ? GetImageSource(_keyDeleted)
			       : GetImageSource(dataset.GeometryType);
	}

	public static string GetImageSource(InstanceConfiguration configuration)
	{
		return configuration switch
		{
			TransformerConfiguration => $"{GetImage(_keyTransform)}.png",
			IssueFilterConfiguration => $"{GetImage(_keyIssueFilter)}.png",
			RowFilterConfiguration => $"{GetImage(_keyRowFilter)}.png",
			_ => throw new NotImplementedException()
		};
	}

	private static string GetImageSource(string key)
	{
		// todo daro!
		return string.Empty;
	}

	private static string GetImageSource(ProSuiteGeometryType geometryType)
	{
		string file;

		switch (geometryType)
		{
			case ProSuiteGeometryType.Point:
			case ProSuiteGeometryType.Multipoint:
				file = "DatasetTypePoint.png";
				break;

			case ProSuiteGeometryType.Polygon:
				file = "DatasetTypePolygon.png";
				break;

			case ProSuiteGeometryType.Polyline:
				file = "DatasetTypeLine.png";
				break;

			case ProSuiteGeometryType.MultiPatch:
				file = "DatasetTypeMultipatch.png";
				break;

			case ProSuiteGeometryType.Null:
				file = "DatasetTypeTable.png";
				break;

			default:
				file = "DatasetTypeUnkown.png";
				break;
		}

		return GetImage(Assert.NotNull(file));
	}

	private static string GetImage(string name)
	{
		return $"images/{name}";
	}
}
