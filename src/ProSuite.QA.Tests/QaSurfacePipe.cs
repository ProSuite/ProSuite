using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Com;
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
	/// <summary>
	/// checks whether interpolated height values are close enough to height model
	/// </summary>
	[UsedImplicitly]
	[ZValuesTest]
	public class QaSurfacePipe : QaSurfaceOffset
	{
		private readonly double _startEndIgnoreLength;
		private readonly bool _asRatio;

		private readonly IPoint _pointTemplate = new PointClass();
		private readonly double _interpolateTolerance;
		private IFeatureClassFilter _queryFilter;
		private QueryFilterHelper _helper;
		private readonly esriGeometryType _shapeType;
		private SortedDictionary<IReadOnlyRow, ShortPartInfo> _shortParts;
		private readonly IEnvelope _removeBox = new EnvelopeClass();

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

			public Code() : base("GeometryToTerrainZOffset") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.Qa3dPipe_0))]
		public QaSurfacePipe(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_terrain))] [NotNull]
			TerrainReference terrain,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit)
			: this(featureClass, terrain, limit,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ZOffsetConstraint.WithinLimit, 0, false) { }

		[Doc(nameof(DocStrings.QaSurfacePipe_1))]
		public QaSurfacePipe(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_terrain))] [NotNull]
			TerrainReference terrain,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfacePipe_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint,
			[Doc(nameof(DocStrings.QaSurfacePipe_startEndIgnoreLength))]
			double startEndIgnoreLength,
			[Doc(nameof(DocStrings.QaSurfacePipe_asRatio))]
			bool asRatio)
			: base(featureClass, terrain, 0, limit, zOffsetConstraint)
		{
			ValidateAsRatio(startEndIgnoreLength, asRatio);

			_shapeType = featureClass.ShapeType;
			_startEndIgnoreLength = startEndIgnoreLength;
			_asRatio = asRatio;

			_interpolateTolerance =
				2 * SpatialReferenceUtils.GetXyResolution(featureClass.SpatialReference);
		}

		[Doc(nameof(DocStrings.QaSurfacePipe_2))]
		public QaSurfacePipe(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_raster))] [NotNull]
			RasterDatasetReference raster,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit)
			: this(featureClass, raster, limit,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ZOffsetConstraint.WithinLimit, 0, false) { }

		[Doc(nameof(DocStrings.QaSurfacePipe_2))]
		public QaSurfacePipe(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_raster))] [NotNull]
			RasterDatasetReference raster,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfacePipe_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint,
			[Doc(nameof(DocStrings.QaSurfacePipe_startEndIgnoreLength))]
			double startEndIgnoreLength,
			[Doc(nameof(DocStrings.QaSurfacePipe_asRatio))]
			bool asRatio)
			: base(featureClass, raster, limit, zOffsetConstraint)
		{
			ValidateAsRatio(startEndIgnoreLength, asRatio);

			_shapeType = featureClass.ShapeType;
			_startEndIgnoreLength = startEndIgnoreLength;
			_asRatio = asRatio;

			_interpolateTolerance =
				2 * SpatialReferenceUtils.GetXyResolution(featureClass.SpatialReference);
		}

		[Doc(nameof(DocStrings.QaSurfacePipe_4))]
		public QaSurfacePipe(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_mosaic))] [NotNull]
			MosaicRasterReference rasterMosaic,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit)
			: this(featureClass, rasterMosaic, limit,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ZOffsetConstraint.WithinLimit, 0, false) { }

		[Doc(nameof(DocStrings.QaSurfacePipe_4))]
		public QaSurfacePipe(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_mosaic))] [NotNull]
			MosaicRasterReference rasterMosaic,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfacePipe_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint,
			[Doc(nameof(DocStrings.QaSurfacePipe_startEndIgnoreLength))]
			double startEndIgnoreLength,
			[Doc(nameof(DocStrings.QaSurfacePipe_asRatio))]
			bool asRatio)
			: base(
				featureClass, rasterMosaic, limit,
				zOffsetConstraint)
		{
			ValidateAsRatio(startEndIgnoreLength, asRatio);

			_shapeType = featureClass.ShapeType;
			_startEndIgnoreLength = startEndIgnoreLength;
			_asRatio = asRatio;

			_interpolateTolerance =
				2 * SpatialReferenceUtils.GetXyResolution(featureClass.SpatialReference);
		}

		/// <summary>
		/// Constructor using Definition. Must always be the last constructor!
		/// </summary>
		/// <param name="pipeDef"></param>
		[InternallyUsedTest]
		public QaSurfacePipe([NotNull] QaSurfacePipeDefinition pipeDef)
			: base((IReadOnlyFeatureClass) pipeDef.FeatureClass,
			       pipeDef.Limit,
			       pipeDef.ZOffsetConstraint)
		{
			if (pipeDef.InvolvedRasters?.Count > 0)
			{
				InvolvedRasters = pipeDef.InvolvedRasters.Cast<RasterReference>().ToList();
			}
			else
			{
				Assert.ArgumentCondition(pipeDef.InvolvedTerrains.Count > 0,
				                         "Surface is not defined (neither raster nor terrain is provided)");

				InvolvedTerrains = pipeDef.InvolvedTerrains.Cast<TerrainReference>().ToList();
				TerrainTolerance = pipeDef.TerrainTolerance;
			}
		}

		private static void ValidateAsRatio(double startEndIgnoreLength, bool asRatio)
		{
			if (! asRatio || ! (startEndIgnoreLength >= 0.5))
			{
				return;
			}

			throw new ArgumentOutOfRangeException(
				nameof(startEndIgnoreLength), startEndIgnoreLength,
				$@"StartEndIgnoreLength {startEndIgnoreLength} >= 0.5 not allowed for AsRatio = 'true'");
		}

		[NotNull]
		private SortedDictionary<IReadOnlyRow, ShortPartInfo> ShortParts =>
			_shortParts ??
			(_shortParts =
				 new SortedDictionary<IReadOnlyRow, ShortPartInfo>(new RowComparer(this)));

		#endregion

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return NoError;
		}

		protected override int ExecuteCore(ISurfaceRow surfaceRow, int surfaceIndex)
		{
			if (_queryFilter == null)
			{
				InitFilter();
				_queryFilter = Assert.NotNull(_queryFilter);
			}

			ISimpleSurface surface = null;

			IEnvelope box = surfaceRow.Extent;

			_queryFilter.FilterGeometry = box;

			WKSEnvelope wksBox;
			box.QueryWKSCoords(out wksBox);

			var errorCount = 0;

			foreach (IReadOnlyRow searchedRow in Search(InvolvedTables[0], _queryFilter, _helper))
			{
				if (surface == null)
				{
					// build the surface only if there is at least one feature in the search box
					surface = surfaceRow.Surface;
				}

				errorCount += CheckFeature(surface, (IReadOnlyFeature) searchedRow, wksBox);
			}

			return errorCount;
		}

		private int CheckFeature([NotNull] ISimpleSurface surface,
		                         [NotNull] IReadOnlyFeature searchedRow,
		                         WKSEnvelope wksBox)
		{
			IGeometry shape = searchedRow.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			switch (_shapeType)
			{
				// NOTE: Multipoints currently not supported

				case esriGeometryType.esriGeometryPoint:
					IGeometry interpolatedPoint = InterpolateShape(surface, shape);

					return CheckPoint((IPoint) shape, (IPoint) interpolatedPoint, searchedRow);

				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryPolyline:
					return CheckPolycurve(surface, (IPolycurve) shape, searchedRow, wksBox);

				case esriGeometryType.esriGeometryMultiPatch:
					return CheckMultiPatch(surface, (IMultiPatch) shape, searchedRow, wksBox);

				default:
					throw new InvalidOperationException($"Unhandled shape type: {_shapeType} ");
			}
		}

		private int CheckPolycurve([NotNull] ISimpleSurface surface,
		                           [NotNull] IPolycurve shape,
		                           [NotNull] IReadOnlyFeature searchedRow,
		                           WKSEnvelope wksBox)
		{
			ICurve truncated;
			if (_startEndIgnoreLength > 0)
			{
				double endIgnoreLength = _asRatio
					                         ? 1 - _startEndIgnoreLength
					                         : shape.Length - _startEndIgnoreLength;

				if (_startEndIgnoreLength >= endIgnoreLength)
				{
					return CheckShortPart(surface, searchedRow, shape, wksBox);
				}

				shape.GetSubcurve(_startEndIgnoreLength, endIgnoreLength, _asRatio,
				                  out truncated);
			}
			else
			{
				truncated = shape;
			}

			return CheckTruncatedPolycurve(surface, shape, truncated, searchedRow, wksBox);
		}

		private int CheckTruncatedPolycurve([NotNull] ISimpleSurface surface,
		                                    [NotNull] IPolycurve fullCurve,
		                                    [NotNull] ICurve truncated,
		                                    [NotNull] IReadOnlyFeature searchedRow,
		                                    WKSEnvelope wksBox)
		{
			IGeometry interpolatedShape = InterpolateShape(surface, truncated);

			if (interpolatedShape == null)
			{
				// interpolateShape returned null, geometry is partly outside terrain
				return CheckSurfaceParts(surface, wksBox,
				                         fullCurve,
				                         searchedRow);
			}

			return CheckPolyCurve(
				wksBox,
				new PolyCurveSearcher(fullCurve, _interpolateTolerance),
				(IPolycurve) interpolatedShape,
				searchedRow);
		}

		private int CheckMultiPatch([NotNull] ISimpleSurface surface,
		                            [NotNull] IMultiPatch multiPatch,
		                            [NotNull] IReadOnlyFeature searchedRow,
		                            WKSEnvelope wksBox)
		{
			var indexedMultiPatchFeature = searchedRow as IIndexedMultiPatchFeature;

			IIndexedMultiPatch indexedMultiPatch =
				indexedMultiPatchFeature?.IndexedMultiPatch ??
				ProxyUtils.CreateIndexedMultiPatch(multiPatch);

			var patches = (IGeometryCollection) multiPatch;
			int patchCount = patches.GeometryCount;

			var errorCount = 0;

			for (var patchIndex = 0; patchIndex < patchCount; patchIndex++)
			{
				foreach (IPolygon face in MultiPatchUtils.GetFaces(
					         indexedMultiPatch, patchIndex))
				{
					errorCount += CheckTruncatedPolycurve(surface, face, face,
					                                      searchedRow, wksBox);
				}
			}

			return errorCount;
		}

		[CanBeNull]
		private static IGeometry InterpolateShape([NotNull] ISimpleSurface surface,
		                                          [NotNull] IGeometry shape)
		{
			ISpatialReference originalSpatialReference = shape.SpatialReference;

			// for raster surfaces : interpolateShape(shape, ..) -> shape gets empty when disjoint from Domain
			IPolygon domain = surface.GetDomain();

			if (domain == null)
			{
				return null;
			}

			if (((IRelationalOperator) domain).Disjoint(shape))
			{
				return null;
			}

			IGeometry shapeCopy = GeometryFactory.Clone(shape);

			IGeometry result;
			try
			{
				result = surface.Drape(shapeCopy);
			}
			finally
			{
				ComUtils.ReleaseComObject(shapeCopy);
			}

			if (result != null && result.SpatialReference == null)
			{
				result.SpatialReference = originalSpatialReference;
			}

			return result;
		}

		private int CheckShortPart([NotNull] ISimpleSurface surface,
		                           [NotNull] IReadOnlyFeature searchedRow,
		                           [NotNull] IGeometry shape,
		                           WKSEnvelope wksBox)
		{
			double maxOffset;
			int partErrorCount;
			bool valid = CheckShortPart((IPolycurve) shape, surface, wksBox, searchedRow,
			                            out maxOffset, out partErrorCount);

			ShortPartInfo partInfo;
			if (ShortParts.TryGetValue(searchedRow, out partInfo))
			{
				if (valid)
				{
					partInfo.Valid = true;
				}
				else if (Math.Abs(partInfo.MaxOffset) < Math.Abs(maxOffset))
				{
					partInfo.MaxOffset = maxOffset;
				}
			}
			else
			{
				ShortParts.Add(searchedRow, new ShortPartInfo(shape, valid, maxOffset));
			}

			return partErrorCount;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			var errorCount = 0;

			if (_shortParts == null)
			{
				return errorCount;
			}

			IEnvelope box = args.CurrentEnvelope;
			if (box == null)
			{
				return errorCount;
			}

			var remove = new List<IReadOnlyRow>();
			foreach (KeyValuePair<IReadOnlyRow, ShortPartInfo> pair in ShortParts)
			{
				IReadOnlyRow row = pair.Key;
				ShortPartInfo partInfo = pair.Value;

				partInfo.Shape.QueryEnvelope(_removeBox);

				if (_removeBox.XMax > box.XMax || _removeBox.YMax > box.YMax)
				{
					continue;
				}

				if (! partInfo.Valid)
				{
					var desc = new StringBuilder("No vertex of short feature fulfills constraint");

					desc.AppendLine();
					IssueCode issueCode;
					desc.Append(GetOffsetMessage(partInfo.MaxOffset, out issueCode));
					errorCount += ReportError(
						desc.ToString(), InvolvedRowUtils.GetInvolvedRows(row), partInfo.Shape,
						issueCode, null, values: new object[] { partInfo.MaxOffset });
				}

				remove.Add(row);
			}

			foreach (IReadOnlyRow row in remove)
			{
				ShortParts.Remove(row);
			}

			return errorCount;
		}

		private bool CheckShortPart([NotNull] IPolycurve shape1,
		                            [NotNull] ISimpleSurface surface,
		                            WKSEnvelope wksBox,
		                            [NotNull] IReadOnlyFeature searchedRow,
		                            out double maxOffset,
		                            out int errorCount)
		{
			IGeometry shape2 = InterpolateShape(surface, shape1);

			maxOffset = 0;
			bool valid;
			if (shape2 != null)
			{
				errorCount = 0;
				valid = CheckShortPart(
					wksBox,
					new PolyCurveSearcher(shape1, _interpolateTolerance),
					(IPolycurve) shape2, ref maxOffset);
			}
			else
			{
				// interpolateShape returned null, geometry is partly outside terrain
				valid = false;
				Dictionary<IPolyline, IPolycurve> surfaceParts =
					GetSurfaceParts(surface, wksBox, shape1, searchedRow,
					                out errorCount);
				foreach (KeyValuePair<IPolyline, IPolycurve> pair in surfaceParts)
				{
					valid = CheckShortPart(
						wksBox,
						new PolyCurveSearcher(pair.Key, _interpolateTolerance),
						pair.Value, ref maxOffset);

					if (valid)
					{
						return true;
					}
				}
			}

			return valid;
		}

		private void InitFilter()
		{
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> helpers;
			CopyFilters(out filters, out helpers);

			_queryFilter = filters[0];
			_helper = helpers[0];

			_queryFilter.SpatialRelationship =
				esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}

		private int CheckSurfaceParts([NotNull] ISimpleSurface surface,
		                              WKSEnvelope box,
		                              [NotNull] IPolycurve polyCurve,
		                              [NotNull] IReadOnlyFeature searchedRow)
		{
			int errorCount;
			Dictionary<IPolyline, IPolycurve> surfaceParts =
				GetSurfaceParts(surface, box, polyCurve, searchedRow, out errorCount);

			foreach (KeyValuePair<IPolyline, IPolycurve> pair in surfaceParts)
			{
				IPolyline polyline = pair.Key;
				IGeometry interpol = pair.Value;

				errorCount += CheckPolyCurve(
					box,
					new PolyCurveSearcher(polyline, _interpolateTolerance),
					(IPolycurve) interpol, searchedRow);
			}

			return errorCount;
		}

		[NotNull]
		private Dictionary<IPolyline, IPolycurve> GetSurfaceParts(
			[NotNull] ISimpleSurface surface,
			WKSEnvelope box,
			[NotNull] IPolycurve polyCurve,
			[NotNull] IReadOnlyFeature searchedRow,
			out int errorCount)
		{
			errorCount = 0;

			var missingTerrain = new SegmentPartList(polyCurve);
			var validTerrain = new SegmentPartList(polyCurve);

			IPointCollection polyCurvePoints = (IPointCollection) polyCurve;
			PopulateSegmentLists(polyCurvePoints, box, surface, missingTerrain, validTerrain);

			ISpatialReference resultSpatialReference = polyCurve.SpatialReference;

			IList<IPolyline> partsOutsideTerrain = missingTerrain.GetParts(resultSpatialReference);

			foreach (IPolyline polyline in partsOutsideTerrain)
			{
				const string description = "Terrain is missing";
				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(searchedRow), polyline,
					Codes[Code.NoTerrainData], null);
			}

			IList<IPolyline> partsInsideTerrain = validTerrain.GetParts(resultSpatialReference);

			var validDict = new Dictionary<IPolyline, IPolycurve>();
			foreach (IPolyline polyline in partsInsideTerrain)
			{
				object missing = Type.Missing;
				IGeometry interpol = InterpolateShape(surface, polyline);

				if (interpol != null)
				{
					validDict.Add(polyline, (IPolycurve) interpol);
				}
				else
				{
					var invalidSegments = new SegmentPartList(polyline);
					var validSegments = new SegmentPartList(polyline);

					var segList = (ISegmentCollection) polyline;
					IEnumSegment enumSegments = segList.EnumSegments;
					bool recycling = enumSegments.IsRecycling;
					ISegment segment;
					var partIndex = 0;
					var segmentIndex = 0;

					enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
					while (segment != null)
					{
						IPolyline segLine = ProxyUtils.CreatePolyline(segment);
						((ISegmentCollection) segLine).AddSegment(recycling
									? GeometryFactory.Clone(
										segment)
									: segment,
							ref missing, ref missing);

						interpol = InterpolateShape(surface, segLine);

						if (interpol == null)
						{
							invalidSegments.Add(partIndex, segmentIndex);
						}
						else
						{
							validSegments.Add(partIndex, segmentIndex);
						}

						if (recycling)
						{
							// release the segment, otherwise "pure virtual function call" occurs 
							// when there are certain circular arcs (IsLine == true ?)
							Marshal.ReleaseComObject(segment);
						}

						enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
					}

					foreach (IPolyline invalidPart in invalidSegments.GetParts(
						         resultSpatialReference))
					{
						const string description = "Terrain is missing";
						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(searchedRow), invalidPart,
							Codes[Code.NoTerrainData], null);
					}

					foreach (IPolyline validPart in validSegments.GetParts(resultSpatialReference))
					{
						interpol = InterpolateShape(surface, validPart);

						validDict.Add(validPart, (IPolycurve) interpol);
					}
				}
			}

			return validDict;
		}

		private void PopulateSegmentLists([NotNull] IPointCollection polyCurvePoints,
		                                  WKSEnvelope box,
		                                  [NotNull] ISimpleSurface surface,
		                                  [NotNull] SegmentPartList missingTerrain,
		                                  [NotNull] SegmentPartList validTerrain)
		{
			IEnumVertex e1 = polyCurvePoints.EnumVertices;
			int part;
			int vertex;
			var p0 = new WKSPointZ();
			int part0 = -1;
			var inside = false;
			var valid = true;

			for (e1.QueryNext(_pointTemplate, out part, out vertex);
			     part >= 0 && vertex >= 0;
			     e1.QueryNext(_pointTemplate, out part, out vertex))
			{
				if (part0 != part)
				{
					inside = false;
					valid = true;
					part0 = part;
				}

				var p1 = new WKSPointZ();
				{
					double x, y;
					_pointTemplate.QueryCoords(out x, out y);

					p1.X = x;
					p1.Y = y;
					p1.Z = _pointTemplate.Z;
				}

				if (box.XMin <= p1.X && box.XMax >= p1.X && box.YMin <= p1.Y &&
				    box.YMax >= p1.Y)
				{
					double h = surface.GetZ(p1.X, p1.Y);
					if (double.IsNaN(h))
					{
						if (valid == false)
						{
							missingTerrain.Add(part, vertex - 1);
						}
						else if (vertex > 0)
						{
							missingTerrain.Add(part, vertex - 1, 0.5, 1);
						}

						valid = false;
					}
					else
					{
						if (valid == false)
						{
							missingTerrain.Add(part, vertex - 1, 0, 0.5);
						}

						if (vertex > 0 && valid)
						{
							if (inside)
							{
								validTerrain.Add(part, vertex - 1);
							}
							else
							{
								double f = GetFactor(p0, box, p1);
								validTerrain.Add(part, vertex - 1, f, 1);
							}
						}

						valid = true;
					}

					inside = true;
				}
				else
				{
					if (inside)
					{
						if (valid)
						{
							double f = GetFactor(p1, box, p0);
							validTerrain.Add(part, vertex - 1, 0, 1 - f);
						}
						else
						{
							missingTerrain.Add(part, vertex - 1, 0, 0.5);
						}
					}

					inside = false;
					valid = true;
				}

				p0 = p1;
			}
		}

		private static double GetFactor(WKSPointZ outBox, WKSEnvelope box, WKSPointZ inBox)
		{
			double g = 0;
			if (outBox.X < box.XMin != inBox.X < box.XMin)
			{
				double f = Factor(outBox.X, box.XMin, inBox.X);
				if (f > g)
				{
					g = f;
				}
			}

			if (outBox.X < box.XMax != inBox.X < box.XMax)
			{
				double f = Factor(outBox.X, box.XMax, inBox.X);
				if (f > g)
				{
					g = f;
				}
			}

			if (outBox.Y < box.YMin != inBox.Y < box.YMin)
			{
				double f = Factor(outBox.Y, box.YMin, inBox.Y);
				if (f > g)
				{
					g = f;
				}
			}

			if (outBox.Y < box.YMax != inBox.Y < box.YMax)
			{
				double f = Factor(outBox.Y, box.YMax, inBox.Y);
				if (f > g)
				{
					g = f;
				}
			}

			return g;
		}

		private static double Factor(double x0, double xm, double x1)
		{
			double f = (xm - x0) / (x1 - x0);
			return f;
		}

		private int CheckPoint([NotNull] IPoint shape,
		                       [CanBeNull] IPoint interpolatedPoint,
		                       [NotNull] IReadOnlyRow row)
		{
			// interpolatedPoint can be null if outside the terrain -> ErrorType.NoTerrain
			double interpolatedZ = interpolatedPoint?.Z ?? double.NaN;
			double diff = shape.Z - interpolatedZ;

			double max = Limit;
			ErrorType errorType = GetErrorType(diff, ref max);

			if (errorType == ErrorType.None)
			{
				return NoError;
			}

			if (errorType == ErrorType.NoTerrain)
			{
				return ReportError(
					MissingTerrainDescription,
					InvolvedRowUtils.GetInvolvedRows(row), GeometryFactory.Clone(shape),
					Codes[Code.NoTerrainData], TestUtils.GetShapeFieldName(row));
			}

			string description;
			// TODO use GetOffsetMessage() after changing allowed-error handling 
			// (old description format must be maintained until then)
			IssueCode issueCode;
			object[] values;
			if (ZOffsetConstraint == ZOffsetConstraint.WithinLimit)
			{
				description = string.Format("Distance from terrain {0:N1} > {1:N1}",
				                            Math.Abs(diff), Limit);
				issueCode = Codes[Code.ZOffset_TooFarFromTerrain];
				values = new object[] { diff };
			}
			else
			{
				description = GetOffsetMessage(max, out issueCode);
				values = new object[] { max };
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), GeometryFactory.Clone(shape),
				issueCode, TestUtils.GetShapeFieldName(row), values: values);
		}

		[NotNull]
		private static IList<WKSPointZ> GetWKSPointZs([NotNull] IPointCollection4 points)
		{
			int pointCount = points.PointCount;

			var result = new WKSPointZ[pointCount];

			GeometryUtils.QueryWKSPointZs(points, result);

			return result;
		}

		private int CheckPolyCurve(WKSEnvelope box,
		                           [NotNull] PolyCurveSearcher polyCurveSearcher,
		                           [CanBeNull] IPolycurve polyCurve,
		                           [NotNull] IReadOnlyRow row)
		{
			if (polyCurve == null)
			{
				return 0;
			}

			var errorCount = 0;

			var error = new SegmentPartList(polyCurve);
			double maxOffset = double.NaN;

			var parts = (IGeometryCollection) polyCurve;
			int partCount = parts.GeometryCount;

			for (var partIndex = 0; partIndex < partCount; partIndex++)
			{
				IGeometry part = parts.Geometry[partIndex];

				errorCount += CheckPartPoints(partIndex, (IPointCollection4) part, box,
				                              polyCurveSearcher, ref maxOffset, error, row);

				Marshal.ReleaseComObject(part);
			}

			errorCount += ReportAnyErrors(error, maxOffset, row, polyCurve.SpatialReference);

			return errorCount;
		}

		private bool CheckShortPart(WKSEnvelope box,
		                            [NotNull] PolyCurveSearcher polyCurveSearcher,
		                            [CanBeNull] IPolycurve polyCurve,
		                            ref double maxOffset)
		{
			if (polyCurve == null)
			{
				return true;
			}

			var parts = (IGeometryCollection) polyCurve;
			int partCount = parts.GeometryCount;

			for (var partIndex = 0; partIndex < partCount; partIndex++)
			{
				IGeometry part = parts.Geometry[partIndex];

				bool valid = CheckShortPart((IPointCollection4) part, box, polyCurveSearcher,
				                            ref maxOffset);

				Marshal.ReleaseComObject(part);

				if (valid)
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckShortPart(
			[NotNull] IPointCollection4 partPoints,
			WKSEnvelope box,
			[NotNull] PolyCurveSearcher polyCurveSearcher,
			ref double maxOffset)
		{
			IList<WKSPointZ> points = GetOffsets(partPoints, box, polyCurveSearcher);

			int pointCount = points.Count;

			for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				WKSPointZ point = points[pointIndex];
				if (double.IsNaN(point.Z))
				{
					continue;
				}

				ErrorType errorType = GetErrorType(point.Z, ref maxOffset);
				if (errorType == ErrorType.None)
				{
					return true;
				}
			}

			return false;
		}

		private int CheckPartPoints(int partIndex,
		                            [NotNull] IPointCollection4 partPoints,
		                            WKSEnvelope box,
		                            [NotNull] PolyCurveSearcher polyCurveSearcher,
		                            ref double maxOffset,
		                            [NotNull] SegmentPartList error,
		                            [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;
			IList<WKSPointZ> points = GetOffsets(partPoints, box, polyCurveSearcher);

			int pointCount = points.Count;

			ISpatialReference spatialReference = ((IGeometry) partPoints).SpatialReference;

			for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				WKSPointZ point = points[pointIndex];
				if (double.IsNaN(point.Z))
				{
					continue;
				}

				ErrorType errorType = GetErrorType(point.Z, ref maxOffset);
				if (errorType != ErrorType.None)
				{
					AddError(error, partIndex, pointIndex);
				}
				else
				{
					errorCount += ReportAnyErrors(error, maxOffset, row, spatialReference);
					maxOffset = double.NaN;
				}
			}

			errorCount += ReportAnyErrors(error, maxOffset, row, spatialReference);
			maxOffset = 0;

			return errorCount;
		}

		[NotNull]
		private static IList<WKSPointZ> GetOffsets(
			[NotNull] IPointCollection4 partPoints,
			WKSEnvelope box,
			[NotNull] PolyCurveSearcher polyCurveSearcher)
		{
			IList<WKSPointZ> points = GetWKSPointZs(partPoints);

			int pointCount = points.Count;

			for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				WKSPointZ point = points[pointIndex];

				if (box.XMin > point.X ||
				    box.XMax < point.X ||
				    box.YMin > point.Y ||
				    box.YMax < point.Y)
				{
					point.Z = double.NaN;
				}
				else
				{
					WKSPointZ? near = polyCurveSearcher.FindNearestPoint(point);
					if (near.HasValue == false)
					{
						point.Z = double.NaN;
					}
					else
					{
						double offset = near.Value.Z - point.Z;
						point.Z = offset;
					}
				}

				// Point is a struct ! assign value back to array
				points[pointIndex] = point;
			}

			return points;
		}

		private static void AddError([NotNull] SegmentPartList error,
		                             int part,
		                             int vertex)
		{
			error.Add(part, vertex - 1, 0.5, 1);
			error.Add(part, vertex, 0, 0.5);
		}

		private int ReportAnyErrors([NotNull] SegmentPartList error,
		                            double maxOffset,
		                            [NotNull] IReadOnlyRow errorRow,
		                            [NotNull] ISpatialReference spatialReference)
		{
			var errorCount = 0;

			IEnumerable<IPolyline> parts = error.GetParts(spatialReference);

			foreach (IPolyline part in parts)
			{
				IssueCode issueCode;
				string description = GetOffsetMessage(maxOffset, out issueCode);

				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(errorRow), part,
					issueCode, TestUtils.GetShapeFieldName(errorRow),
					values: new object[] { maxOffset });
			}

			error.Clear();

			return errorCount;
		}

		#region Nested type: PolyCurveSearcher

		private class PolyCurveSearcher
		{
			private readonly IPolycurve _curve;
			private readonly double _tolerance;

			private int _lastPart;
			private int _lastPoint;
			private List<IList<WKSPointZ>> _points;

			public PolyCurveSearcher([NotNull] IPolycurve polycurve, double tolerance)
			{
				_curve = polycurve;

				_tolerance = tolerance;
			}

			[NotNull]
			private List<IList<WKSPointZ>> Points
			{
				get
				{
					if (_points == null)
					{
						var parts = (IGeometryCollection) _curve;
						int nParts = parts.GeometryCount;
						_points = new List<IList<WKSPointZ>>(nParts);

						for (var iPart = 0; iPart < nParts; iPart++)
						{
							var part = (IPointCollection4) parts.get_Geometry(iPart);
							_points.Add(GetWKSPointZs(part));
						}
					}

					return _points;
				}
			}

			public WKSPointZ? FindNearestPoint(WKSPointZ point)
			{
				WKSPointZ? near = FindNearestPoint(_lastPart, _lastPoint, point);
				if (near != null)
				{
					return near;
				}

				int nParts = Points.Count;
				for (var iPart = 0; iPart < nParts; iPart++)
				{
					near = FindNearestPoint(iPart, 0, point);
					if (near != null)
					{
						return near;
					}
				}

				//bool test = false;
				//if (test)
				//{
				//    IPoint p = new PointClass();
				//    p.PutCoords(point.X, point.Y);
				//    IPoint o = new PointClass();
				//    double along = 0, offset = 0;
				//    bool right = false;
				//    _curve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension, p,
				//                                 true, o,
				//                                 ref along, ref offset, ref right);
				//    if (offset < 1) {}
				//}

				return null;
			}

			private WKSPointZ? FindNearestPoint(int partIndex,
			                                    int startPointIndex,
			                                    WKSPointZ point)
			{
				IList<WKSPointZ> points = Points[partIndex];

				WKSPointZ p1 = points[startPointIndex];
				int pointCount = points.Count;

				for (int pointIndex = startPointIndex + 1; pointIndex < pointCount; pointIndex++)
				{
					WKSPointZ p0 = p1;
					p1 = points[pointIndex];

					double sx = p1.X - p0.X;
					double sy = p1.Y - p0.Y;
					double sl = sx * sx + sy * sy;
					if (Math.Abs(sl) < double.Epsilon)
					{
						continue;
					}

					double px = point.X - p0.X;
					double py = point.Y - p0.Y;

					double s = sx * px + sy * py;
					double sn = s / sl;
					double tolerance = 2 * _tolerance / (Math.Abs(sx) + Math.Abs(sy));
					if (sn < -tolerance)
					{
						continue;
					}

					if (sn - 1 > tolerance)
					{
						continue;
					}

					double v = sx * py - sy * px;
					double vn = v / sl;
					if (Math.Abs(vn) > tolerance)
					{
						continue;
					}

					var near = new WKSPointZ
					           {
						           X = p0.X + sn * sx,
						           Y = p0.Y + sn * sy,
						           Z = p0.Z + sn * (p1.Z - p0.Z)
					           };

					_lastPart = partIndex;
					_lastPoint = pointIndex - 1;
					return near;
				}

				return null;
			}
		}

		#endregion

		#region Nested type: ShortPartInfo

		private class ShortPartInfo
		{
			public readonly IGeometry Shape;
			public bool Valid;
			public double MaxOffset;

			public ShortPartInfo([NotNull] IGeometry shape,
			                     bool valid,
			                     double maxOffset)
			{
				Shape = shape;
				Valid = valid;
				MaxOffset = maxOffset;
			}
		}

		#endregion

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
					throw new NotImplementedException("Unhandled ZOffsetConstraint " +
					                                  ZOffsetConstraint);
			}
		}
	}
}
