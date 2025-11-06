using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are two crossing lines within several different line layers
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[LinearNetworkTest]
	public class QaLineIntersect : QaSpatialRelationSelfBase
	{
		private readonly AllowedEndpointInteriorIntersections
			_allowedEndpointInteriorIntersections;

		private readonly bool _reportOverlaps;

		private readonly List<double> _xyTolerances;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly List<double> _xyResolutions; // not yet used
		private readonly string _validRelationConstraintSql;

		private IValidRelationConstraint _validRelationConstraint;

		private const AllowedLineInteriorIntersections _defaultAllowedInteriorIntersections
			= AllowedLineInteriorIntersections.None;

		private readonly IPoint _pointTemplate1 = new PointClass();
		private readonly IPoint _pointTemplate2 = new PointClass();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidLineIntersection = "InvalidLineIntersection";

			public const string InvalidLineIntersection_ConstraintNotFulfilled =
				"InvalidLineIntersection.ConstraintNotFulfilled";

			public Code() : base("LineIntersections") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaLineIntersect_0))]
		public QaLineIntersect(
				[Doc(nameof(DocStrings.QaLineIntersect_polylineClasses))] [NotNull]
				IList<IReadOnlyFeatureClass>
					polylineClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, null, AllowedEndpointInteriorIntersections.All, false) { }

		[Doc(nameof(DocStrings.QaLineIntersect_1))]
		public QaLineIntersect(
				[Doc(nameof(DocStrings.QaLineIntersect_polylineClass))] [NotNull]
				IReadOnlyFeatureClass polylineClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClass, null) { }

		[Doc(nameof(DocStrings.QaLineIntersect_2))]
		public QaLineIntersect(
			[Doc(nameof(DocStrings.QaLineIntersect_polylineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersect_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(
				polylineClasses, validRelationConstraint,
				// ReSharper disable once IntroduceOptionalParameters.Global
				AllowedEndpointInteriorIntersections.All, false) { }

		[Doc(nameof(DocStrings.QaLineIntersect_3))]
		public QaLineIntersect(
			[Doc(nameof(DocStrings.QaLineIntersect_polylineClass))] [NotNull]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaLineIntersect_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(new[] {polylineClass}, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaLineIntersect_4))]
		public QaLineIntersect(
			[Doc(nameof(DocStrings.QaLineIntersect_polylineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaLineIntersect_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint,
			[Doc(nameof(DocStrings.QaLineIntersect_allowedEndpointInteriorIntersections))]
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			[Doc(nameof(DocStrings.QaLineIntersect_reportOverlaps))]
			bool reportOverlaps)
			: base(polylineClasses,
			       GetSpatialRel(allowedEndpointInteriorIntersections, reportOverlaps))
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));

			_allowedEndpointInteriorIntersections = allowedEndpointInteriorIntersections;
			_reportOverlaps = reportOverlaps;
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);

			AllowedInteriorIntersections = _defaultAllowedInteriorIntersections;

			_xyTolerances = new List<double>(polylineClasses.Count);
			_xyResolutions = new List<double>(polylineClasses.Count);

			foreach (IReadOnlyFeatureClass polylineClass in polylineClasses)
			{
				_xyTolerances.Add(GeometryUtils.GetXyTolerance(polylineClass.SpatialReference));
				_xyResolutions.Add(
					SpatialReferenceUtils.GetXyResolution(polylineClass.SpatialReference));
			}
		}

		[InternallyUsedTest]
		public QaLineIntersect(QaLineIntersectDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>()
							 .ToList(),
				   definition.ValidRelationConstraint,
				   definition.AllowedEndpointInteriorIntersections,
				   definition.ReportOverlaps)
		{
			AllowedInteriorIntersections = definition.AllowedInteriorIntersections;
		}

		[TestParameter(_defaultAllowedInteriorIntersections)]
		[Doc(nameof(DocStrings.QaLineIntersect_AllowedInteriorIntersections))]
		public AllowedLineInteriorIntersections AllowedInteriorIntersections { get; set; }

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (row1 == row2)
			{
				// self intersections are better tested with QaSimpleGeometry
				return NoError;
			}

			var polyline1 = (IPolyline) ((IReadOnlyFeature) row1).Shape;
			var polyline2 = (IPolyline) ((IReadOnlyFeature) row2).Shape;

			double vertexIntersectionTolerance =
				GetVertexIntersectionTolerance(
					tableIndex1, tableIndex2,
					_allowedEndpointInteriorIntersections);

			IPoint knownInvalidIntersection = null;
			if (_allowedEndpointInteriorIntersections !=
			    AllowedEndpointInteriorIntersections.All)
			{
				if (! LineIntersectionUtils.HasInvalidIntersection(
					    polyline1, polyline2,
					    _allowedEndpointInteriorIntersections,
					    _reportOverlaps,
					    _pointTemplate1, _pointTemplate2,
					    vertexIntersectionTolerance,
					    out knownInvalidIntersection))
				{
					return NoError;
				}
			}

			const string description = "Intersection";

			string formattedMessage;
			if (HasFulfilledConstraint(row1, tableIndex1,
			                           row2, tableIndex2,
			                           description,
			                           out formattedMessage))
			{
				return NoError;
			}

			IGeometry errorGeometry = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				_allowedEndpointInteriorIntersections,
				AllowedInteriorIntersections,
				_reportOverlaps,
				vertexIntersectionTolerance);

			if (errorGeometry.IsEmpty && knownInvalidIntersection != null)
			{
				errorGeometry = knownInvalidIntersection;
			}

			IssueCode issueCode = _validRelationConstraintSql == null
				                      ? Codes[Code.InvalidLineIntersection]
				                      : Codes[Code.InvalidLineIntersection_ConstraintNotFulfilled];

			return ReportIntersections(formattedMessage, errorGeometry,
			                           issueCode,
			                           row1, row2);
		}

		private int ReportIntersections([NotNull] string message,
		                                [NotNull] IGeometry errorGeometry,
		                                [CanBeNull] IssueCode issueCode,
		                                [NotNull] IReadOnlyRow row1,
		                                [NotNull] IReadOnlyRow row2)
		{
			if (errorGeometry.IsEmpty)
			{
				return NoError;
			}

			var parts = errorGeometry as IGeometryCollection;
			if (parts != null && parts.GeometryCount > 1)
			{
				return GeometryUtils.GetParts(parts)
				                    .Sum(part => ReportError(
					                         message, InvolvedRowUtils.GetInvolvedRows(row1, row2),
					                         part, issueCode, null));
			}

			return ReportError(message, InvolvedRowUtils.GetInvolvedRows(row1, row2),
			                   errorGeometry, issueCode, null);
		}

		private bool HasFulfilledConstraint([NotNull] IReadOnlyRow row1, int tableIndex1,
		                                    [NotNull] IReadOnlyRow row2, int tableIndex2,
		                                    [NotNull] string description,
		                                    [NotNull] out string formattedMessage)
		{
			// TODO consider consolidating with QaSpatialRelationUtils.HasFulfilledConstraint()

			if (_validRelationConstraintSql == null)
			{
				formattedMessage = description;
				return false;
			}

			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = false;
				_validRelationConstraint =
					new ValidRelationConstraint(_validRelationConstraintSql,
					                            constraintIsDirected,
					                            GetSqlCaseSensitivity());
			}

			string conditionMessage;
			if (_validRelationConstraint.IsFulfilled(row1, tableIndex1,
			                                         row2, tableIndex2,
			                                         out conditionMessage))
			{
				formattedMessage = string.Empty;
				return true;
			}

			formattedMessage = string.Format("{0} and constraint is not fulfilled: {1}",
			                                 description, conditionMessage);
			return false;
		}

		private double GetVertexIntersectionTolerance(
			int tableIndex1, int tableIndex2,
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections)
		{
			switch (allowedEndpointInteriorIntersections)
			{
				case AllowedEndpointInteriorIntersections.All:
				case AllowedEndpointInteriorIntersections.None:
					return 0; // doesn't matter

				case AllowedEndpointInteriorIntersections.Vertex:
					return Math.Max(_xyTolerances[tableIndex1],
					                _xyTolerances[tableIndex2]);

				default:
					throw CreateArgumentOutOfRangeException(allowedEndpointInteriorIntersections);
			}
		}

		private static esriSpatialRelEnum GetSpatialRel(
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			bool reportOverlaps)
		{
			if (reportOverlaps)
			{
				return esriSpatialRelEnum.esriSpatialRelIntersects;
			}

			switch (allowedEndpointInteriorIntersections)
			{
				case AllowedEndpointInteriorIntersections.All:
					return esriSpatialRelEnum.esriSpatialRelCrosses;

				case AllowedEndpointInteriorIntersections.Vertex:
				case AllowedEndpointInteriorIntersections.None:
					return esriSpatialRelEnum.esriSpatialRelIntersects;

				default:
					throw CreateArgumentOutOfRangeException(allowedEndpointInteriorIntersections);
			}
		}

		[NotNull]
		private static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections)
		{
			return new ArgumentOutOfRangeException(
				nameof(allowedEndpointInteriorIntersections),
				allowedEndpointInteriorIntersections,
				string.Format("Unknown constraint: {0}", allowedEndpointInteriorIntersections));
		}
	}
}
