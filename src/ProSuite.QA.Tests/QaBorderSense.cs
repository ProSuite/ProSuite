using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if the lakes and islands are pointing in the right direction
	/// This test should be run after an intersecting lines test
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[PolygonNetworkTest]
	public class QaBorderSense : QaNetworkBase
	{
		private readonly RingGrower<DirectedRow> _grower;
		private readonly bool _orientation;
		private int _errorCount;
		[CanBeNull] private IRelationalOperator _checkArea;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			//ToDo: review with rsc
			//ToDo: add to resources
			public const string IncompleteLine = "IncompleteLine";
			public const string DanglingLine = "DanglingLine";
			public const string InconsistentOrientation = "InconsistentOrientation";
			public const string EmptyPolygonCreated = "EmptyPolygonCreated";
			public const string InvertedOrientation = "InvertedOrientation";

			public Code() : base("BorderSense") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaBorderSense_0))]
		public QaBorderSense(
			[Doc(nameof(DocStrings.QaBorderSense_polylineClass))]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaBorderSense_clockwise))]
			bool clockwise)
			: this(new[] { polylineClass }, clockwise) { }

		[Doc(nameof(DocStrings.QaBorderSense_1))]
		public QaBorderSense(
			[Doc(nameof(DocStrings.QaBorderSense_polylineClasses))]
			IList<IReadOnlyFeatureClass> polylineClasses,
			[Doc(nameof(DocStrings.QaBorderSense_clockwise))]
			bool clockwise)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) polylineClasses), true)
		{
			_orientation = clockwise;
			_grower = new RingGrower<DirectedRow>(DirectedRow.Reverse);
			_grower.GeometryCompleted += RingGeometryCompleted;
		}

		[InternallyUsedTest]
		public QaBorderSense(QaBorderSenseDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.Clockwise) { }

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				return NoError;
			}

			_checkArea = (IRelationalOperator) args.AllBox;

			int errorCount = base.CompleteTileCore(args);

			if (ConnectedLinesList == null)
			{
				return errorCount;
			}

			errorCount += ConnectedLinesList.Sum(connectedRows => ResolveRows(connectedRows));

			foreach (LineList<DirectedRow> list in
			         _grower.GetAndRemoveCollectionsInside(args.ProcessedEnvelope))
			{
				// these are all not closed polygons and hence errors
				const string description = "Incomplete line";

				errorCount += ReportError(
					description,
					InvolvedRowUtils.GetInvolvedRows(list.GetUniqueRows(InvolvedTables)),
					CreateUnclosedErrorGeometry(list),
					Codes[Code.IncompleteLine],
					TestUtils.GetShapeFieldName(
						(IReadOnlyFeature) list.DirectedRows.First.Value.Row.Row));
			}

			return errorCount;
		}

		[NotNull]
		private static IMultipoint CreateUnclosedErrorGeometry([NotNull] LineList<DirectedRow> list)
		{
			IMultipoint result = new MultipointClass();

			var points = (IPointCollection) result;

			object missing = Type.Missing;
			points.AddPoint(GeometryFactory.Clone(list.FromPoint), ref missing, ref missing);
			points.AddPoint(GeometryFactory.Clone(list.ToPoint), ref missing, ref missing);

			return result;
		}

		private int ResolveRows([NotNull] List<DirectedRow> connectedRows)
		{
			_errorCount = 0;
			// Errors are also produced in the eventhandler RingGeometryCompleted --> member variable

			int lineCount = connectedRows.Count;

			if (lineCount == 1 &&
			    _checkArea?.Disjoint(connectedRows[0].FromPoint) != true)
			{
				const string description = "Dangling line";

				_errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(connectedRows[0].Row.Row),
					connectedRows[0].FromPoint, Codes[Code.DanglingLine], null);
			}

			connectedRows.Sort(new DirectedRow.RowByLineAngleComparer());

			DirectedRow row0 = connectedRows[lineCount - 1];
			foreach (DirectedRow row1 in connectedRows)
			{
				_grower.Add(row0.Reverse(), row1);
				row0 = row1;
			}

			return _errorCount;
		}

		/// <summary>
		/// Handles the GeometryCompleted event of the ring grower.
		/// </summary>
		/// <param name="ringGrower">The ring grower.</param>
		/// <param name="polygonLineList">The polygon line list.</param>
		private void RingGeometryCompleted([NotNull] RingGrower<DirectedRow> ringGrower,
		                                   [NotNull] LineList<DirectedRow> polygonLineList)
		{
			_errorCount += CheckPolygon(polygonLineList);
		}

		private int CheckPolygon([NotNull] LineList<DirectedRow> polygonLineList)
		{
			int errorCount = CheckConsistentOrientation(polygonLineList);

			if (errorCount > 0)
			{
				// inconsistent orientation found, report no other errors
				return errorCount;
			}

			int clockwise = polygonLineList.Orientation();

			if (clockwise == 0)
			{
				IPolyline border = polygonLineList.GetBorder();
				if (border.IsEmpty || _checkArea?.Disjoint(border.FromPoint) != true)
				{
					const string description = "Empty polygon created";

					return ReportError(
						description,
						InvolvedRowUtils.GetInvolvedRows(
							polygonLineList.GetUniqueRows(InvolvedTables)),
						border, Codes[Code.EmptyPolygonCreated], null);
				}

				return 0;
			}

			if (polygonLineList.DirectedRows.Count > 1 || ! FirstReversed(polygonLineList))
			{
				bool polyOrientation = clockwise > 0;

				if ((polyOrientation == _orientation) == FirstReversed(polygonLineList))
				{
					const string description = "Inverted orientation";

					return ReportError(
						description,
						InvolvedRowUtils.GetInvolvedRows(
							polygonLineList.GetUniqueRows(InvolvedTables)),
						polygonLineList.GetPolygon(),
						Codes[Code.InvertedOrientation], null);
				}
			}

			return NoError;
		}

		private int CheckConsistentOrientation([NotNull] LineList<DirectedRow> polygonLineList)
		{
			int errorCount = 0;
			DirectedRow row0 = polygonLineList.DirectedRows.Last.Value;

			foreach (DirectedRow row1 in polygonLineList.DirectedRows)
			{
				if (row0.IsBackward != row1.IsBackward
				    && _checkArea?.Disjoint(row0.ToPoint) != true)
				{
					const string description = "Inconsistent orientation";

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row0.Row.Row, row1.Row.Row),
						row0.ToPoint, Codes[Code.InconsistentOrientation], null);
				}

				row0 = row1;
			}

			return errorCount;
		}

		private static bool FirstReversed([NotNull] LineList<DirectedRow> polygonLineList)
		{
			return polygonLineList.DirectedRows.First.Value.IsBackward;
		}
	}
}
