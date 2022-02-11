using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.AO.Geodatabase;

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
			[Doc(nameof(DocStrings.QaBorderSense_polylineClass))] IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaBorderSense_clockwise))] bool clockwise)
			: this(new[] {polylineClass}, clockwise) { }

		[Doc(nameof(DocStrings.QaBorderSense_1))]
		public QaBorderSense(
			[Doc(nameof(DocStrings.QaBorderSense_polylineClasses))] IList<IReadOnlyFeatureClass> polylineClasses,
			[Doc(nameof(DocStrings.QaBorderSense_clockwise))] bool clockwise)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) polylineClasses), true)
		{
			_orientation = clockwise;
			_grower = new RingGrower<DirectedRow>(DirectedRow.Reverse);
			_grower.GeometryCompleted += RingGeometryCompleted;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				return NoError;
			}

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

				errorCount += ReportError(description,
				                          CreateUnclosedErrorGeometry(list),
				                          Codes[Code.IncompleteLine],
				                          TestUtils.GetShapeFieldName(
					                          (IReadOnlyFeature) list.DirectedRows.First.Value.Row.Row),
				                          InvolvedRow.CreateList(
					                          list.GetUniqueRows(InvolvedTables)));
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

			if (lineCount == 1)
			{
				const string description = "Dangling line";

				_errorCount += ReportError(description, connectedRows[0].FromPoint,
				                           Codes[Code.DanglingLine], null,
				                           connectedRows[0].Row.Row);
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
				const string description = "Empty polygon created";

				return ReportError(description, polygonLineList.GetBorder(),
				                   Codes[Code.EmptyPolygonCreated], null,
				                   InvolvedRow.CreateList(
					                   polygonLineList.GetUniqueRows(InvolvedTables)));
			}

			if (polygonLineList.DirectedRows.Count > 1 || ! FirstReversed(polygonLineList))
			{
				bool polyOrientation = clockwise > 0;

				if ((polyOrientation == _orientation) == FirstReversed(polygonLineList))
				{
					const string description = "Inverted orientation";

					return ReportError(description, polygonLineList.GetPolygon(),
					                   Codes[Code.InvertedOrientation], null,
					                   InvolvedRow.CreateList(
						                   polygonLineList.GetUniqueRows(InvolvedTables)));
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
				if (row0.IsBackward != row1.IsBackward)
				{
					const string description = "Inconsistent orientation";

					errorCount += ReportError(description,
					                          row0.ToPoint,
					                          Codes[Code.InconsistentOrientation], null,
					                          row0.Row.Row, row1.Row.Row);
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
