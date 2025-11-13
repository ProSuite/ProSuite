using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.EdgeMatch;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[EdgeMatchTest]
	public class QaEdgeMatchBorderingLines : ContainerTest
	{
		private readonly List<int> _lineClass1Indexes;
		private readonly int _borderClass1Index;
		private readonly List<int> _lineClass2Indexes;
		private readonly List<int> _allLineClassIndexes;
		private readonly int _borderClass2Index;

		private readonly double _searchDistance;

		private string _lineClass1BorderMatchConditionSql;
		private string _lineClass2BorderMatchConditionSql;
		private string _borderingLineMatchConditionSql;
		private string _borderingLineAttributeConstraintSql;

		private IList<string> _borderingLineEqualAttributeOptions;

		private BorderMatchCondition _lineClass1BorderMatchCondition;
		private BorderMatchCondition _lineClass2BorderMatchCondition;
		private LineMatchCondition _borderingLineMatchCondition;

		private LineAttributeConstraint _borderingLineAttributeConstraint;
		private string _borderingLineEqualAttributes;

		private EqualFieldValuesCondition _borderingLineEqualFieldValuesCondition;

		private readonly BorderConnectionCache _borderConnectionCache =
			new BorderConnectionCache();

		private readonly BorderConnectionUnion _borderConnectionUnion1 =
			new BorderConnectionUnion();

		private readonly BorderConnectionUnion _borderConnectionUnion2 =
			new BorderConnectionUnion();

		private IEnvelope _tileEnvelope;
		private WKSEnvelope _tileWksEnvelope;
		private WKSEnvelope _allWksEnvelope;

		private readonly ConstraintErrorCache _constraintErrors = new ConstraintErrorCache();

		private readonly IEnvelope _searchEnvelopeTemplate = new EnvelopeClass();

		private ISpatialReference _spatialReference;
		private ISpatialReference _highResolutionSpatialReference;
		private IList<IFeatureClassFilter> _filters;
		private IList<QueryFilterHelper> _filterHelpers;
		private readonly IDictionary<int, esriGeometryType> _geometryTypesByTableIndex;
		private readonly IDictionary<int, double> _xyToleranceByTableIndex;

		private BufferFactory _bufferFactory;

		private bool _isBorderingLineAttributeConstraintSymmetric =
			_defaultIsBorderingLineAttributeConstraintSymmetric;

		private const bool _defaultIsBorderingLineAttributeConstraintSymmetric = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident = false;

		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool _defaultAllowNonCoincidentEndPointsOnBorder = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = false;

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoMatch_NoCandidate = "NoMatch.NoCandidate";

			public const string NoMatch_NoCandidate_PartlyOutsideVerifiedExtent =
				"NoMatch.NoCandidate.PartlyOutsideVerifiedExtent";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled =
					"NoMatch.CandidateExists.BordersNotCoincident+ConstraintsFulfilled";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled =
					"NoMatch.CandidateExists.BordersNotCoincident+ConstraintsNotFulfilled";

			public const string Match_EndPointNotCoincident = "Match.EndPointNotCoincident";

			public const string Match_ConstraintsNotFulfilled =
				"Match.ConstraintsNotFulfilled";

			public Code() : base("BorderingLines") { }
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_0))]
		public QaEdgeMatchBorderingLines(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClass1))] [NotNull]
			IReadOnlyFeatureClass lineClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass1))] [NotNull]
			IReadOnlyFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClass2))] [NotNull]
			IReadOnlyFeatureClass lineClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass2))] [NotNull]
			IReadOnlyFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_searchDistance))]
			double
				searchDistance)
			: this(new[] { lineClass1 }, borderClass1,
			       new[] { lineClass2 }, borderClass2, searchDistance) { }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_1))]
		public QaEdgeMatchBorderingLines(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClasses1))] [NotNull]
			IList<IReadOnlyFeatureClass>
				lineClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass1))] [NotNull]
			IReadOnlyFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClasses2))] [NotNull]
			IList<IReadOnlyFeatureClass>
				lineClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass2))] [NotNull]
			IReadOnlyFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_searchDistance))]
			double
				searchDistance)
			: base(CastToTables(lineClasses1, new[] { borderClass1 },
			                    lineClasses2, new[] { borderClass2 }))
		{
			Assert.ArgumentNotNull(lineClasses1, nameof(lineClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(lineClasses2, nameof(lineClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));

			SearchDistance = searchDistance;
			_searchDistance = searchDistance;

			foreach (IReadOnlyFeatureClass lineClass in Union(lineClasses1, lineClasses2))
			{
				Assert.ArgumentCondition(
					lineClass.ShapeType == esriGeometryType.esriGeometryPolyline,
					string.Format("Polyline feature class expected: {0}",
					              lineClass.Name));
			}

			foreach (IReadOnlyFeatureClass borderClass in new[] { borderClass1, borderClass2 })
			{
				Assert.ArgumentCondition(
					borderClass.ShapeType == esriGeometryType.esriGeometryPolyline ||
					borderClass.ShapeType == esriGeometryType.esriGeometryPolygon,
					string.Format("Polyline or polygon feature class expected: {0}",
					              borderClass.Name));
			}

			_lineClass1Indexes = new List<int>(lineClasses1.Count);
			for (var i = 0; i < lineClasses1.Count; i++)
			{
				_lineClass1Indexes.Add(i);
			}

			_borderClass1Index = lineClasses1.Count;

			_lineClass2Indexes = new List<int>(lineClasses2.Count);
			for (var i = 0; i < lineClasses2.Count; i++)
			{
				_lineClass2Indexes.Add(_borderClass1Index + 1 + i);
			}

			_allLineClassIndexes = new List<int>();
			_allLineClassIndexes.AddRange(_lineClass1Indexes);
			_allLineClassIndexes.AddRange(_lineClass2Indexes);

			_borderClass2Index = _borderClass1Index + lineClasses2.Count + 1;

			_geometryTypesByTableIndex = GetGeometryTypesByTableIndex(InvolvedTables);
			_xyToleranceByTableIndex = GetXyToleranceByTableIndex(InvolvedTables);

			// defaults
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident;
			AllowNoFeatureWithinSearchDistance =
				_defaultAllowNoFeatureWithinSearchDistance;
			AllowNonCoincidentEndPointsOnBorder =
				_defaultAllowNonCoincidentEndPointsOnBorder;
			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[InternallyUsedTest]
		public QaEdgeMatchBorderingLines(
			[NotNull] QaEdgeMatchBorderingLinesDefinition definition)
			: this(definition.LineClasses1.Cast<IReadOnlyFeatureClass>().ToList(),
			       (IReadOnlyFeatureClass) definition.BorderClass1,
			       definition.LineClasses2.Cast<IReadOnlyFeatureClass>().ToList(),
			       (IReadOnlyFeatureClass) definition.BorderClass2,
			       definition.SearchDistance)
		{
			LineClass1BorderMatchCondition = definition.LineClass1BorderMatchCondition;
			LineClass2BorderMatchCondition = definition.LineClass2BorderMatchCondition;
			BorderingLineMatchCondition = definition.BorderingLineMatchCondition;
			BorderingLineAttributeConstraint = definition.BorderingLineAttributeConstraint;
			BorderingLineEqualAttributes = definition.BorderingLineEqualAttributes;
			BorderingLineEqualAttributeOptions = definition.BorderingLineEqualAttributeOptions;
			ReportIndividualAttributeConstraintViolations =
				definition.ReportIndividualAttributeConstraintViolations;
			IsBorderingLineAttributeConstraintSymmetric =
				definition.IsBorderingLineAttributeConstraintSymmetric;
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				definition.AllowDisjointCandidateFeatureIfBordersAreNotCoincident;
			AllowNoFeatureWithinSearchDistance = definition.AllowNoFeatureWithinSearchDistance;
			AllowNonCoincidentEndPointsOnBorder = definition.AllowNonCoincidentEndPointsOnBorder;
			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = definition
				.AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_LineClass1BorderMatchCondition))]
		public string LineClass1BorderMatchCondition
		{
			get { return _lineClass1BorderMatchConditionSql; }
			set
			{
				_lineClass1BorderMatchConditionSql = value;
				_lineClass1BorderMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_LineClass2BorderMatchCondition))]
		public string LineClass2BorderMatchCondition
		{
			get { return _lineClass2BorderMatchConditionSql; }
			set
			{
				_lineClass2BorderMatchConditionSql = value;
				_lineClass2BorderMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineMatchCondition))]
		public string BorderingLineMatchCondition
		{
			get { return _borderingLineMatchConditionSql; }
			set
			{
				_borderingLineMatchConditionSql = value;
				_borderingLineMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineAttributeConstraint))]
		public string BorderingLineAttributeConstraint
		{
			get { return _borderingLineAttributeConstraintSql; }
			set
			{
				_borderingLineAttributeConstraintSql = value;
				_borderingLineAttributeConstraint = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineEqualAttributes))]
		public string BorderingLineEqualAttributes
		{
			get { return _borderingLineEqualAttributes; }
			set
			{
				_borderingLineEqualAttributes = value;
				_borderingLineEqualFieldValuesCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineEqualAttributeOptions))]
		public IList<string> BorderingLineEqualAttributeOptions
		{
			get { return _borderingLineEqualAttributeOptions; }
			set
			{
				_borderingLineEqualAttributeOptions = value;
				_borderingLineEqualFieldValuesCondition = null;
			}
		}

		[TestParameter(false)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[TestParameter(_defaultIsBorderingLineAttributeConstraintSymmetric)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_IsBorderingLineAttributeConstraintSymmetric))]
		public bool IsBorderingLineAttributeConstraintSymmetric
		{
			get { return _isBorderingLineAttributeConstraintSymmetric; }
			set
			{
				_isBorderingLineAttributeConstraintSymmetric = value;
				_borderingLineAttributeConstraint = null;
			}
		}

		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_AllowNoFeatureWithinSearchDistance))]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[TestParameter(_defaultAllowNonCoincidentEndPointsOnBorder)]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_AllowNonCoincidentEndPointsOnBorder))]
		public bool AllowNonCoincidentEndPointsOnBorder { get; set; }

		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }

		protected override int CompleteTileCore(TileInfo tileInfo)
		{
			var errorCount = 0;

			if (tileInfo.State == TileState.Initial)
			{
				_borderConnectionUnion1.Clear();
				_borderConnectionUnion2.Clear();
				_borderConnectionCache.Clear();
				_constraintErrors.Clear();
			}
			else
			{
				errorCount += CompareBorderConnectionList(_borderConnectionUnion1, tileInfo);
				errorCount += CompareBorderConnectionList(_borderConnectionUnion2, tileInfo);

				errorCount += ReportConstraintErrors(_constraintErrors, tileInfo);

				if (tileInfo.CurrentEnvelope != null)
				{
					WKSEnvelope tileWksBox = _tileWksEnvelope;
					WKSEnvelope allWksBox = _allWksEnvelope;

					_borderConnectionCache.Clear(tileWksBox, allWksBox);
					_constraintErrors.Clear(tileWksBox, allWksBox);

					_borderConnectionUnion1.Clear(tileWksBox, allWksBox);
					_borderConnectionUnion2.Clear(tileWksBox, allWksBox);
				}
			}

			return errorCount + base.CompleteTileCore(tileInfo);
		}

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			_tileEnvelope = parameters.TileEnvelope;

			if (_tileEnvelope != null)
			{
				_tileEnvelope.QueryWKSCoords(out _tileWksEnvelope);
			}

			if (parameters.TestRunEnvelope != null)
			{
				parameters.TestRunEnvelope.QueryWKSCoords(out _allWksEnvelope);
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			if (_filters == null)
			{
				InitFilters();
			}

			if (_spatialReference == null)
			{
				_spatialReference = GetSpatialReference();
				if (_spatialReference != null)
				{
					_highResolutionSpatialReference =
						SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
							_spatialReference, 10);
				}
			}

			int borderLineClassIndex;
			int neighborBorderLineClassIndex;
			ICollection<int> neighborLineClassIndexes = GetNeighborLineClassIndexes(
				tableIndex,
				out borderLineClassIndex,
				out neighborBorderLineClassIndex);

			if (neighborLineClassIndexes == null)
			{
				return NoError;
			}

			Assert.True(borderLineClassIndex >= 0, "Unexpected border line class index");
			Assert.True(neighborBorderLineClassIndex >= 0,
			            "Unexpected neighboring border line class index");

			int errorCount =
				CheckConnections(feature, tableIndex,
				                 borderLineClassIndex,
				                 neighborLineClassIndexes,
				                 neighborBorderLineClassIndex);

			// Remark: most errors determined in CompleteTileCore
			return errorCount;
		}

		private int ReportConstraintErrors(
			[NotNull] ConstraintErrorCache constraintErrors,
			[NotNull] TileInfo tileInfo)
		{
			if (tileInfo.State == TileState.Initial)
			{
				constraintErrors.Clear();
				return NoError;
			}

			var errorCount = 0;

			WKSEnvelope tileWksBox = _tileWksEnvelope;
			WKSEnvelope allWksBox = _allWksEnvelope;

			var commonErrors = new List<ConstraintError>();

			foreach (ConstraintError error in constraintErrors.GetSortedErrors())
			{
				if (commonErrors.Count > 0 &&
				    constraintErrors.Compare(error, commonErrors[0]) != 0)
				{
					errorCount += ReportConstraintErrors(commonErrors, tileInfo,
					                                     tileWksBox, allWksBox);
					commonErrors.Clear();
				}

				commonErrors.Add(error);
			}

			errorCount += ReportConstraintErrors(commonErrors, tileInfo,
			                                     tileWksBox, allWksBox);

			return errorCount;
		}

		private int ReportConstraintErrors(
			[NotNull] IList<ConstraintError> constraintErrors,
			[NotNull] TileInfo tileInfo,
			WKSEnvelope tileWksBox,
			WKSEnvelope allWksBox)
		{
			if (constraintErrors.Count == 0)
			{
				return NoError;
			}

			ConstraintError first = constraintErrors[0];
			IReadOnlyFeature lineFeature = first.BorderConnection.Feature;
			IReadOnlyFeature neighborFeature = first.NeighborBorderConnection.Feature;

			if (tileInfo.State != TileState.Final &&
			    (! EdgeMatchUtils.VerifyHandled(lineFeature, tileWksBox, allWksBox) ||
			     ! EdgeMatchUtils.VerifyHandled(neighborFeature, tileWksBox, allWksBox)))
			{
				return NoError;
			}

			List<IPolyline> commonLines = constraintErrors.Select(error => error.ErrorLine)
			                                              .ToList();

			IPolyline errorGeometry = EdgeMatchUtils.Union(commonLines, null);

			string description;
			if (first.IssueCode.ID == Code.Match_ConstraintsNotFulfilled)
			{
				description =
					string.Format(
						LocalizableStrings.QaEdgeMatchBorderingLines_Match_ConstraintsNotFulfilled,
						FormatLength(errorGeometry.Length, _spatialReference).Trim(),
						first.ConstraintDescription);
			}
			else
			{
				description = first.ConstraintDescription;
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(lineFeature, neighborFeature),
				GeometryFactory.Clone(errorGeometry),
				first.IssueCode, first.AffectedComponents, values: new[] { first.TextValue });
		}

		private int CompareBorderConnectionList(
			[NotNull] BorderConnectionUnion borderConnectionUnion,
			[NotNull] TileInfo tileInfo)
		{
			if (tileInfo.State == TileState.Initial)
			{
				borderConnectionUnion.Clear();
				return NoError;
			}

			int errorCount = NoError;

			// IPolyline completeBorder = borderConnectionUnion.CompleteBorder;
			WKSEnvelope tileWksBox = _tileWksEnvelope;
			WKSEnvelope allWksBox = _allWksEnvelope;

			IPolyline completeBoundary = borderConnectionUnion.GetCompleteBorder(tileInfo);

			IPolyline unmatchedBoundary =
				borderConnectionUnion.GetUnmatchedBoundary(completeBoundary);
			if (unmatchedBoundary == null || unmatchedBoundary.IsEmpty)
			{
				return errorCount;
			}

			IEnvelope tileEnvelope = Assert.NotNull(tileInfo.CurrentEnvelope);

			foreach (Neighbors neighbors in borderConnectionUnion.Neighbors)
			{
				BorderConnection borderConnection = neighbors.BorderConnection;
				if (tileInfo.State != TileState.Final &&
				    ((IRelationalOperator) tileEnvelope).Disjoint(
					    borderConnection.LineAlongBorder.Envelope))
				{
					// will be handled in a later tile
					continue;
				}

				if (EdgeMatchUtils.IsDisjoint(borderConnection.LineAlongBorder,
				                              unmatchedBoundary))
				{
					// The border connection is completely matched by neighbor features
					borderConnection.UncoveredBoundary =
						GeometryFactory.CreateEmptyPolyline(borderConnection.LineAlongBorder);
					continue;
				}

				IPolyline uncoveredLineAlongBorder =
					EdgeMatchUtils.GetLinearIntersection(
						borderConnection.UncoveredBoundary,
						unmatchedBoundary,
						_xyToleranceByTableIndex[borderConnection.ClassIndex]);

				borderConnection.UncoveredBoundary = uncoveredLineAlongBorder;

				if (uncoveredLineAlongBorder.IsEmpty)
				{
					continue;
				}

				var incompleteGapErrorLines = new List<IPolyline>();
				var handledGapErrorLines = new List<IPolyline>();

				foreach (NeighborConnection neighbor in neighbors.NeighborConnections)
				{
					if (! neighbor.IsGap)
					{
						// this cannot be a mismatch error
						continue;
					}

					if (EdgeMatchUtils.IsDisjoint(neighbor.CommonLine, uncoveredLineAlongBorder))
					{
						continue;
					}

					IPolyline gapErrorLine =
						EdgeMatchUtils.GetLinearIntersection(
							uncoveredLineAlongBorder,
							neighbor.CommonLine,
							_xyToleranceByTableIndex[borderConnection.ClassIndex]);

					if (gapErrorLine.IsEmpty)
					{
						continue;
					}

					if (tileInfo.State != TileState.Final &&
					    ! EdgeMatchUtils.VerifyHandled(gapErrorLine, tileWksBox, allWksBox))
					{
						incompleteGapErrorLines.Add(gapErrorLine);
						continue;
					}

					handledGapErrorLines.Add(gapErrorLine);

					BorderConnection neighborConnection = neighbor.NeighborBorderConnection;

					if (! AllowDisjointCandidateFeatureIfBordersAreNotCoincident)
					{
						IPolyline errorLine = GetGapErrorGeometry(gapErrorLine, neighborConnection);

						ICollection<AttributeConstraintViolation> constraintViolations =
							GetAttributeConstraintViolations(
								borderConnection, neighborConnection);

						foreach (AttributeConstraintViolation constraintViolation in
						         constraintViolations)
						{
							AddConstraintError(
								borderConnection, neighborConnection,
								errorLine,
								Codes[
									Code
										.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled],
								$"{LocalizableStrings.QaEdgeMatchBorderingLines_NoMatch_CandidateExists} {constraintViolation.Description}.",
								constraintViolation.AffectedComponents,
								constraintViolation.TextValue);
						}

						if (constraintViolations.Count == 0 &&
						    ! AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)
						{
							AddConstraintError(
								borderConnection, neighborConnection,
								errorLine,
								Codes[
									Code
										.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled],
								LocalizableStrings
									.QaEdgeMatchBorderingLines_NoMatch_CandidateExists);
						}
					}
				}

				foreach (IPolyline gapErrorLine in handledGapErrorLines)
				{
					uncoveredLineAlongBorder = EdgeMatchUtils.GetDifference(
						uncoveredLineAlongBorder, gapErrorLine,
						_xyToleranceByTableIndex[borderConnection.ClassIndex]);
				}

				borderConnection.UncoveredBoundary = uncoveredLineAlongBorder;

				if (! AllowNoFeatureWithinSearchDistance)
				{
					foreach (IPolyline incompleteGapErrorLine in incompleteGapErrorLines)
					{
						uncoveredLineAlongBorder = EdgeMatchUtils.GetDifference(
							uncoveredLineAlongBorder, incompleteGapErrorLine,
							_xyToleranceByTableIndex[borderConnection.ClassIndex]);
					}
				}

				if (! AllowNoFeatureWithinSearchDistance &&
				    ! uncoveredLineAlongBorder.IsEmpty)
				{
					var uncoveredParts = (IGeometryCollection) uncoveredLineAlongBorder;

					int partCount = uncoveredParts.GeometryCount;
					for (var partIndex = 0; partIndex < partCount; partIndex++)
					{
						IPolyline uncoveredPart =
							GeometryFactory.CreatePolyline(uncoveredParts.Geometry[partIndex]);

						WKSEnvelope uncoveredBox = ProxyUtils.GetWKSEnvelope(uncoveredPart);

						// TODO revise
						if (uncoveredBox.XMax < tileWksBox.XMin ||
						    uncoveredBox.YMax < tileWksBox.YMin)
						{
							continue;
						}

						if (tileInfo.State != TileState.Final &&
						    (uncoveredBox.XMax >= tileWksBox.XMax ||
						     uncoveredBox.YMax >= tileWksBox.YMax))
						{
							continue;
						}

						IEnvelope verificationExtent = Assert.NotNull(tileInfo.AllBox);

						bool partlyOutside =
							! ((IRelationalOperator) verificationExtent).Contains(uncoveredPart);

						string codeId;
						string description;
						if (partlyOutside)
						{
							codeId = Code.NoMatch_NoCandidate_PartlyOutsideVerifiedExtent;
							description = string.Format(
								"{0} {1}",
								LocalizableStrings.QaEdgeMatchBorderingLines_NoMatch_NoCandidate,
								LocalizableStrings
									.QaEdgeMatchBorderingLines_PartlyOutsideVerifiedExtent);
						}
						else
						{
							codeId = Code.NoMatch_NoCandidate;
							description = LocalizableStrings
								.QaEdgeMatchBorderingLines_NoMatch_NoCandidate;
						}

						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(borderConnection.Feature),
							uncoveredPart, Codes[codeId], null);
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private ICollection<AttributeConstraintViolation> GetAttributeConstraintViolations(
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborConnection)
		{
			return EdgeMatchUtils.GetAttributeConstraintViolations(
				borderConnection.Feature, borderConnection.ClassIndex,
				neighborConnection.Feature, neighborConnection.ClassIndex,
				_borderingLineAttributeConstraint,
				_borderingLineEqualFieldValuesCondition,
				ReportIndividualAttributeConstraintViolations).ToList();
		}

		private void AddConstraintError([NotNull] BorderConnection borderConnection,
		                                [NotNull] BorderConnection neighborConnection,
		                                [NotNull] IPolyline commonLine,
		                                [CanBeNull] IssueCode code,
		                                [NotNull] string constraintDescription,
		                                [CanBeNull] string affectedComponents = null,
		                                [CanBeNull] string textValue = null)
		{
			BorderConnection lineClass1Connection;
			BorderConnection lineClass2Connection;
			if (IsLineClass1(borderConnection.ClassIndex))
			{
				lineClass1Connection = borderConnection;
				lineClass2Connection = neighborConnection;
			}
			else
			{
				lineClass2Connection = borderConnection;
				lineClass1Connection = neighborConnection;
			}

			_constraintErrors.Add(lineClass1Connection, lineClass2Connection,
			                      commonLine, code,
			                      constraintDescription, affectedComponents,
			                      textValue);
		}

		[NotNull]
		private IPolyline GetGapErrorGeometry([NotNull] IPolyline gapErrorLine,
		                                      [NotNull] BorderConnection neighborConnection)
		{
			IPolyline gapNeighborLine = GetNearPart(gapErrorLine,
			                                        neighborConnection.LineAlongBorder);
			if (gapNeighborLine == null)
			{
				return GeometryFactory.Clone(gapErrorLine);
			}

			return EdgeMatchUtils.Union(new[] { gapErrorLine, gapNeighborLine },
			                            _highResolutionSpatialReference);
		}

		private IPolyline GetNearPart(IPolyline toBuffer, IPolyline line)
		{
			return EdgeMatchUtils.GetNearPart(toBuffer, line, _searchDistance,
			                                  ref _bufferFactory);
		}

		private int CheckConnections([NotNull] IReadOnlyFeature feature,
		                             int lineClassIndex,
		                             int borderLineClassIndex,
		                             [NotNull] ICollection<int> neighborLineClassIndexes,
		                             int neighborBorderLineClassIndex)
		{
			int errorCount = NoError;
			// determine if the feature connects to the border 
			var line = (IPolyline) feature.Shape;

			if (line.IsEmpty)
			{
				return NoError;
			}

			IEnumerable<BorderConnection> borderConnections =
				GetBorderConnections(line, feature, lineClassIndex, borderLineClassIndex);

			BorderConnectionUnion borderUnion =
				Assert.NotNull(GetBorderConnectionUnion(lineClassIndex));

			EnsureBorderingLineMatchCondition();
			EnsureBorderingLineAttributeConstraint();
			EnsureBorderingLineEqualFieldValuesCondition();

			// search neighboring features (within search distance, also connected to THEIR border)
			foreach (BorderConnection borderConnection in borderConnections)
			{
				Neighbors neighbors =
					borderUnion.GetNeighbors(borderConnection);
				errorCount +=
					CheckBorderConnection(borderConnection, neighbors,
					                      neighborLineClassIndexes,
					                      neighborBorderLineClassIndex,
					                      _borderingLineMatchCondition);
			}

			return errorCount;
		}

		private int CheckBorderConnection(
			[NotNull] BorderConnection borderConnection,
			Neighbors neighbors,
			[NotNull] IEnumerable<int> neighborLineClassIndexes,
			int neighborBorderLineClassIndex,
			[NotNull] LineMatchCondition crossingLineMatchCondition)
		{
			int errorCount = NoError;
			if (borderConnection.UncoveredBoundary == null ||
			    borderConnection.UncoveredBoundary.IsEmpty)
			{
				return errorCount;
			}

			foreach (int neighborLineClassIndex in neighborLineClassIndexes)
			{
				foreach (IReadOnlyFeature neighborFeature in
				         SearchNeighborRows(borderConnection.GeometryAlongBoundary,
				                            neighborLineClassIndex))
				{
					if (! crossingLineMatchCondition.IsFulfilled(borderConnection.Feature,
					                                             borderConnection.ClassIndex,
					                                             neighborFeature,
					                                             neighborLineClassIndex))
					{
						continue;
					}

					var neighborLine = (IPolyline) neighborFeature.Shape;

					double distanceToNeighborLine =
						GetDistance(borderConnection.LineAlongBorder,
						            neighborLine);

					if (distanceToNeighborLine > _searchDistance)
					{
						// the line is outside the search distance
						continue;
					}

					// determine if the neighbor area is connected to it's border
					IEnumerable<BorderConnection> neighborBorderConnections =
						GetBorderConnections(neighborLine, neighborFeature, neighborLineClassIndex,
						                     neighborBorderLineClassIndex);

					foreach (BorderConnection neighborBorderConnection in neighborBorderConnections)
					{
						errorCount += CheckIsNeighborEqual(borderConnection,
						                                   neighborBorderConnection,
						                                   neighbors);
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> SearchNeighborRows([NotNull] IPolyline borderLine,
		                                                         int neighborLineClassIndex)
		{
			IFeatureClassFilter spatialFilter = _filters[neighborLineClassIndex];
			IEnvelope cacheEnvelope = borderLine.Envelope;

			WKSEnvelope searchEnvelope;
			cacheEnvelope.QueryWKSCoords(out searchEnvelope);

			searchEnvelope.XMin -= _searchDistance;
			searchEnvelope.XMax += _searchDistance;
			searchEnvelope.YMin -= _searchDistance;
			searchEnvelope.YMax += _searchDistance;

			_searchEnvelopeTemplate.PutWKSCoords(searchEnvelope);

			spatialFilter.FilterGeometry = _searchEnvelopeTemplate;

			QueryFilterHelper filterHelper = _filterHelpers[neighborLineClassIndex];

			bool origForNetwork = filterHelper.ForNetwork;
			IEnumerable<IReadOnlyFeature> features;
			try
			{
				features =
					Search(InvolvedTables[neighborLineClassIndex], spatialFilter, filterHelper)
						.Cast<IReadOnlyFeature>();
			}
			finally
			{
				filterHelper.ForNetwork = origForNetwork;
			}

			return features;
		}

		private int CheckIsNeighborEqual(
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborConnection,
			[NotNull] Neighbors neighbors)
		{
			int errorCount = NoError;
			if (neighborConnection.LineAlongBorder.IsEmpty)
			{
				return errorCount;
			}

			if (neighbors.ContainsAny(neighborConnection))
			{
				return errorCount;
			}

			IPolyline lineAlongBorder = borderConnection.LineAlongBorder;
			IPolyline neighborLineAlongBorder =
				neighborConnection.LineAlongBorder;

			IPolyline commonBorder = EdgeMatchUtils.GetCommonBorder(
				lineAlongBorder,
				neighborLineAlongBorder,
				_xyToleranceByTableIndex[borderConnection.ClassIndex]);

			if (! commonBorder.IsEmpty)
			{
				foreach (AttributeConstraintViolation issue in GetAttributeConstraintViolations(
					         borderConnection, neighborConnection))
				{
					AddConstraintError(borderConnection, neighborConnection,
					                   GeometryFactory.Clone(commonBorder),
					                   Codes[Code.Match_ConstraintsNotFulfilled],
					                   issue.Description,
					                   issue.AffectedComponents,
					                   issue.TextValue);
				}

				if (! AllowNonCoincidentEndPointsOnBorder)
				{
					var line = (IPolyline) borderConnection.Feature.Shape;

					foreach (IPoint endPoint in new[] { line.FromPoint, line.ToPoint })
					{
						if (((IRelationalOperator) commonBorder).Disjoint(endPoint))
						{
							continue;
						}

						if (((IRelationalOperator) neighborLineAlongBorder).Contains(endPoint))
						{
							errorCount += ReportEndPointError(endPoint, borderConnection.Feature,
							                                  neighborConnection.Feature);
						}
					}
				}
			}

			if (_searchDistance > 0)
			{
				IPolyline notEqualLine =
					GetNotEqualLine(commonBorder,
					                lineAlongBorder,
					                neighborLineAlongBorder,
					                _xyToleranceByTableIndex[borderConnection.ClassIndex]);

				if (notEqualLine != null && ! notEqualLine.IsEmpty)
				{
					var neighborWithGap = new NeighborConnection(neighborConnection,
					                                             notEqualLine,
					                                             isGap: true);
					neighbors.AddNeighbor(neighborWithGap);
				}
			}

			if (! commonBorder.IsEmpty)
			{
				var neighborExactMatch = new NeighborConnection(neighborConnection,
				                                                commonBorder);
				neighbors.AddNeighbor(neighborExactMatch);
			}

			return errorCount;
		}

		private int ReportEndPointError([NotNull] IPoint endPoint,
		                                [NotNull] IReadOnlyFeature lineFeature,
		                                [NotNull] IReadOnlyFeature neighborFeature)
		{
			return ReportError(
				LocalizableStrings.QaEdgeMatchBorderingLines_Match_EndPointNotCoincident,
				InvolvedRowUtils.GetInvolvedRows(lineFeature, neighborFeature),
				GeometryFactory.Clone(endPoint),
				Codes[Code.Match_EndPointNotCoincident], null);
		}

		[CanBeNull]
		private IPolyline GetNotEqualLine(
			[NotNull] IPolyline commonBorder,
			[NotNull] IPolyline lineAlongBorder,
			[NotNull] IPolyline neighborLineAlongBorder,
			double xyTolerance)
		{
			return EdgeMatchUtils.GetNotEqualLine(commonBorder, lineAlongBorder,
			                                      neighborLineAlongBorder,
			                                      _searchDistance,
			                                      ref _bufferFactory,
			                                      xyTolerance);
		}

		private static double GetDistance([NotNull] IPolyline polyline,
		                                  [NotNull] IPolyline neighbor)
		{
			var proximity = (IProximityOperator) neighbor;

			return proximity.ReturnDistance(polyline);
		}

		[NotNull]
		private IEnumerable<BorderConnection> GetBorderConnections(
			[NotNull] IPolyline line,
			[NotNull] IReadOnlyFeature lineFeature,
			int lineClassIndex,
			int borderClassIndex)
		{
			QueryFilterHelper borderClassFilterHelper = _filterHelpers[borderClassIndex];
			bool origForNetwork = borderClassFilterHelper.ForNetwork;
			try
			{
				borderClassFilterHelper.ForNetwork = true;
				return _borderConnectionCache.GetBorderConnections(
					line, lineFeature,
					lineClassIndex, borderClassIndex,
					InvolvedTables[borderClassIndex],
					_filters[borderClassIndex],
					borderClassFilterHelper,
					(table, filter, filterHelper) => Search(table, filter, filterHelper),
					GetBorderMatchCondition(lineClassIndex));
			}
			finally
			{
				borderClassFilterHelper.ForNetwork = origForNetwork;
			}
		}

		private void InitFilters()
		{
			CopyFilters(out _filters, out _filterHelpers);

			foreach (var filter in _filters)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			}

			_filters[_borderClass1Index].SpatialRelationship = GetBorderClassSpatialRelation(
				_geometryTypesByTableIndex[_borderClass1Index]);

			_filters[_borderClass2Index].SpatialRelationship = GetBorderClassSpatialRelation(
				_geometryTypesByTableIndex[_borderClass2Index]);
		}

		[CanBeNull]
		private BorderConnectionUnion GetBorderConnectionUnion(int areaClassIndex)
		{
			if (IsLineClass1(areaClassIndex))
			{
				return _borderConnectionUnion1;
			}

			if (IsLineClass2(areaClassIndex))
			{
				return _borderConnectionUnion2;
			}

			return null;
		}

		private void EnsureBorderingLineMatchCondition()
		{
			if (_borderingLineMatchCondition != null)
			{
				return;
			}

			_borderingLineMatchCondition =
				new LineMatchCondition(_borderingLineMatchConditionSql,
				                       GetSqlCaseSensitivity(_allLineClassIndexes));
		}

		private void EnsureBorderingLineEqualFieldValuesCondition()
		{
			if (_borderingLineEqualFieldValuesCondition != null)
			{
				return;
			}

			_borderingLineEqualFieldValuesCondition =
				new EqualFieldValuesCondition(_borderingLineEqualAttributes,
				                              _borderingLineEqualAttributeOptions,
				                              GetTables(_allLineClassIndexes),
				                              GetSqlCaseSensitivity(_allLineClassIndexes));
		}

		private void EnsureBorderingLineAttributeConstraint()
		{
			if (_borderingLineAttributeConstraint != null)
			{
				return;
			}

			_borderingLineAttributeConstraint = new LineAttributeConstraint(
				_borderingLineAttributeConstraintSql,
				GetSqlCaseSensitivity(_allLineClassIndexes),
				! IsBorderingLineAttributeConstraintSymmetric);
		}

		[CanBeNull]
		private ICollection<int> GetNeighborLineClassIndexes(
			int tableIndex,
			out int borderLineClassIndex,
			out int neighborBorderLineClassIndex)
		{
			if (IsLineClass1(tableIndex))
			{
				borderLineClassIndex = _borderClass1Index;
				neighborBorderLineClassIndex = _borderClass2Index;
				return _lineClass2Indexes;
			}

			if (IsLineClass2(tableIndex))
			{
				borderLineClassIndex = _borderClass2Index;
				neighborBorderLineClassIndex = _borderClass1Index;
				return _lineClass1Indexes;
			}

			// it's one of the border line classes --> ignore
			borderLineClassIndex = -1;
			neighborBorderLineClassIndex = -1;
			return null;
		}

		[NotNull]
		private IEnumerable<IReadOnlyTable> GetTables([NotNull] IEnumerable<int> tableIndexes)
		{
			var result = new SimpleSet<int>();
			foreach (int tableIndex in tableIndexes)
			{
				if (result.Contains(tableIndex))
				{
					continue;
				}

				result.Add(tableIndex);
				yield return InvolvedTables[tableIndex];
			}
		}

		private bool IsLineClass1(int tableIndex)
		{
			return tableIndex < _borderClass1Index;
		}

		private bool IsLineClass2(int tableIndex)
		{
			return tableIndex > _borderClass1Index && tableIndex < _borderClass2Index;
		}

		[NotNull]
		private static IDictionary<int, double> GetXyToleranceByTableIndex(
			[NotNull] ICollection<IReadOnlyTable> involvedTables)
		{
			var result = new Dictionary<int, double>(involvedTables.Count);

			var index = 0;
			foreach (IReadOnlyTable table in involvedTables)
			{
				var featureClass = table as IReadOnlyFeatureClass;

				double xyTolerance;
				if (featureClass == null ||
				    ! DatasetUtils.TryGetXyTolerance(featureClass.SpatialReference,
				                                     out xyTolerance))
				{
					xyTolerance = 0;
				}

				result.Add(index, xyTolerance);
				index++;
			}

			return result;
		}

		[NotNull]
		private static IDictionary<int, esriGeometryType> GetGeometryTypesByTableIndex(
			[NotNull] ICollection<IReadOnlyTable> involvedTables)
		{
			var result = new Dictionary<int, esriGeometryType>(involvedTables.Count);

			var index = 0;
			foreach (IReadOnlyTable table in involvedTables)
			{
				result.Add(index, ((IReadOnlyFeatureClass) table).ShapeType);
				index++;
			}

			return result;
		}

		private static esriSpatialRelEnum GetBorderClassSpatialRelation(
			esriGeometryType borderGeometryType)
		{
			switch (borderGeometryType)
			{
				case esriGeometryType.esriGeometryPolyline:
					return esriSpatialRelEnum.esriSpatialRelIntersects;

				case esriGeometryType.esriGeometryPolygon:
					return esriSpatialRelEnum.esriSpatialRelTouches;

				default:
					throw new ArgumentException(
						$"Unexpected border geometry type: {borderGeometryType}");
			}
		}

		[NotNull]
		private BorderMatchCondition GetBorderMatchCondition(int lineClassIndex)
		{
			if (IsLineClass1(lineClassIndex))
			{
				return _lineClass1BorderMatchCondition ??
				       (_lineClass1BorderMatchCondition =
					        new BorderMatchCondition(_lineClass1BorderMatchConditionSql,
					                                 GetSqlCaseSensitivity(lineClassIndex,
						                                 _borderClass1Index)));
			}

			if (IsLineClass2(lineClassIndex))
			{
				return _lineClass2BorderMatchCondition ??
				       (_lineClass2BorderMatchCondition =
					        new BorderMatchCondition(_lineClass2BorderMatchConditionSql,
					                                 GetSqlCaseSensitivity(lineClassIndex,
						                                 _borderClass2Index)));
			}

			throw new ArgumentException("Not a line class index");
		}

		#region nested types

		private class BorderMatchCondition : RowPairCondition
		{
			public BorderMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "LINE", "BORDER", caseSensitive) { }
		}

		private class LineMatchCondition : RowPairCondition
		{
			public LineMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "LINE1", "LINE2", caseSensitive) { }
		}

		private class LineAttributeConstraint : RowPairCondition
		{
			public LineAttributeConstraint([CanBeNull] string condition, bool caseSensitive,
			                               bool isDirected)
				: base(condition, isDirected, true, "LINE1", "LINE2",
				       caseSensitive, conciseMessage: true) { }
		}

		private class BorderConnectionCache :
			EdgeMatchBorderConnectionCache<BorderConnection>
		{
			protected override BorderConnection CreateBorderConnection(IReadOnlyFeature feature,
				int featureClassIndex,
				IReadOnlyFeature borderFeature,
				int borderClassIndex,
				IPolyline
					lineAlongBorder,
				IPolyline uncoveredLine)
			{
				var created = new BorderConnection(feature, featureClassIndex,
				                                   borderFeature, borderClassIndex,
				                                   lineAlongBorder, uncoveredLine);
				return created;
			}

			protected override bool VerifyHandled(BorderConnection borderConnection,
			                                      WKSEnvelope tileBox, WKSEnvelope allBox)
			{
				return EdgeMatchUtils.VerifyHandled(borderConnection.Feature, tileBox, allBox) ||
				       EdgeMatchUtils.VerifyHandled(borderConnection.BorderFeature, tileBox,
				                                    allBox);
			}
		}

		private class BorderConnectionUnion :
			EdgeMatchBorderConnectionUnion<Neighbors, NeighborConnection, BorderConnection>
		{
			protected override Neighbors CreateNeighbors(BorderConnection borderConnection)
			{
				return new Neighbors(borderConnection);
			}
		}

		private class Neighbors :
			EdgeMatchNeighbors<NeighborConnection, BorderConnection>
		{
			public Neighbors([NotNull] BorderConnection borderConnection)
				: base(borderConnection) { }
		}

		private class NeighborConnection : EdgeMatchNeighborConnection<BorderConnection>
		{
			public NeighborConnection([NotNull] BorderConnection neighborBorderConnection,
			                          [NotNull] IPolyline commonLine, bool isGap = false)
				: base(neighborBorderConnection, commonLine, isGap) { }
		}

		private class BorderConnection : EdgeMatchSingleBorderConnection
		{
			public BorderConnection([NotNull] IReadOnlyFeature lineFeature,
			                        int lineClassIndex,
			                        [NotNull] IReadOnlyFeature borderFeature,
			                        int borderClassIndex,
			                        [NotNull] IPolyline lineAlongBorder,
			                        [NotNull] IPolyline uncoveredBoundary)
				: base(
					lineFeature, lineClassIndex, borderFeature, borderClassIndex, lineAlongBorder)
			{
				Assert.ArgumentNotNull(borderFeature, nameof(borderFeature));

				UncoveredBoundary = uncoveredBoundary;
			}

			[NotNull]
			public IPolyline LineAlongBorder => GeometryAlongBoundary;

			public IPolyline UncoveredBoundary { get; set; }
		}

		private class ConstraintErrorCache :
			ConstraintErrorCache<ConstraintError, BorderConnection> { }

		private class ConstraintError : ConstraintError<BorderConnection> { }

		#endregion
	}
}
