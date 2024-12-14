using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ZValuesTest]
	public class QaSurfaceVertex : QaSurfaceOffset
	{
		private readonly IPoint _pointTemplate = new PointClass();
		private QueryFilterHelper _helper;
		private IFeatureClassFilter _queryFilter;
		private readonly esriGeometryType _shapeType;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoTerrainData = "NoTerrainData";

			public const string ZOffset_NotEnoughAboveTerrain =
				"ZOffset.NotEnoughAboveTerrain";

			public const string ZOffset_NotEnoughBelowTerrain =
				"ZOffset.NotEnoughBelowTerrain";

			public const string ZOffset_TooCloseToTerrain =
				"ZOffset.TooCloseToTerrain";

			public const string ZOffset_TooFarFromTerrain =
				"ZOffset.TooFarFromTerrain";

			public Code() : base("VertexToTerrainZOffset") { }
		}

		#endregion

		[Doc(nameof(DocStrings.Qa3dSmoothing_0))]
		public QaSurfaceVertex(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.Qa3dSmoothing_terrain))] [NotNull]
			TerrainReference terrain,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mustBeLarger))]
			bool mustBeLarger)
			:
			this(featureClass, terrain, limit,
			     mustBeLarger
				     ? ZOffsetConstraint.AboveLimit
				     : ZOffsetConstraint.WithinLimit) { }

		[Doc(nameof(DocStrings.Qa3dSmoothing_0))]
		public QaSurfaceVertex(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.Qa3dSmoothing_terrain))] [NotNull]
			TerrainReference terrain,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint)
			: base(featureClass, terrain, 0, limit, zOffsetConstraint)
		{
			_shapeType = featureClass.ShapeType;
		}

		[Doc(nameof(DocStrings.QaSurfaceVertex_2))]
		public QaSurfaceVertex(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_raster))] [NotNull]
			RasterDatasetReference raster,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mustBeLarger))]
			bool mustBeLarger)
			: this(featureClass, raster, limit,
			       mustBeLarger
				       ? ZOffsetConstraint.AboveLimit
				       : ZOffsetConstraint.WithinLimit) { }

		[Doc(nameof(DocStrings.QaSurfaceVertex_2))]
		public QaSurfaceVertex(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_raster))] [NotNull]
			RasterDatasetReference raster,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint)
			: base(featureClass, raster, limit, zOffsetConstraint)
		{
			_shapeType = featureClass.ShapeType;
		}

		[Doc(nameof(DocStrings.QaSurfaceVertex_4))]
		public QaSurfaceVertex(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mosaic))] [NotNull]
			MosaicRasterReference rasterMosaic,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mustBeLarger))]
			bool mustBeLarger)
			: this(featureClass, rasterMosaic, limit,
			       mustBeLarger
				       ? ZOffsetConstraint.AboveLimit
				       : ZOffsetConstraint.WithinLimit) { }

		[Doc(nameof(DocStrings.QaSurfaceVertex_4))]
		public QaSurfaceVertex(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mosaic))] [NotNull]
			MosaicRasterReference rasterMosaic,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint)
			: base(featureClass, rasterMosaic, limit, zOffsetConstraint)
		{
			_shapeType = featureClass.ShapeType;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return NoError;
		}

		protected override int ExecuteCore(ISurfaceRow surfaceRow, int surfaceIndex)
		{
			if (_queryFilter == null)
			{
				InitFilter();
				Assert.NotNull(_queryFilter);
			}

			var errorCount = 0;
			ISimpleSurface surface = null;

			_queryFilter.FilterGeometry = surfaceRow.Extent;

			double xMin;
			double yMin;
			double xMax;
			double yMax;

			surfaceRow.Extent.QueryCoords(out xMin, out yMin,
			                              out xMax, out yMax);

			foreach (IReadOnlyRow searchRow in Search(InvolvedTables[0], _queryFilter, _helper))
			{
				var feature = (IReadOnlyFeature) searchRow;

				if (surface == null)
				{
					surface = surfaceRow.Surface;
				}

				IGeometry shape = feature.Shape;
				if (shape == null || shape.IsEmpty)
				{
					continue;
				}

				switch (_shapeType)
				{
					case esriGeometryType.esriGeometryPoint:
						var p = (IPoint) shape;
						double x, y;
						p.QueryCoords(out x, out y);

						double max = double.NaN;
						ErrorType error = CheckPoint(x, y, p.Z, surface, ref max);

						switch (error)
						{
							case ErrorType.None:
								break;

							case ErrorType.NoTerrain:
								errorCount += ReportError(
									MissingTerrainDescription,
									InvolvedRowUtils.GetInvolvedRows(feature), p,
									Codes[Code.NoTerrainData],
									TestUtils.GetShapeFieldName(feature));
								break;

							default:
								IssueCode issueCode;
								errorCount += ReportError(
									GetOffsetMessage(max, out issueCode),
									InvolvedRowUtils.GetInvolvedRows(feature), p,
									issueCode, TestUtils.GetShapeFieldName(feature));
								break;
						}

						break;

					case esriGeometryType.esriGeometryPolygon:
					case esriGeometryType.esriGeometryPolyline:
					case esriGeometryType.esriGeometryMultipoint:
					case esriGeometryType.esriGeometryMultiPatch:
						var vertices = (IPointCollection) shape;

						if (xMin > xMax)
						{
							((IEnvelope) _queryFilter.FilterGeometry).QueryCoords(
								out xMin, out yMin,
								out xMax, out yMax);
						}

						errorCount += CheckPoints(xMin, yMin, xMax, yMax,
						                          surface, vertices, feature);
						break;

					default:
						throw new InvalidOperationException(
							$"Unhandled geometry type: {_shapeType}");
				}
			}

			return errorCount;
		}

		private void InitFilter()
		{
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> helpers;
			CopyFilters(out filters, out helpers);

			_queryFilter = filters[0];
			_helper = helpers[0];

			_queryFilter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}

		private int CheckPoints(double xMin, double yMin,
		                        double xMax, double yMax,
		                        [NotNull] ISimpleSurface surface,
		                        [NotNull] IPointCollection vertices,
		                        [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;
			IEnumVertex enumVertices = vertices.EnumVertices;
			double max = double.NaN;

			int part, vertex;
			enumVertices.QueryNext(_pointTemplate, out part, out vertex);

			var offsetPoints = new List<PartVertex>();
			var missingPoints = new List<PartVertex>();

			int part0 = -1;
			while (part >= 0 && vertex >= 0)
			{
				if (part0 != part)
				{
					errorCount += ReportMissingTerrainError(missingPoints, vertices, row, false);
					errorCount += ReportOffsetError(offsetPoints, vertices, ref max, row, false);
					part0 = part;
				}

				double x, y;
				_pointTemplate.QueryCoords(out x, out y);

				if (xMin <= x && xMax >= x &&
				    yMin <= y && yMax >= y)
				{
					ErrorType error = CheckPoint(x, y, _pointTemplate.Z, surface, ref max);

					switch (error)
					{
						case ErrorType.None:
							errorCount += ReportMissingTerrainError(missingPoints, vertices, row,
								false);
							errorCount += ReportOffsetError(offsetPoints, vertices, ref max,
							                                row, false);
							break;

						case ErrorType.NoTerrain:
							missingPoints.Add(new PartVertex(part, vertex));
							errorCount += ReportOffsetError(offsetPoints, vertices, ref max,
							                                row, false);
							break;

						default:
							offsetPoints.Add(new PartVertex(part, vertex));
							errorCount += ReportMissingTerrainError(missingPoints, vertices, row,
								false);
							break;
					}
				}

				enumVertices.QueryNext(_pointTemplate, out part, out vertex);
			}

			const bool final = true;
			errorCount += ReportMissingTerrainError(missingPoints, vertices, row, final);
			errorCount += ReportOffsetError(offsetPoints, vertices, ref max, row, final);

			return errorCount;
		}

		private ErrorType CheckPoint(double x, double y, double z,
		                             [NotNull] ISimpleSurface surface,
		                             ref double max)
		{
			Assert.ArgumentNotNull(surface, nameof(surface));

			double dist = z - surface.GetZ(x, y);

			return GetErrorType(dist, ref max);
		}

		private int ReportOffsetError([NotNull] IList<PartVertex> errorPoints,
		                              IPointCollection baseGeometry,
		                              ref double offset,
		                              IReadOnlyRow row,
		                              bool final)
		{
			var errorCount = 0;

			if (errorPoints.Count > 0)
			{
				IssueCode issueCode;
				string description = GetOffsetMessage(offset, out issueCode);

				errorCount += ReportError(errorPoints, baseGeometry, description, issueCode, row,
				                          final);
				offset = double.NaN;
			}

			return errorCount;
		}

		private int ReportMissingTerrainError([NotNull] IList<PartVertex> errorPoints,
		                                      IPointCollection geometry,
		                                      IReadOnlyRow row,
		                                      bool final)
		{
			var errorCount = 0;

			if (errorPoints.Count > 0)
			{
				IssueCode issueCode = Codes[Code.NoTerrainData];
				errorCount += ReportError(errorPoints, geometry, MissingTerrainDescription,
				                          issueCode, row, final);
			}

			return errorCount;
		}

		[NotNull]
		private static SegmentPartList GetSegments(
			[NotNull] IPolycurve line,
			[NotNull] IEnumerable<PartVertex> points)
		{
			var result = new SegmentPartList(line);

			foreach (PartVertex point in points)
			{
				result.Add(point.Part, point.Vertex - 1, 0.5, 1);
				result.Add(point.Part, point.Vertex, 0, 0.5);
			}

			return result;
		}

		private int ReportError([NotNull] IList<PartVertex> errorPoints,
		                        [NotNull] IPointCollection baseGeometry,
		                        string description,
		                        IssueCode issueCode,
		                        [NotNull] IReadOnlyRow errorRow,
		                        bool final)
		{
			var errorCount = 0;

			if (baseGeometry is IPolycurve polycurve)
			{
				SegmentPartList error = GetSegments((IPolycurve) baseGeometry, errorPoints);
				IEnumerable<IPolyline> parts = error.GetParts(polycurve.SpatialReference);

				foreach (IPolyline part in parts)
				{
					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(errorRow), part, issueCode,
						TestUtils.GetShapeFieldName(errorRow));
				}

				error.Clear();
				errorPoints.Clear();
			}
			else if (final)
			{
				IMultipoint errorGeom = CreateMultipoint(baseGeometry, errorPoints);
				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(errorRow), errorGeom, issueCode,
					TestUtils.GetShapeFieldName(errorRow));
			}

			return errorCount;
		}

		[NotNull]
		private static IMultipoint CreateMultipoint([NotNull] IPointCollection baseGeometry,
		                                            [NotNull] IEnumerable<PartVertex> points)
		{
			IPointCollection multi = (IPointCollection) GeometryFactory.CreateEmptyMultipoint(
				((IGeometry) baseGeometry));

			object missing = Type.Missing;
			IEnumVertex vList = baseGeometry.EnumVertices;

			foreach (PartVertex point in points)
			{
				vList.SetAt(point.Part, point.Vertex);
				IPoint p;
				vList.Next(out p, out int _, out int _);

				multi.AddPoint(p, ref missing, ref missing);
			}

			return (IMultipoint) multi;
		}

		[NotNull]
		protected string GetOffsetMessage(double offset, out IssueCode issueCode)
		{
			switch (ZOffsetConstraint)
			{
				case ZOffsetConstraint.AboveLimit:
					issueCode = Codes[Code.ZOffset_NotEnoughAboveTerrain];
					return string.Format("Not enough above terrain: {0}",
					                     FormatComparison(offset, "<", Limit, "N1"));

				case ZOffsetConstraint.BelowLimit:
					issueCode = Codes[Code.ZOffset_NotEnoughBelowTerrain];
					return string.Format("Not enough below terrain: {0}",
					                     FormatComparison(offset, ">", Limit, "N1"));

				case ZOffsetConstraint.WithinLimit:
					issueCode = Codes[Code.ZOffset_TooFarFromTerrain];
					return string.Format("Too far from terrain: {0}",
					                     FormatComparison(Math.Abs(offset), ">", Limit,
					                                      "N1"));

				case ZOffsetConstraint.OutsideLimit:
					issueCode = Codes[Code.ZOffset_TooCloseToTerrain];
					return string.Format("Too close to terrain: {0}",
					                     FormatComparison(Math.Abs(offset), "<", Limit,
					                                      "N1"));

				default:
					throw new InvalidOperationException("Unhandled ZOffsetConstraint " +
					                                    ZOffsetConstraint);
			}
		}

		#region Nested type: PartVertex

		private class PartVertex
		{
			public readonly int Part;
			public readonly int Vertex;

			public PartVertex(int part, int vertex)
			{
				Part = part;
				Vertex = vertex;
			}
		}

		#endregion
	}
}
