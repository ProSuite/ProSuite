using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there is exactly one vertex 
	/// within each polygon. 
	/// Polygons are derived out of polylines.
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	[PolygonNetworkTest]
	public class QaCentroids : QaNetworkBase
	{
		private List<IReadOnlyRow> _centroids;

		private string _constraint;
		private MultiTableView _constraintHelper;
		private RingGrower<DirectedRow> _grower;
		private List<LineList<DirectedRow>> _innerRings;
		private List<LineList<DirectedRow>> _outerRings;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string CentroidLiesOnBorder = "CentroidLiesOnBorder";
			public const string NoCentroid = "NoCentroid";
			public const string MultipleCentroids = "MultipleCentroids";
			public const string DoesNotMatchConstraint = "DoesNotMatchConstraint";
			public const string DanglingLine = "DanglingLine";

			public Code() : base("Centroids") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaCentroids_0))]
		public QaCentroids(
				[Doc(nameof(DocStrings.QaCentroids_polylineClass))]
				IReadOnlyFeatureClass polylineClass,
				[Doc(nameof(DocStrings.QaCentroids_pointClass))]
				IReadOnlyFeatureClass pointClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClass, pointClass, null) { }

		[Doc(nameof(DocStrings.QaCentroids_0))]
		public QaCentroids(
			[Doc(nameof(DocStrings.QaCentroids_polylineClass))]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaCentroids_pointClass))]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaCentroids_constraint))]
			string constraint)
			: base(CastToTables(polylineClass, pointClass), false)
		{
			Init(constraint);
		}

		[Doc(nameof(DocStrings.QaCentroids_2))]
		public QaCentroids(
				[Doc(nameof(DocStrings.QaCentroids_polylineClasses))]
				IList<IReadOnlyFeatureClass> polylineClasses,
				[Doc(nameof(DocStrings.QaCentroids_pointClasses))]
				IList<IReadOnlyFeatureClass> pointClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, pointClasses, null) { }

		[Doc(nameof(DocStrings.QaCentroids_2))]
		public QaCentroids(
			[Doc(nameof(DocStrings.QaCentroids_polylineClasses))]
			IList<IReadOnlyFeatureClass> polylineClasses,
			[Doc(nameof(DocStrings.QaCentroids_pointClasses))]
			IList<IReadOnlyFeatureClass> pointClasses,
			[Doc(nameof(DocStrings.QaCentroids_constraint))]
			string constraint)
			: base(CastToTables(polylineClasses, pointClasses), false)
		{
			Init(constraint);
		}

		[InternallyUsedTest]
		public QaCentroids(QaCentroidsDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.PointClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.Constraint
			) { }

		private void Init(string constraint)
		{
			_grower =
				new RingGrower<DirectedRow>(
					DirectedRow.Reverse);
			_grower.GeometryCompleted += RingCompleted;
			_centroids = new List<IReadOnlyRow>();
			_innerRings = new List<LineList<DirectedRow>>();
			_outerRings = new List<LineList<DirectedRow>>();
			KeepRows = true;

			_constraint = constraint;
		}

		protected override void ConfigureQueryFilter(int tableIndex, ITableFilter filter)
		{
			if (! string.IsNullOrWhiteSpace(_constraint))
			{
				_constraintHelper = _constraintHelper ?? CreateConstraintHelper(InvolvedTables[0],
					                    InvolvedTables.First(
						                    x => ((IReadOnlyFeatureClass) x).ShapeType ==
						                         esriGeometryType.esriGeometryPoint));

				var table = InvolvedTables[tableIndex];
				foreach (string columnName in _constraintHelper.GetInvolvedColumnNames())
				{
					foreach (string fieldName in
					         ExpressionUtils.GetExpressionFieldNames(table, columnName))
					{
						filter.AddField(fieldName);
					}
				}
			}

			base.ConfigureQueryFilter(tableIndex, filter);
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			IGeometry geometry = ((IReadOnlyFeature) row).Shape;

			var errorCount = 0;
			if (geometry is IPoint)
			{
				_centroids.Add(row);
			}
			else
			{
				errorCount += base.ExecuteCore(row, tableIndex);
			}

			return errorCount;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			var errorCount = 0;
			if (args.State == TileState.Initial)
			{
				return errorCount;
			}

			errorCount += base.CompleteTileCore(args);
			if (ConnectedLinesList == null)
			{
				return errorCount;
			}

			errorCount +=
				ConnectedLinesList.Sum(connectedRows => ResolveRows(connectedRows));

			List<LineList<DirectedRow>> outerRingList;
			List<LineList<DirectedRow>> innerRingList;
			List<DirectedRow> innerLineList;
			IEnvelope outerRingsBox;

			IEnvelope processedEnvelope = args.ProcessedEnvelope ?? args.AllBox;
			Assert.NotNull(processedEnvelope, "processedEnvelope");

			PrepareOuterRings(processedEnvelope, out outerRingList, out outerRingsBox);
			if (outerRingsBox == null)
			{
				return errorCount;
			}

			PrepareInnerRings(outerRingsBox, out innerRingList, out innerLineList);

			PolygonNet<DirectedRow> polyList =
				PolygonNet<DirectedRow>.Create(outerRingList,
				                               outerRingsBox,
				                               innerRingList,
				                               innerLineList);

			errorCount += AssignCentroids(polyList);
			errorCount += CheckCentroids(polyList);

			if (_constraint != null)
			{
				errorCount += CheckConstraint(polyList);
			}

			Drop(polyList);

			return errorCount;
		}

		private void Drop(
			[NotNull] IEnumerable<LineListPolygon<DirectedRow>>
				polygonList)
		{
			foreach (LineListPolygon<DirectedRow> poly in polygonList)
			{
				_outerRings.Remove(poly.OuterRing);
				foreach (DirectedRow row in poly.OuterRing.DirectedRows)
				{
					row.TopologicalLine.ClearPoly();
				}

				foreach (
					LineList<DirectedRow> innerRing in poly.InnerRingList)
				{
					_innerRings.Remove(innerRing);
					foreach (DirectedRow row in innerRing.DirectedRows)
					{
						row.TopologicalLine.ClearPoly();
					}
				}
			}
		}

		private int AssignCentroids(
			[NotNull] PolygonNet<DirectedRow> net)
		{
			var errorCount = 0;

			var unprocessedCentroids = new List<IReadOnlyRow>();

			foreach (IReadOnlyRow pointRow in _centroids)
			{
				int side;
				TopologicalLine line;
				LineListPolygon poly = net.AssignCentroid(pointRow, out line, out side);

				if (poly != null && poly.IsInnerRing)
				{
					int last = poly.Centroids.Count - 1;
					poly.Centroids.RemoveAt(last);
					poly = null;
				}

				if (line != null && side == 0)
				{
					Assert.Null(poly, "poly is not null");

					const string description = "Centroid lies on border";
					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(pointRow),
						((IReadOnlyFeature) pointRow).Shape,
						Codes[Code.CentroidLiesOnBorder],
						TestUtils.GetShapeFieldName(pointRow));
					continue;
				}

				if (poly == null)
				{
					unprocessedCentroids.Add(pointRow);
				}
			}

			_centroids = unprocessedCentroids;

			return errorCount;
		}

		private int CheckCentroids(
			[NotNull] IEnumerable<LineListPolygon<DirectedRow>>
				polyList)
		{
			var errorCount = 0;

			foreach (LineListPolygon<DirectedRow> poly in polyList)
			{
				int pointsWithin = GetReducedCount(poly.Centroids);
				if (pointsWithin == 1)
				{
					continue;
				}

				IPolygon errorGeometry = poly.GetPolygon();
				InvolvedRows involved =
					InvolvedRowUtils.GetInvolvedRows(poly.OuterRing.GetUniqueRows(InvolvedTables));

				string description =
					$"Constructed polygon contains {pointsWithin} centroids.";
				errorCount += ReportError(
					description, involved, errorGeometry,
					pointsWithin > 1 ? Codes[Code.MultipleCentroids] : Codes[Code.NoCentroid],
					null);

				if (pointsWithin <= 1)
				{
					continue;
				}

				description = "Multiple centroids of constructed polygon";
				errorCount += ReportError(
					description, involved, GetErrorGeometry(poly),
					Codes[Code.MultipleCentroids], null);
			}

			return errorCount;
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] LineListPolygon poly)
		{
			object missing = Type.Missing;

			IPointCollection multiPoint = new MultipointClass();

			foreach (IReadOnlyRow pointRow in poly.Centroids)
			{
				multiPoint.AddPoint(((IReadOnlyFeature) pointRow).Shape as Point, ref missing,
				                    ref missing);
			}

			return (IGeometry) multiPoint;
		}

		/// <summary>
		/// Centroids near Tile borders may exist twice
		/// </summary>
		private static int GetReducedCount([NotNull] ICollection<IReadOnlyRow> centroids)
		{
			if (centroids.Count < 2)
			{
				return centroids.Count;
			}

			var reducedList = new Dictionary<BaseRow, IReadOnlyRow>(new BaseRowComparer());

			foreach (IReadOnlyRow row in centroids)
			{
				var centroid = (IReadOnlyFeature) row;
				var add = new CachedRow(centroid);
				if (reducedList.ContainsKey(add) == false)
				{
					reducedList.Add(add, centroid);
				}
			}

			return reducedList.Count;
		}

		private int CheckConstraint(
			[NotNull] IEnumerable<LineListPolygon<DirectedRow>>
				polyList)
		{
			var errorCount = 0;

			foreach (LineListPolygon<DirectedRow> poly in polyList)
			{
				IReadOnlyRow centroid = GetUniqueCentroid(poly);

				errorCount += poly.OuterRing.DirectedRows.Sum(
					line => CheckConstraint(line, centroid));

				foreach (LineList<DirectedRow> ring in poly.InnerRingList)
				{
					errorCount += ring.DirectedRows.Sum(
						line => CheckConstraint(line, centroid));
				}
			}

			return errorCount;
		}

		[CanBeNull]
		private static IReadOnlyRow GetUniqueCentroid([NotNull] LineListPolygon lineListPolygon)
		{
			return lineListPolygon.Centroids.Count != 1
				       ? null
				       : lineListPolygon.Centroids[0];
		}

		private int CheckConstraint([NotNull] DirectedRow line,
		                            [CanBeNull] IReadOnlyRow centroid)
		{
			if (_constraintHelper == null && centroid != null)
			{
				_constraintHelper = CreateConstraintHelper(
					line.Row.Row.Table, centroid.Table);
			}

			Assert.Null(line.LeftCentroid, "left centroid is not null");

			var errorCount = 0;

			if (line.RightCentroid != null)
			{
				var lineFeature = (IReadOnlyFeature) line.Row.Row;

				if (centroid != null &&
				    ! _constraintHelper.MatchesConstraint(lineFeature,
				                                          centroid,
				                                          line.RightCentroid))
				{
					errorCount +=
						ReportError(
							_constraintHelper.ToString(lineFeature,
							                           centroid,
							                           line.RightCentroid),
							InvolvedRowUtils.GetInvolvedRows(
								lineFeature, centroid, line.RightCentroid),
							lineFeature.Shape,
							Codes[Code.DoesNotMatchConstraint], null);
				}

				line.RightCentroid = null;
			}
			else
			{
				line.LeftCentroid = centroid;
			}

			return errorCount;
		}

		[NotNull]
		private MultiTableView CreateConstraintHelper([NotNull] IReadOnlyTable lineTable,
		                                              [NotNull] IReadOnlyTable polyTable)
		{
			return TableViewFactory.Create(
				new[] { lineTable, polyTable, polyTable },
				new[] { "B", "L", "R" },
				_constraint,
				GetSqlCaseSensitivity(lineTable, polyTable));
		}

		private void PrepareOuterRings(
			[NotNull] IEnvelope processedEnvelope,
			[NotNull] out List<LineList<DirectedRow>> outerRings,
			[CanBeNull] out IEnvelope outerBox)
		{
			double boxXMin;
			double boxYMin;
			double boxXMax;
			double boxYMax;
			processedEnvelope.QueryCoords(out boxXMin, out boxYMin,
			                              out boxXMax, out boxYMax);

			outerRings = new List<LineList<DirectedRow>>();
			outerBox = null;

			foreach (LineList<DirectedRow> lineList in _outerRings)
			{
				IEnvelope lineListEnvelope = lineList.Envelope();
				Assert.NotNull(lineListEnvelope, "lineListEnvelope");

				double lineListXMin;
				double lineListYMin;
				double lineListXMax;
				double lineListYMax;
				lineListEnvelope.QueryCoords(out lineListXMin, out lineListYMin,
				                             out lineListXMax, out lineListYMax);

				if (lineListXMin < boxXMin ||
				    lineListXMax > boxXMax ||
				    lineListYMin < boxYMin ||
				    lineListYMax > boxYMax)
				{
					continue;
				}

				outerRings.Add(lineList);
				if (outerBox == null)
				{
					outerBox = lineListEnvelope;
				}
				else
				{
					outerBox.Union(lineListEnvelope);
				}
			}
		}

		private void PrepareInnerRings([NotNull] IEnvelope outerRingsBox,
		                               [NotNull] out
			                               List<LineList<DirectedRow>>
			                               innerRings,
		                               [NotNull] out
			                               List<DirectedRow> innerLines)
		{
			double boxXMin;
			double boxYMin;
			double boxXMax;
			double boxYMax;
			outerRingsBox.QueryCoords(out boxXMin, out boxYMin,
			                          out boxXMax, out boxYMax);

			innerRings = new List<LineList<DirectedRow>>();
			innerLines = new List<DirectedRow>();

			foreach (LineList<DirectedRow> lineList in _innerRings)
			{
				IEnvelope lineListEnvelope = lineList.Envelope();
				Assert.NotNull(lineListEnvelope, "lineListEnvelope");

				double lineListXMin;
				double lineListYMin;
				double lineListXMax;
				double lineListYMax;
				lineListEnvelope.QueryCoords(out lineListXMin, out lineListYMin,
				                             out lineListXMax, out lineListYMax);

				// remark: inner rings lying outside of outerBox cannot be assigned to any outer ring
				// --> use outerBox instead of env
				if (lineListXMin < boxXMin ||
				    lineListXMax > boxXMax ||
				    lineListYMin < boxYMin ||
				    lineListYMax > boxYMax)
				{
					continue;
				}

				innerRings.Add(lineList);
				innerLines.AddRange(lineList.DirectedRows);
			}
		}

		private int ResolveRows(List<DirectedRow> connectedRows)
		{
			var errorCount = 0;

			int lineCount = connectedRows.Count;

			if (lineCount == 1)
			{
				const string description = "Dangling line";
				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(connectedRows[0].Row.Row),
					connectedRows[0].FromPoint,
					Codes[Code.DanglingLine],
					TestUtils.GetShapeFieldName(connectedRows[0].Row.Row));
			}

			connectedRows.Sort(new DirectedRow.RowByLineAngleComparer());

			DirectedRow connectedRow = connectedRows[lineCount - 1];
			var row0 = new DirectedRow(connectedRow.TopologicalLine,
			                           ! connectedRow.IsBackward);
			foreach (DirectedRow row1 in connectedRows)
			{
				_grower.Add(row0,
				            new DirectedRow(row1.TopologicalLine,
				                            row1.IsBackward));
				row0 = new DirectedRow(row1.TopologicalLine,
				                       ! row1.IsBackward);
			}

			return errorCount;
		}

		private void RingCompleted(RingGrower<DirectedRow> sender,
		                           [NotNull] LineList<DirectedRow> polygon)
		{
			int orientation = polygon.Orientation();
			if (orientation > 0)
			{
				_outerRings.Add(polygon);
			}
			else if (orientation < 0)
			{
				_innerRings.Add(polygon);
			}

			// ignore degenerated "Line" polygons
		}
	}
}
