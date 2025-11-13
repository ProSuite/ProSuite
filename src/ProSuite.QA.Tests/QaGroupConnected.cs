using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	// TODO:
	// - revise DocStrings for QaGroupConnected_RecheckMultiplePartIssues and QaGroupConnected_CompleteGroupsOutsideTestArea
	// - document influence of 'errorReporting' on available options (Recheck, OutsideTestArea), and vice versa
	// - control default value for 'ErrorReporting' property (ddx editor/customize)
	// - evaluate if other issue types should also be addressed using CompleteGroupsOutsideTestarea (e.g. branches --> cycles)
	//   -relevant: cycle, inside branches
	// - document which combinations of RecheckMultiplePartIssues and CompleteGroupsOutsideTestArea make sense
	// - does RecheckMultiplePartIssues *always* only reduce errors, or could additional errors be found?

	/// <summary>
	/// Check if polylines with same attributes are connected
	/// </summary>
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaGroupConnected : QaGroupNetworkBase<DirectedRow>
	{
		#region nested classes

		private class EndsGap
		{
			[NotNull]
			public InvolvedGroupEnds ThisGroup { get; }

			[NotNull]
			public InvolvedGroupEnd ThisEnd { get; }

			[NotNull]
			public InvolvedGroupEnds OtherGroup { get; }

			[NotNull]
			public InvolvedGroupEnd OtherEnd { get; }

			public double Distance { get; }

			public EndsGap([NotNull] InvolvedGroupEnds group,
			               [NotNull] InvolvedGroupEnd groupEnd,
			               [NotNull] InvolvedGroupEnds otherGroup,
			               [NotNull] InvolvedGroupEnd otherEnd,
			               double distance)
			{
				ThisGroup = group;
				ThisEnd = groupEnd;
				OtherGroup = otherGroup;
				OtherEnd = otherEnd;
				Distance = distance;
			}

			public override string ToString()
			{
				return $"{Distance:f2}; {ThisEnd} <-> {OtherEnd}";
			}

			[NotNull]
			public IPath CreatePath()
			{
				IPoint start = ThisEnd.EndPoint;
				IPoint end = OtherEnd.EndPoint;

				IPointCollection line = ProxyUtils.CreatePolyline(start);

				object missing = Type.Missing;
				line.AddPoint(start, ref missing, ref missing);
				line.AddPoint(end, ref missing, ref missing);

				IGeometry path = ((IGeometryCollection) line).get_Geometry(0);

				return (IPath) path;
			}

			[NotNull]
			public IMultipoint CreateMultiPoint()
			{
				IPoint start = ThisEnd.EndPoint;
				IPoint end = OtherEnd.EndPoint;

				IPointCollection result = ProxyUtils.CreateMultipoint(start);

				object missing = Type.Missing;
				result.AddPoint(start, ref missing, ref missing);
				result.AddPoint(end, ref missing, ref missing);

				return (IMultipoint) result;
			}

			public static int CompareDistance(EndsGap x, EndsGap y)
			{
				return x.Distance.CompareTo(y.Distance);
			}

			public static int CompareDistanceList(List<EndsGap> x, List<EndsGap> y)
			{
				if (x.Count == 0)
				{
					if (y.Count == 0)
					{
						return 0;
					}

					return 1;
				}

				if (y.Count == 0)
				{
					return -1;
				}

				return x[0].Distance.CompareTo(y[0].Distance);
			}
		}

		private class EndsGapEqualityComparer : IEqualityComparer<EndsGap>
		{
			public bool Equals(EndsGap x, EndsGap y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				if (! x.Distance.Equals(y.Distance))
				{
					return false;
				}

				if (x.ThisEnd.InvolvedRows == y.ThisEnd.InvolvedRows &&
				    x.OtherEnd.InvolvedRows == y.OtherEnd.InvolvedRows)
				{
					return true;
				}

				if (x.ThisEnd.InvolvedRows == y.OtherEnd.InvolvedRows &&
				    x.OtherEnd.InvolvedRows == y.ThisEnd.InvolvedRows)
				{
					return true;
				}

				return false;
			}

			public int GetHashCode(EndsGap obj)
			{
				int result = Math.Abs(obj.ThisEnd.FirstOID -
				                      obj.OtherEnd.FirstOID)
				                 .GetHashCode();
				result = (result * 397) ^ obj.Distance.GetHashCode();
				return result;
			}
		}

		#endregion

		private const GroupErrorReporting _defaultErrorReporting =
			GroupErrorReporting.ReferToFirstPart;

		private const double _defaultIgnoreGapsLongerThan = -1;
		private const bool _defaultReportIndividualGaps = false;

		private const bool _defaultCompleteGroupsOutsideTestArea = false;

		private readonly ShapeAllowed _allowedShape;
		private readonly double _minimumErrorConnectionLineLength;

		#region issue codes

		[CanBeNull] private static ITestIssueCodes _codes;
		[CanBeNull] private static TestIssueCodes _localCodes;

		[NotNull]
		[UsedImplicitly]
		public new static ITestIssueCodes Codes
			=> _codes ?? (_codes = new AggregatedTestIssueCodes(LocalCodes,
			                                                    QaGroupNetworkBase.Codes));

		[NotNull]
		private static TestIssueCodes LocalCodes
			=> _localCodes ?? (_localCodes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidLineGroup_InsideBranch =
				"InvalidLineGroup.InsideBranch";

			public const string InvalidLineGroup_Cycle = "InvalidLineGroup.Cycle";

			public const string InvalidLineGroup_MultipleParts =
				"InvalidLineGroup.MultipleParts";

			public const string InvalidLineGroup_Branch = "InvalidLineGroup.Branch";

			public Code() : base("ConnectedLineGroups") { }
		}

		#endregion

		private int _groupCompletedErrorsCount;

		[NotNull] private readonly Dictionary<Group, RingGrower<DirectedRow>>
			_groupGrowerDict;

		[NotNull] private readonly Dictionary<RingGrower<DirectedRow>, Group>
			_growerGroupDict;

		[NotNull] private readonly Dictionary<Group, List<InvolvedGroupEnds>>
			_groupGroupendsDict;

		private readonly Dictionary<Group, MultipartGroupEnds> _multiplePartsCandidates =
			new Dictionary<Group, MultipartGroupEnds>();

		[Doc(nameof(DocStrings.QaGroupConnected_0))]
		public QaGroupConnected(
			[Doc(nameof(DocStrings.QaGroupConnected_polylineClass))]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaGroupConnected_groupBy))] [NotNull]
			IList<string> groupBy,
			[Doc(nameof(DocStrings.QaGroupConnected_allowedShape))]
			ShapeAllowed allowedShape)
			: this(new[] { polylineClass }, groupBy, null, allowedShape,
			       _defaultErrorReporting, -1) { }

		[Doc(nameof(DocStrings.QaGroupConnected_1))]
		public QaGroupConnected(
			[Doc(nameof(DocStrings.QaGroupConnected_polylineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaGroupConnected_groupBy))] [NotNull]
			IList<string> groupBy,
			[Doc(nameof(DocStrings.QaGroupConnected_valueSeparator))] [CanBeNull]
			string valueSeparator,
			[Doc(nameof(DocStrings.QaGroupConnected_allowedShape))]
			ShapeAllowed allowedShape,
			[Doc(nameof(DocStrings.QaGroupConnected_errorReporting))]
			[DefaultValue(GroupErrorReporting.ShortestGaps)]
			GroupErrorReporting errorReporting,
			[Doc(nameof(DocStrings.QaGroupConnected_minimumErrorConnectionLineLength))]
			double minimumErrorConnectionLineLength)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) polylineClasses), groupBy)
		{
			Assert.ArgumentNotNull(groupBy, nameof(groupBy));

			_allowedShape = allowedShape;
			ErrorReporting = errorReporting;
			_minimumErrorConnectionLineLength = minimumErrorConnectionLineLength;

			var groupByComparer = new GroupByComparer();
			_groupGrowerDict = new Dictionary<Group, RingGrower<DirectedRow>>(groupByComparer);
			_growerGroupDict = new Dictionary<RingGrower<DirectedRow>, Group>();
			_groupGroupendsDict =
				new Dictionary<Group, List<InvolvedGroupEnds>>(groupByComparer);

			ValueSeparatorBase = valueSeparator;
		}

		[InternallyUsedTest]
		public QaGroupConnected(QaGroupConnectedDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.GroupBy, definition.ValueSeparator, definition.AllowedShape,
			       definition.ErrorReporting, definition.MinimumErrorConnectionLineLength)
		{
			ReportIndividualGaps = definition.ReportIndividualGaps;
			IgnoreGapsLongerThan = definition.IgnoreGapsLongerThan;
			CompleteGroupsOutsideTestArea = definition.CompleteGroupsOutsideTestArea;
		}

		[TestParameter(_defaultReportIndividualGaps)]
		[Doc(nameof(DocStrings.QaGroupConnected_ReportIndividualGaps))]
		public bool ReportIndividualGaps { get; set; } = _defaultReportIndividualGaps;

		[TestParameter(_defaultIgnoreGapsLongerThan)]
		[Doc(nameof(DocStrings.QaGroupConnected_IgnoreGapsLongerThan))]
		public double IgnoreGapsLongerThan { get; set; } = _defaultIgnoreGapsLongerThan;

		// NOTE: currently not exposed as test parameter
		//[TestParameter]
		[Doc(nameof(DocStrings.QaGroupConnected_RecheckMultiplePartIssues))]
		public bool RecheckMultiplePartIssues { get; set; }

		[TestParameter(_defaultCompleteGroupsOutsideTestArea)]
		[Doc(nameof(DocStrings.QaGroupConnected_CompleteGroupsOutsideTestArea))]
		public bool CompleteGroupsOutsideTestArea { get; set; } =
			_defaultCompleteGroupsOutsideTestArea;

		// Goal: a test factory that newly includes this test parameter should continue to use the same value that the was previously used (when no parameter value was stored for the factory), 
		// until an explicit value is selected for the new parameter.
		// --> the same value passed in constructor 0, which is used by factory (--> ReferToFirstPart).
		[Doc(nameof(DocStrings.QaGroupConnected_errorReporting))]
		[DefaultValue(_defaultErrorReporting)]
		[PublicAPI]
		public GroupErrorReporting ErrorReporting { get; set; }

		protected override DirectedRow ConvertRow(DirectedRow row)
		{
			return row;
		}

		protected override DirectedRow Reverse(DirectedRow row)
		{
			return row.Reverse();
		}

		private int GetMultipartErrors(
			[NotNull] Dictionary<Group, MultipartGroupEnds> groupsErrors,
			[NotNull] IRelationalOperator extent)
		{
			if (groupsErrors.Count == 0)
			{
				return NoError;
			}

			var groupDict = new Dictionary<Group, List<ConnectedLine>>(new GroupByComparer());
			foreach (Group group in groupsErrors.Keys)
			{
				groupDict.Add(group, new List<ConnectedLine>());
			}

			IList<QueryFilterHelper> filterHelpers = null;
			for (var tableIndex = 0; tableIndex < InvolvedTables.Count; tableIndex++)
			{
				IReadOnlyTable polylineClass = InvolvedTables[tableIndex];
				IUniqueIdProvider uniqueIdProvider = GetUniqueIdProvider(tableIndex);

				#region init handlig of table constraint

				string filterExpression = GetConstraint(tableIndex);
				ITableFilter filter = GetQueryFilter(
					polylineClass, tableIndex, uniqueIdProvider,
					getAllFields: StringUtils.IsNotEmpty(filterExpression));

				QueryFilterHelper filterHelper = null;
				if (StringUtils.IsNotEmpty(filterExpression))
				{
					if (filterHelpers == null)
					{
						CopyFilters(out _, out filterHelpers);
					}

					filterHelper = filterHelpers[tableIndex];
				}

				#endregion

				foreach (IReadOnlyRow row in polylineClass.EnumRows(filter, recycle: true))
				{
					if (filterHelper?.MatchesConstraint(row) == false)
					{
						continue;
					}

					List<ConnectedLine> lineParts = null;
					foreach (Group group in GetGroups(row, tableIndex))
					{
						List<ConnectedLine> connectedGrower;
						if (! groupDict.TryGetValue(group, out connectedGrower))
						{
							continue;
						}

						lineParts = lineParts ?? GetLineParts(row, tableIndex, uniqueIdProvider);

						foreach (ConnectedLine linePart in lineParts)
						{
							connectedGrower.Add(linePart);
						}
					}
				}
			}

			var errorCount = 0;
			foreach (KeyValuePair<Group, List<ConnectedLine>> pair in groupDict)
			{
				errorCount += Validate(pair.Key, groupsErrors[pair.Key], pair.Value, extent);
			}

			return errorCount;
		}

		[NotNull]
		private ITableFilter GetQueryFilter([NotNull] IReadOnlyTable polylineClass,
		                                    int tableIndex,
		                                    [CanBeNull] IUniqueIdProvider uniqueIdProvider,
		                                    bool getAllFields)
		{
			ITableFilter filter = new AoTableFilter();

			if (WorkspaceUtils.IsInMemoryWorkspace(polylineClass.Workspace)
			    || getAllFields)
			{
				// filter for inMemoryWorkspace does not allow specific subfields
				return filter;
			}

			filter.SubFields = polylineClass.OIDFieldName;

			if (uniqueIdProvider != null)
			{
				foreach (int oidFieldIndex in uniqueIdProvider.GetOidFieldIndexes())
				{
					filter.AddField(polylineClass.Fields.Field[oidFieldIndex].Name);
				}
			}

			filter.AddField(((IReadOnlyFeatureClass) polylineClass).ShapeFieldName);

			foreach (GroupBy groupBy in GroupBys)
			{
				string field = groupBy.GetFieldName(tableIndex);
				filter.AddField(field);
			}

			return filter;
		}

		[NotNull]
		private List<ConnectedLine> GetLineParts(
			[NotNull] IReadOnlyRow row, int tableIndex,
			[CanBeNull] IUniqueIdProvider uniqueIdProvider)
		{
			bool? cancelledRow = null;

			var result = new List<ConnectedLine>();
			long? rowKeys = null;

			foreach (DirectedRow dirRow in GetDirectedRows(new TableIndexRow(row, tableIndex)))
			{
				rowKeys = rowKeys
				          ?? (uniqueIdProvider as IUniqueIdProvider<IReadOnlyFeature>)?.GetUniqueId(
					          (IReadOnlyFeature) row)
				          ?? row.OID;
				cancelledRow = cancelledRow ??
				               RecheckMultiplePartIssues
					               ? CancelTestingRow(row, recycleUnique: Guid.NewGuid(),
					                                  ignoreTestArea: true)
					               : false;

				double fromX;
				double fromY;
				dirRow.FromPoint.QueryCoords(out fromX, out fromY);
				double toX;
				double toY;
				dirRow.ToPoint.QueryCoords(out toX, out toY);

				result.Add(new ConnectedLine(tableIndex, rowKeys.Value, dirRow.PartIndex,
				                             uniqueIdProvider, cancelledRow.Value)
				           {
					           FromX = fromX,
					           FromY = fromY,
					           ToX = toX,
					           ToY = toY,
				           });
			}

			return result;
		}

		private static void Join(ICollection<NetRow> neighbors)
		{
			ConnectedLine.Join(neighbors);

			if (neighbors.Count > 1)
			{
				foreach (NetRow neighbor in neighbors)
				{
					neighbor.Connected = true;
				}
			}
		}

		private int Validate([NotNull] Group group,
		                     [NotNull] MultipartGroupEnds groupEndsList,
		                     [NotNull] List<ConnectedLine> lines,
		                     [NotNull] IRelationalOperator extent)
		{
			var errorCount = 0;
			var xElems = new List<NetRow>();
			foreach (ConnectedLine line in lines)
			{
				line.Reset();
				xElems.Add(new FromRow(line));
				xElems.Add(new ToRow(line));
			}

			SortElements(xElems, Tolerance, neighbors => { Join(neighbors); });

			var hashSet = new HashSet<List<ConnectedLine>>();
			foreach (ConnectedLine row in lines)
			{
				hashSet.UnionWith(new[] { row.Connected });
			}

			if (hashSet.Count < 2)
			{
				return errorCount;
			}

			RemoveCancelledGroups(hashSet);
			if (hashSet.Count < 2)
			{
				return errorCount;
			}

			List<GroupEnd> unhandledEnds = groupEndsList.EnumGroupEnds().ToList();

			var cmp = new ConnectedLineComparer();
			var joinedGroups = new List<Dictionary<GroupEnd, ConnectedLine>>();
			List<ConnectedLine> mainConnectedLines = null;

			foreach (List<ConnectedLine> connectedLines in hashSet)
			{
				Dictionary<GroupEnd, ConnectedLine> handled = null;
				var remain = new List<GroupEnd>(unhandledEnds.Count);
				var uniqueLines = new Dictionary<ITableIndexRowPart, ConnectedLine>(
					connectedLines.Count, cmp);

				foreach (ConnectedLine connectedLine in connectedLines)
				{
					uniqueLines.Add(connectedLine, connectedLine);
				}

				foreach (GroupEnd unhandledEnd in unhandledEnds)
				{
					ConnectedLine connectedLine;
					if (uniqueLines.TryGetValue(unhandledEnd, out connectedLine))
					{
						handled = handled ?? new Dictionary<GroupEnd, ConnectedLine>();
						handled[unhandledEnd] = connectedLine;

						mainConnectedLines = mainConnectedLines ?? connectedLines;
					}
					else
					{
						remain.Add(unhandledEnd);
					}
				}

				if (handled != null)
				{
					joinedGroups.Add(handled);
					unhandledEnds.Clear();
					unhandledEnds.AddRange(remain);
				}

				if (unhandledEnds.Count == 0)
				{
					break;
				}
			}

			if (joinedGroups.Count > 1)
			{
				List<InvolvedGroupEnds> joinedGroupEnds =
					joinedGroups.Select(GetGroupEnds).ToList();

				GroupErrorReporting errorReporting =
					ErrorReporting == GroupErrorReporting.ReferToFirstPart
						? GroupErrorReporting.ShortestGaps
						: ErrorReporting;

				errorCount += ReportCombinedErrors(group, joinedGroupEnds, errorReporting,
				                                   distancesExtent: extent);
			}
			else if (groupEndsList.CheckAll)
			{
				var joinedGroupEnds = new List<InvolvedGroupEnds>();
				if (joinedGroups.Count == 1)
				{
					joinedGroupEnds.Add(GetGroupEnds(joinedGroups[0]));
				}

				foreach (List<ConnectedLine> connectedLines in hashSet)
				{
					var otherEnds = new InvolvedGroupEnds();
					if (connectedLines == mainConnectedLines)
					{
						continue;
					}

					var endFound = false;
					foreach (ConnectedLine line in connectedLines)
					{
						if (! line.FromConnected)
						{
							otherEnds.Add(CreateInvolvedGroupEnd(new ToRow(line),
							                                     GetInvolvedRows(line)));
							endFound = true;
						}

						if (! line.ToConnected)
						{
							otherEnds.Add(
								CreateInvolvedGroupEnd(new FromRow(line), GetInvolvedRows(line)));
							endFound = true;
						}
					}

					if (! endFound)
					{
						ConnectedLine minLine = connectedLines[0];
						foreach (ConnectedLine line in connectedLines)
						{
							if (line.CompareTo(minLine) < 0)
							{
								minLine = line;
							}
						}

						otherEnds.Add(
							CreateInvolvedGroupEnd(new FromRow(connectedLines[0]),
							                       GetInvolvedRows(connectedLines[0])));
					}

					joinedGroupEnds.Add(otherEnds);
				}

				GroupErrorReporting errorReporting =
					ErrorReporting == GroupErrorReporting.ReferToFirstPart
						? GroupErrorReporting.ShortestGaps
						: ErrorReporting;

				errorCount += ReportCombinedErrors(group, joinedGroupEnds, errorReporting,
				                                   reportExtent: extent);
			}

			return errorCount;
		}

		private void RemoveCancelledGroups(
			[NotNull] HashSet<List<ConnectedLine>> allGroups)
		{
			var cancelledGroups = new List<List<ConnectedLine>>();
			foreach (List<ConnectedLine> connectedLines in allGroups)
			{
				var cancelled = true;
				foreach (ConnectedLine line in connectedLines)
				{
					if (! line.Cancelled)
					{
						cancelled = false;
					}

					break;
				}

				if (cancelled)
				{
					cancelledGroups.Add(connectedLines);
				}
			}

			foreach (List<ConnectedLine> cancelledGroup in cancelledGroups)
			{
				allGroups.Remove(cancelledGroup);
			}
		}

		[NotNull]
		private IList<InvolvedRow> GetInvolvedRows([NotNull] ConnectedLine line)
		{
			return (line.UniqueIdProvider as IInvolvedRowsProvider)?.GetInvolvedRows(line.Keys) ??
			       new List<InvolvedRow>
			       {
				       new InvolvedRow(InvolvedTables[line.TableIndex].Name,
				                       line.RowIndex)
			       };
		}

		[CanBeNull]
		private static InvolvedGroupEnds GetGroupEnds(
			[NotNull] Dictionary<GroupEnd, ConnectedLine> joinedGroup)
		{
			var involvedRows = new List<InvolvedRow>();

			foreach (KeyValuePair<GroupEnd, ConnectedLine> pair in joinedGroup)
			{
				GroupEnd groupEnd = pair.Key;
				InvolvedRowUtils.AddUniqueInvolvedRows(
					involvedRows, groupEnd.InvolvedRows);
			}

			InvolvedGroupEnds result = null;

			foreach (ConnectedLine firstLine in joinedGroup.Values)
			{
				InvolvedGroupEnds groupEnds = GetGroupEnds(
					Assert.NotNull(firstLine.Connected),
					involvedRows);

				if (groupEnds.Count == 0) // if group is cyclic !
				{
					groupEnds.Add(CreateInvolvedGroupEnd(new FromRow(firstLine), involvedRows));
				}

				result = groupEnds;
				break;
			}

			return result;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				_groupGrowerDict.Clear();
				_groupGroupendsDict.Clear();

				_multiplePartsCandidates.Clear();
			}

			_groupCompletedErrorsCount = 0;

			int errorCount = base.CompleteTileCore(args);
			errorCount += ResolveNodes();

			if (args.State != TileState.Final)
			{
				return errorCount + _groupCompletedErrorsCount;
			}

			// last tile

			IEnvelope testRunExtent = Assert.NotNull(args.AllBox);
			var testRunExtentRelOp = (IRelationalOperator) testRunExtent;

			foreach (KeyValuePair<Group, RingGrower<DirectedRow>> entry in _groupGrowerDict)
			{
				Group group = entry.Key;
				List<LineList<DirectedRow>> uncompletedLineLists =
					entry.Value.GetAndRemoveCollectionsInside(null);

				List<InvolvedGroupEnds> groupEnds = _groupGroupendsDict[group];

				if (! CompleteGroupsOutsideTestArea)
				{
					int groupErrors = CompleteInsideOnlyVerification(
						group, groupEnds, uncompletedLineLists, testRunExtentRelOp);

					errorCount += groupErrors;
				}
				else
				{
					MultipartGroupEnds multiPart = AddMultiplePartsCandidates(
						group, null, new List<InvolvedGroupEnd>());

					multiPart.CheckAll = true;
				}
			}

			if (RecheckMultiplePartIssues || CompleteGroupsOutsideTestArea)
			{
				errorCount += GetMultipartErrors(_multiplePartsCandidates, testRunExtentRelOp);
			}

			return errorCount + _groupCompletedErrorsCount;
		}

		private int CompleteInsideOnlyVerification(
			[NotNull] Group group,
			[NotNull] IList<InvolvedGroupEnds> groupEnds,
			[NotNull] IList<LineList<DirectedRow>> uncompletedLineLists,
			[NotNull] IRelationalOperator testRunExtentRelOp)
		{
			var errorCount = 0;

			// optimistic assumption : all parts not completely within allBox are connected (outside allBox)
			if (ErrorReporting == GroupErrorReporting.ReferToFirstPart)
			{
				if (groupEnds.Count == 0 || uncompletedLineLists.Count == 0)
				{
					return errorCount;
				}

				errorCount += ReportMultiplePartsErrorReferToFirstPart(
					group,
					new[]
					{
						CreateInvolvedGroupEnd(uncompletedLineLists[0].DirectedRows.First.Value)
					},
					groupEnds[0]);
			}
			else if (RecheckMultiplePartIssues)
			{
				// check everything at end, not here
			}
			else
			{
				if (groupEnds.Count == 0 || groupEnds.Count + uncompletedLineLists.Count < 2)
				{
					return errorCount;
				}

				var groupEndsPlusUncomplete = new List<InvolvedGroupEnds>(groupEnds.Count + 1);
				groupEndsPlusUncomplete.AddRange(groupEnds);

				if (uncompletedLineLists.Count > 0)
				{
					var uncompletedEnds = new InvolvedGroupEnds(
						GetUncompletedEnds(uncompletedLineLists, testRunExtentRelOp));

					groupEndsPlusUncomplete.Add(uncompletedEnds);
				}

				errorCount += ReportCombinedErrors(group,
				                                   groupEndsPlusUncomplete,
				                                   ErrorReporting);
			}

			return errorCount;
		}

		protected sealed override int OnNodeAssembled(
			[NotNull] List<DirectedRow> directedRows,
			[NotNull] Dictionary<Group, List<DirectedRow>> groupDict)
		{
			var errorCount = 0;
			foreach (KeyValuePair<Group, List<DirectedRow>> pair in groupDict)
			{
				Group group = pair.Key;
				List<DirectedRow> groupRows = pair.Value;

				RingGrower<DirectedRow> grower;
				if (! _groupGrowerDict.TryGetValue(group, out grower))
				{
					grower = new RingGrower<DirectedRow>(Reverse);
					grower.GeometryCompleted += GroupCompleted;

					_groupGrowerDict.Add(group, grower);
					_growerGroupDict.Add(grower, group);
					_groupGroupendsDict.Add(group, new List<InvolvedGroupEnds>());
				}

				errorCount += AddGroups(group, groupRows, grower);
			}

			return errorCount;
		}

		private int AddGroups([NotNull] Group group,
		                      [NotNull] IList<DirectedRow> directedRows,
		                      [NotNull] RingGrower<DirectedRow> grower)
		{
			int errorCount = ReportAddGroupsErrors(group, directedRows);

			int lineCount = directedRows.Count;

			if (lineCount > 0)
			{
				DirectedRow row0 = directedRows[lineCount - 1];

				foreach (DirectedRow row1 in directedRows)
				{
					grower.Add(Reverse(row0), row1);
					row0 = row1;
				}
			}

			return errorCount;
		}

		[NotNull]
		private IList<InvolvedGroupEnd> GetUncompletedEnds(
			[NotNull] IList<LineList<DirectedRow>> lineLists,
			[NotNull] IRelationalOperator allBox)
		{
			var insideEnds = new List<InvolvedGroupEnd>();

			foreach (LineList<DirectedRow> lineList in lineLists)
			{
				foreach (DirectedRow insideEnd in GetInsideEnds(lineList, allBox))
				{
					InvolvedGroupEnd involved = CreateInvolvedGroupEnd(insideEnd);
					insideEnds.Add(involved);
				}
			}

			if (insideEnds.Count == 0)
			{
				insideEnds.Add(
					CreateInvolvedGroupEnd(lineLists[0].DirectedRows.First.Value.Reverse()));
			}

			return insideEnds;
		}

		[NotNull]
		private InvolvedGroupEnd CreateInvolvedGroupEnd([NotNull] DirectedRow row)
		{
			return new InvolvedGroupEnd(
				row.Row.TableIndex, row.Row.RowOID, row.PartIndex, row.IsBackward,
				GetInvolvedRows(row.Row), row.ToPoint, row.FromPoint);
		}

		[NotNull]
		private static InvolvedGroupEnd CreateInvolvedGroupEnd(
			[NotNull] NetRow row, [NotNull] IList<InvolvedRow> involvedRows)
		{
			return new InvolvedGroupEnd(
				row.Row.TableIndex, row.Row.RowIndex, row.Row.PartIndex, row.IsBackward,
				involvedRows, row.GetToPoint(), row.GetFromPoint());
		}

		[NotNull]
		private static IEnumerable<DirectedRow> GetInsideEnds(
			[NotNull] LineList<DirectedRow> uncompletedLineList,
			[NotNull] IRelationalOperator allBox)
		{
			List<DirectedRow> allEnds = uncompletedLineList.GetEnds();
			var result = new List<DirectedRow>(allEnds.Count);

			foreach (DirectedRow end in allEnds)
			{
				if (! allBox.Disjoint(end.ToPoint))
				{
					result.Add(end);
				}
			}

			return result;
		}

		private bool IgnoreLonger(double gapDistance)
		{
			if (IgnoreGapsLongerThan <= 0)
			{
				return false;
			}

			return gapDistance > IgnoreGapsLongerThan;
		}

		protected override int ReportAddGroupsErrors(
			Group group, IList<DirectedRow> groupRows)
		{
			int lineCount = groupRows.Count;
			if (lineCount == 0)
			{
				return NoError;
			}

			if (lineCount <= 2 || _allowedShape != 0)
			{
				return NoError;
			}

			InvolvedRows involvedRows = new InvolvedRows();
			foreach (DirectedRow row in groupRows)
			{
				InvolvedRowUtils.AddUniqueInvolvedRows(
					involvedRows,
					InvolvedRowUtils.GetInvolvedRows(row.Row.Row));
			}

			const string description = "Group branches";
			return ReportError(description, involvedRows,
			                   groupRows[0].FromPoint,
			                   LocalCodes[Code.InvalidLineGroup_Branch], null);
		}

		[NotNull]
		private MultipartGroupEnds AddMultiplePartsCandidates(
			[NotNull] Group group,
			[CanBeNull] IList<InvolvedGroupEnd> groupEndRows,
			[NotNull] IList<InvolvedGroupEnd> otherPartEnds)
		{
			MultipartGroupEnds ends;
			if (! _multiplePartsCandidates.TryGetValue(group, out ends))
			{
				ends = new MultipartGroupEnds(groupEndRows);

				_multiplePartsCandidates.Add(group, ends);
			}

			foreach (InvolvedGroupEnd involvedGroupEnd in otherPartEnds)
			{
				ends.Add(involvedGroupEnd);
			}

			return ends;
		}

		private int ReportMultiplePartsErrorReferToFirstPart(
			[NotNull] Group group,
			[NotNull] IList<InvolvedGroupEnd> groupEndRows,
			[NotNull] IList<InvolvedGroupEnd> otherPartEnds)
		{
			if (RecheckMultiplePartIssues)
			{
				AddMultiplePartsCandidates(group, groupEndRows, otherPartEnds);

				return NoError;
			}

			int endRowCount = groupEndRows.Count;
			InvolvedRows involvedRows = new InvolvedRows();

			for (var i = 0; i < endRowCount; i++)
			{
				InvolvedRowUtils.AddUniqueInvolvedRows(
					involvedRows, groupEndRows[i].InvolvedRows);
			}

			foreach (InvolvedGroupEnd involvedGroupEnd in otherPartEnds)
			{
				InvolvedRowUtils.AddUniqueInvolvedRows(
					involvedRows, involvedGroupEnd.InvolvedRows);
			}

			IPointCollection endPoints = null;
			object missing = Type.Missing;
			for (var i = 0; i < endRowCount; i++)
			{
				InvolvedGroupEnd groupEndRow = groupEndRows[i];

				IPoint endPoint = groupEndRow.EndPoint;

				if (endPoints == null)
				{
					endPoints = ProxyUtils.CreateMultipoint(endPoint);
				}

				endPoints.AddPoint(GeometryFactory.Clone(endPoint),
				                   ref missing, ref missing);
			}

			foreach (InvolvedGroupEnd involvedGroupEnd in otherPartEnds)
			{
				Assert.NotNull(endPoints, nameof(endPoints));
				endPoints.AddPoint(GeometryFactory.Clone(involvedGroupEnd.AnyPoint),
				                   ref missing, ref missing);
			}

			string description =
				string.Format("At least 2 groups with attributes {0} exist." +
				              " (Errorgeometry = endpoints of one group and any point(s) of another group)",
				              group.GetInfo(GroupBys));

			return ReportError(description, involvedRows,
			                   (IGeometry) endPoints,
			                   LocalCodes[Code.InvalidLineGroup_MultipleParts], null);
		}

		private int ReportCombinedErrors([NotNull] Group group,
		                                 [NotNull] List<InvolvedGroupEnds> groupEndsList,
		                                 GroupErrorReporting errorReporting,
		                                 int i = 1,
		                                 IRelationalOperator distancesExtent = null,
		                                 IRelationalOperator reportExtent = null)
		{
			if (groupEndsList.Count < 2)
			{
				return NoError;
			}

			List<List<EndsGap>> sortedEndsGaps = GetSortedEndsDistances(groupEndsList,
				distancesExtent);

			List<EndsGap> reportGaps = GetReportGaps(sortedEndsGaps, errorReporting);

			if (reportGaps == null || reportGaps.Count <= 0)
			{
				return NoError;
			}

			return ReportIndividualGaps
				       ? ReportIndividualErrors(reportGaps, group, groupEndsList, reportExtent)
				       : ReportMultipartErrors(reportGaps, group, groupEndsList, reportExtent);
		}

		private int ReportMultipartErrors(
			[NotNull] IList<EndsGap> gaps,
			[NotNull] Group group,
			[NotNull] ICollection<InvolvedGroupEnds> groupEndsList,
			[CanBeNull] IRelationalOperator extent = null)
		{
			List<EndsGap> longGaps;
			List<EndsGap> shortGaps;
			ClassifyGaps(gaps, out longGaps, out shortGaps);

			IPolyline errorLines = null;
			if (longGaps.Count > 0)
			{
				errorLines = GetLinearErrorGeometry(longGaps);
				if (extent?.Disjoint(errorLines) == true)
				{
					errorLines = null;
					longGaps.Clear();
				}
			}

			IMultipoint errorPoints = null;
			if (shortGaps.Count > 0)
			{
				errorPoints = GetMultipointErrorGeometry(shortGaps);
				if (extent?.Disjoint(errorLines) == true)
				{
					errorPoints = null;
					shortGaps.Clear();
				}
			}

			var errorCount = 0;

			string baseDescription = string.Format("{0} line groups for {1} exist",
			                                       groupEndsList.Count,
			                                       group.GetInfo(GroupBys));

			int totalGaps = longGaps.Count + shortGaps.Count;

			if (errorLines != null)
			{
				string description;
				if (longGaps.Count < totalGaps)
				{
					string format =
						shortGaps.Count == 1
							? "{0} of {1} gaps reported - end points of {2} gap shorter than {3} reported separately"
							: "{0} of {1} gaps reported - end points of {2} gaps shorter than {3} reported separately";

					string addition = string.Format(format,
					                                longGaps.Count,
					                                totalGaps,
					                                shortGaps.Count,
					                                FormatLength(_minimumErrorConnectionLineLength,
					                                             errorLines.SpatialReference).Trim
						                                ());

					description = $"{baseDescription} ({addition})";
				}
				else
				{
					description = baseDescription;
				}

				errorCount += ReportError(
					description, GetInvolvedRows(longGaps), errorLines,
					LocalCodes[Code.InvalidLineGroup_MultipleParts], null);
			}

			if (errorPoints != null)
			{
				string description;
				if (shortGaps.Count < totalGaps)
				{
					string format =
						longGaps.Count == 1
							? "{0} of {1} gaps reported - connection lines for {2} gap longer than {3} reported separately"
							: "{0} of {1} gaps reported - connection lines for {2} gaps longer than {3} reported separately";

					string addition = string.Format(format,
					                                longGaps.Count,
					                                totalGaps,
					                                shortGaps.Count,
					                                FormatLength(_minimumErrorConnectionLineLength,
					                                             errorPoints.SpatialReference).Trim
						                                ());

					description = string.Format("{0} ({1})", baseDescription, addition);
				}
				else
				{
					description = baseDescription;
				}

				errorCount += ReportError(description, GetInvolvedRows(shortGaps), errorPoints,
				                          LocalCodes[Code.InvalidLineGroup_MultipleParts], null);
			}

			return errorCount;
		}

		private int ReportIndividualErrors(
			[NotNull] IEnumerable<EndsGap> gaps,
			[NotNull] Group group,
			[NotNull] ICollection<InvolvedGroupEnds> groupEndsList,
			[CanBeNull] IRelationalOperator extent = null)
		{
			string groupInfo = group.GetInfo(GroupBys);

			var errorCount = 0;

			foreach (EndsGap gap in gaps)
			{
				IGeometry errorGeometry = gap.Distance > _minimumErrorConnectionLineLength
					                          ? (IGeometry) GetLinearErrorGeometry(gap)
					                          : gap.CreateMultiPoint();

				if (extent?.Disjoint(errorGeometry) == true)
				{
					continue;
				}

				string description =
					string.Format(
						"Gap between two line groups (of {0}) for {1} (gap distance: {2})",
						groupEndsList.Count,
						groupInfo,
						FormatLength(gap.Distance, errorGeometry.SpatialReference).Trim());

				errorCount += ReportError(description, GetInvolvedRows(gap), errorGeometry,
				                          LocalCodes[Code.InvalidLineGroup_MultipleParts], null);
			}

			return errorCount;
		}

		private void ClassifyGaps([NotNull] IEnumerable<EndsGap> gaps,
		                          [NotNull] out List<EndsGap> longGaps,
		                          [NotNull] out List<EndsGap> shortGaps)
		{
			shortGaps = new List<EndsGap>();
			longGaps = new List<EndsGap>();

			foreach (EndsGap gap in gaps)
			{
				if (gap.Distance >= _minimumErrorConnectionLineLength)
				{
					longGaps.Add(gap);
				}
				else
				{
					shortGaps.Add(gap);
				}
			}
		}

		[NotNull]
		private static IPolyline GetLinearErrorGeometry([NotNull] EndsGap gap)
		{
			object missing = Type.Missing;

			IPath connection = gap.CreatePath();

			IGeometryCollection lineParts = ProxyUtils.CreatePolyline(connection);

			lineParts.AddGeometry(connection, ref missing, ref missing);

			return (IPolyline) lineParts;
		}

		[NotNull]
		private static IPolyline GetLinearErrorGeometry(
			[NotNull] ICollection<EndsGap> gaps)
		{
			Assert.ArgumentNotNull(gaps, nameof(gaps));
			Assert.ArgumentCondition(gaps.Count > 0, "invalid gaps count");

			object missing = Type.Missing;

			IGeometryCollection result = null;

			foreach (EndsGap gap in gaps)
			{
				IPath connection = gap.CreatePath();
				if (result == null)
				{
					result = ProxyUtils.CreatePolyline(connection);
				}

				result.AddGeometry(connection, ref missing, ref missing);
			}

			return (IPolyline) Assert.NotNull(result);
		}

		[NotNull]
		private static IMultipoint GetMultipointErrorGeometry(
			[NotNull] ICollection<EndsGap> gaps)
		{
			Assert.ArgumentNotNull(gaps, nameof(gaps));
			Assert.ArgumentCondition(gaps.Count > 0, "invalid gaps count");

			object missing = Type.Missing;
			IPointCollection result = null;

			foreach (EndsGap gap in gaps)
			{
				IPoint point0 = gap.ThisEnd.EndPoint;
				IPoint point1 = gap.OtherEnd.EndPoint;

				if (result == null)
				{
					result = ProxyUtils.CreateMultipoint(point0);
					((IGeometry) result).SpatialReference = point0.SpatialReference;
				}

				result.AddPoint(point0, ref missing, ref missing);
				result.AddPoint(point1, ref missing, ref missing);
			}

			Assert.NotNull(result, "result");

			return (IMultipoint) result;
		}

		[CanBeNull]
		private List<EndsGap> GetReportGaps([NotNull] List<List<EndsGap>> sortedEndsGaps,
		                                    GroupErrorReporting errorReporting)
		{
			switch (errorReporting)
			{
				case GroupErrorReporting.ShortestGaps:
					return GetShortestGaps(sortedEndsGaps);

				case GroupErrorReporting.CombineParts:
					return GetNonBranchGaps(sortedEndsGaps);

				default:
					throw new InvalidOperationException("Unhandled ErrorReporting " +
					                                    errorReporting);
			}
		}

		[CanBeNull]
		private List<EndsGap> GetNonBranchGaps([NotNull] List<List<EndsGap>> sortedEndsGaps)
		{
			List<EndsGap> result = null;
			var linked = new LinkedListHelper<InvolvedGroupEnds>();

			List<List<EndsGap>> reducedList = sortedEndsGaps;

			var handledGaps = new HashSet<EndsGap>(new EndsGapEqualityComparer());

			while (reducedList.Count > 0)
			{
				EndsGap shortestGap = reducedList[0][0];

				if (IgnoreLonger(shortestGap.Distance))
				{
					break;
				}

				bool validConnection =
					linked.TryAdd(shortestGap.ThisGroup, shortestGap.OtherGroup);

				if (validConnection && ! handledGaps.Contains(shortestGap))
				{
					if (result == null)
					{
						result = new List<EndsGap>();
					}

					result.Add(shortestGap);

					handledGaps.Add(shortestGap);
				}

				reducedList = ReduceLists(reducedList, shortestGap, validConnection);
				reducedList.Sort(EndsGap.CompareDistanceList);
			}

			return result;
		}

		[CanBeNull]
		private List<EndsGap> GetShortestGaps(
			[NotNull] IEnumerable<List<EndsGap>> sortedEndsGaps)
		{
			List<EndsGap> result = null;

			var handledGroups = new HashSet<InvolvedGroupEnds>();
			var handledGaps = new HashSet<EndsGap>(new EndsGapEqualityComparer());

			var cancel = false;
			foreach (List<EndsGap> sortedGaps in sortedEndsGaps)
			{
				if (cancel)
				{
					break;
				}

				foreach (EndsGap sortedGap in sortedGaps)
				{
					if (handledGroups.Contains(sortedGap.ThisGroup))
					{
						// all members of sortedDistances have the same ThisEnd and ThisGroup
						break;
					}

					if (IgnoreLonger(sortedGap.Distance))
					{
						cancel = true; // all following gaps are longer --> stop here
						break;
					}

					if (handledGaps.Contains(sortedGap))
					{
						break;
					}

					if (result == null)
					{
						result = new List<EndsGap>();
					}

					result.Add(sortedGap);

					Assert.True(handledGroups.Add(sortedGap.ThisGroup),
					            "Group already added: {0}", sortedGap.ThisGroup);
					handledGroups.Add(sortedGap.OtherGroup); // ok if already added

					Assert.True(handledGaps.Add(sortedGap), "Gap already added: {0}", sortedGap);
					break;
				}
			}

			return result;
		}

		[NotNull]
		private static InvolvedRows GetInvolvedRows([NotNull] EndsGap gap)
		{
			var result = new InvolvedRows();

			InvolvedRowUtils.AddUniqueInvolvedRows(result, gap.ThisEnd.InvolvedRows);
			InvolvedRowUtils.AddUniqueInvolvedRows(result, gap.OtherEnd.InvolvedRows);

			return result;
		}

		//[NotNull]
		//private IEnumerable<InvolvedRow> GetInvolvedRows(
		//    [NotNull] ICollection<GroupEnds> groupEndsList)
		//{
		//    var result = new List<InvolvedRow>(groupEndsList.Count * 2);

		//    foreach (GroupEnds groupEnds in groupEndsList)
		//    {
		//        foreach (DirectedRow groupEndRow in groupEnds.EndRows)
		//        {
		//            InvolvedRowUtils.AddUniqueInvolvedRows(
		//                result, GetInvolvedRows(groupEndRow.Row.Row));
		//        }
		//    }

		//    return result;
		//}

		[NotNull]
		private static InvolvedRows GetInvolvedRows(
			[NotNull] ICollection<EndsGap> gaps)
		{
			InvolvedRows result = new InvolvedRows();

			foreach (EndsGap gap in gaps)
			{
				InvolvedRowUtils.AddUniqueInvolvedRows(
					result, gap.ThisEnd.InvolvedRows);
				InvolvedRowUtils.AddUniqueInvolvedRows(
					result, gap.OtherEnd.InvolvedRows);
			}

			return result;
		}

		[NotNull]
		private static List<List<EndsGap>> GetSortedEndsDistances(
			[NotNull] ICollection<InvolvedGroupEnds> groupEndsList,
			IRelationalOperator extent = null)
		{
			var result = new List<List<EndsGap>>(groupEndsList.Count * 2);

			foreach (InvolvedGroupEnds group in groupEndsList)
			{
				foreach (InvolvedGroupEnd groupEnd in group)
				{
					bool outside = extent?.Disjoint(groupEnd.EndPoint) ?? false;
					var point = (IProximityOperator) groupEnd.EndPoint;
					var distances = new List<EndsGap>(groupEndsList.Count * 2);

					result.Add(distances);

					foreach (InvolvedGroupEnds otherGroup in groupEndsList)
					{
						if (otherGroup == group)
						{
							continue;
						}

						foreach (InvolvedGroupEnd otherEnd in otherGroup)
						{
							if (outside && extent.Disjoint(otherEnd.EndPoint))
							{
								continue;
							}

							double distance = point.ReturnDistance(otherEnd.EndPoint);

							var endsDistance = new EndsGap(group, groupEnd,
							                               otherGroup, otherEnd,
							                               distance);

							distances.Add(endsDistance);
						}
					}

					distances.Sort(EndsGap.CompareDistance);
				}
			}

			result.Sort(EndsGap.CompareDistanceList);

			return result;
		}

		[NotNull]
		private static List<List<EndsGap>> ReduceLists(
			[NotNull] List<List<EndsGap>> endsGaps,
			[NotNull] EndsGap endsGap,
			bool validConnection)
		{
			if (! validConnection)
			{
				endsGaps[0].RemoveAt(0);

				if (endsGaps[0].Count == 0)
				{
					endsGaps.RemoveAt(0);
				}

				return endsGaps;
			}

			var result = new List<List<EndsGap>>(endsGaps.Count);

			bool cyclic = endsGap.ThisGroup.Cyclic;

			foreach (List<EndsGap> gaps in endsGaps)
			{
				var reduced = new List<EndsGap>(gaps.Count);

				foreach (EndsGap distance in gaps)
				{
					bool remove;

					if (! cyclic)
					{
						remove = distance.ThisEnd == endsGap.ThisEnd ||
						         distance.OtherEnd == endsGap.ThisEnd ||
						         distance.ThisEnd == endsGap.OtherEnd ||
						         distance.OtherEnd == endsGap.OtherEnd;
					}
					else
					{
						remove = distance.ThisEnd == endsGap.ThisEnd;
					}

					if (! remove)
					{
						reduced.Add(distance);
					}
				}

				if (reduced.Count > 0)
				{
					result.Add(reduced);
				}
			}

			return result;
		}

		private void GroupCompleted([NotNull] RingGrower<DirectedRow> sender,
		                            [NotNull] LineList<DirectedRow> lineList)
		{
			List<DirectedRow> endRows = null;
			Group group = _growerGroupDict[sender];
			List<InvolvedGroupEnds> groupEndsList = _groupGroupendsDict[group];

			int orientation = lineList.Orientation();

			if (orientation > 0 && (_allowedShape & ShapeAllowed.Cycles) == 0)
			{
				// the line list forms a cycle, but cycles are not allowed
				LineList<DirectedRow> errList = Assert.NotNull(lineList.RemoveEnds());

				const string description = "Cycle found";
				_groupCompletedErrorsCount +=
					ReportError(
						description, GetUniqueInvolvedRows(errList.GetUniqueRows(InvolvedTables)),
						errList.GetPolygon(), LocalCodes[Code.InvalidLineGroup_Cycle], null);
			}

			if (orientation > 0 && (_allowedShape & ShapeAllowed.InsideBranches) == 0)
			{
				// The line list forms a cycle, and inside branches are not allowed.
				// The line list should contain no open ends
				endRows = lineList.GetEnds();

				const int maxEnd = 0;
				_groupCompletedErrorsCount +=
					CheckMultipleEnds(endRows, lineList,
					                  LocalCodes[Code.InvalidLineGroup_InsideBranch],
					                  maxEnd);
			}

			if (orientation <= 0 && groupEndsList.Count > 0)
			{
				// the line list does not form a cycle, but it contains disconnected groups
				if (ErrorReporting == GroupErrorReporting.ReferToFirstPart)
				{
					// report error now (otherwise: gather info for later reporting)
					_groupCompletedErrorsCount +=
						ReportMultiplePartsErrorReferToFirstPart(group, groupEndsList[0],
						                                         GetGroupEnds(lineList));
				}
			}
			else if ((_allowedShape & ShapeAllowed.Branches) == 0)
			{
				// branches are not allowed
				if (endRows == null)
				{
					endRows = lineList.GetEnds();
				}

				const int maxEnd = 2;
				_groupCompletedErrorsCount +=
					CheckMultipleEnds(endRows, lineList,
					                  LocalCodes[Code.InvalidLineGroup_Branch], maxEnd);
			}

			if (orientation <= 0)
			{
				// the line list does not form a cycle
				if (groupEndsList.Count == 0 ||
				    ErrorReporting != GroupErrorReporting.ReferToFirstPart)
				{
					// gather info for later reporting
					InvolvedGroupEnds groupEnds = GetGroupEnds(lineList);
					groupEndsList.Add(groupEnds);
				}
			}
		}

		private int CheckMultipleEnds([NotNull] ICollection<DirectedRow> endRows,
		                              [NotNull] LineList<DirectedRow> lineList,
		                              [CanBeNull] IssueCode issueCode,
		                              int maxEnd)
		{
			if (endRows.Count <= maxEnd)
			{
				return NoError;
			}

			object missing = Type.Missing;
			IPointCollection points = null;
			foreach (DirectedRow row1 in endRows)
			{
				if (points == null)
				{
					points = ProxyUtils.CreateMultipoint(row1.ToPoint);
				}

				points.AddPoint(row1.ToPoint, ref missing, ref missing);
			}

			string description = string.Format("Found {0} ends, expected <= {1}",
			                                   endRows.Count, maxEnd);
			return ReportError(
				description, GetUniqueInvolvedRows(lineList.GetUniqueRows(InvolvedTables)),
				(IGeometry) points, issueCode, null);
		}

		[NotNull]
		private InvolvedGroupEnds GetGroupEnds([NotNull] LineList<DirectedRow> lineList)
		{
			List<DirectedRow> ends = lineList.GetEnds();
			if (ends.Count == 0)
			{
				ends.Add(lineList.DirectedRows.First.Value);
			}

			var result = new InvolvedGroupEnds(ends.Select(CreateInvolvedGroupEnd));

			if (ends.Count <= 1)
			{
				result.Cyclic = true;
			}

			// ends.Clear();

			return result;
		}

		[NotNull]
		private static InvolvedGroupEnds GetGroupEnds(
			[NotNull] IEnumerable<ConnectedLine> connectedLines,
			[NotNull] IList<InvolvedRow> involvedRows)
		{
			var result = new InvolvedGroupEnds();

			foreach (ConnectedLine line in connectedLines)
			{
				if (! line.FromConnected)
				{
					result.Add(CreateInvolvedGroupEnd(new FromRow(line), involvedRows));
				}

				if (! line.ToConnected)
				{
					result.Add(CreateInvolvedGroupEnd(new ToRow(line), involvedRows));
				}
			}

			if (result.Count <= 1)
			{
				result.Cyclic = true;
			}

			return result;
		}

		private interface ITableIndexRowPart
		{
			int TableIndex { get; }

			long RowIndex { get; }

			int PartIndex { get; }
		}

		private class ConnectedLineComparer : IEqualityComparer<ITableIndexRowPart>
		{
			private int Compare(ITableIndexRowPart x, ITableIndexRowPart y)
			{
				if (x == y)
				{
					return 0;
				}

				if (x == null)
				{
					return 1;
				}

				if (y == null)
				{
					return -1;
				}

				int d = x.RowIndex.CompareTo(y.RowIndex);
				if (d != 0)
				{
					return d;
				}

				d = x.TableIndex.CompareTo(y.TableIndex);
				if (d != 0)
				{
					return d;
				}

				d = x.PartIndex.CompareTo(y.PartIndex);
				return d;
			}

			public bool Equals(ITableIndexRowPart x, ITableIndexRowPart y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(ITableIndexRowPart obj)
			{
				return obj.RowIndex.GetHashCode();
			}
		}

		private class ConnectedLine : ITableIndexRowPart, ITableIndexRow
		{
			public int TableIndex { get; }

			public long RowIndex => Keys;

			public int PartIndex { get; }

			[CanBeNull]
			public IUniqueIdProvider UniqueIdProvider { get; }

			[NotNull]
			public long Keys { get; }

			public bool Cancelled { get; }

			public int CompareTo([NotNull] ConnectedLine other)
			{
				int d = TableIndex.CompareTo(other.TableIndex);
				if (d != 0)
				{
					return d;
				}

				// Do not use RowIndex first, it is not stable
				if (UniqueIdProvider != null)
				{
					d = Keys.CompareTo(other.Keys);
					return d;
				}

				return RowIndex.CompareTo(other.RowIndex);
			}

			public ConnectedLine(int tableIndex,
			                     long rowKeys,
			                     int partIndex,
			                     [CanBeNull] IUniqueIdProvider uniqueIdProvider,
			                     bool cancelled)
			{
				TableIndex = tableIndex;
				UniqueIdProvider = uniqueIdProvider;
				Keys = rowKeys;
				PartIndex = partIndex;
				Cancelled = cancelled;
			}

			[NotNull]
			public IPoint CreatePoint(bool isBackward)
			{
				return isBackward
					       ? new PointClass { X = ToX, Y = ToY }
					       : new PointClass { X = FromX, Y = FromY };
			}

			public void Reset()
			{
				FromConnected = false;
				ToConnected = false;
				Connected = null;
			}

			[CanBeNull]
			public List<ConnectedLine> Connected { get; private set; }

			public double FromX { get; set; }

			public double FromY { get; set; }

			public bool FromConnected { get; set; }

			public double ToX { get; set; }

			public double ToY { get; set; }

			public bool ToConnected { get; set; }

			long ITableIndexRow.RowOID => RowIndex;

			int ITableIndexRow.TableIndex => TableIndex;

			IReadOnlyRow ITableIndexRow.GetRow(IList<IReadOnlyTable> tableIndexTables)
			{
				return tableIndexTables[TableIndex].GetRow(RowIndex);
			}

			IReadOnlyRow ITableIndexRow.CachedRow => null;

			public override string ToString()
			{
				return $"{FromX:N1},{FromY:N1} -> {ToX:N1},{ToY:N1}";
			}

			public static void Join([NotNull] IEnumerable<NetRow> neighbors)
			{
				List<ConnectedLine> connected = null;

				foreach (NetRow netRow in neighbors)
				{
					ConnectedLine row = netRow.Row;
					if (row.Connected == null)
					{
						connected = connected ?? new List<ConnectedLine>();
						row.Connected = connected;
						connected.Add(row);
					}
					else if (connected == null)
					{
						connected = row.Connected;
					}
					else if (row.Connected != connected)
					{
						List<ConnectedLine> combined;
						List<ConnectedLine> adds;
						if (row.Connected.Count > connected.Count)
						{
							combined = row.Connected;
							adds = connected;
						}
						else
						{
							combined = connected;
							adds = row.Connected;
						}

						combined.AddRange(adds);
						foreach (ConnectedLine add in adds)
						{
							add.Connected = combined;
						}

						connected = combined;
					}
				}
			}
		}

		private class MultipartGroupEnds
		{
			[CanBeNull] private readonly IList<GroupEnd> _firstGroupEnds;
			[NotNull] private readonly IList<GroupEnd> _groupEnds;

			public MultipartGroupEnds([CanBeNull] IList<InvolvedGroupEnd> groupEndRows)
			{
				_groupEnds = new List<GroupEnd>();
				if (groupEndRows != null)
				{
					_firstGroupEnds = new List<GroupEnd>();
					foreach (InvolvedGroupEnd involvedGroupEnd in groupEndRows)
					{
						var groupEnd = new GroupEnd(involvedGroupEnd);
						_firstGroupEnds.Add(groupEnd);
						_groupEnds.Add(groupEnd);
					}
				}
			}

			public bool CheckAll { get; set; }

			public void Add([NotNull] InvolvedGroupEnd groupEnd)
			{
				_groupEnds.Add(new GroupEnd(groupEnd));
			}

			public IEnumerable<GroupEnd> EnumGroupEnds()
			{
				foreach (GroupEnd groupEnd in _groupEnds)
				{
					yield return groupEnd;
				}
			}
		}

		private class GroupEnd : ITableIndexRowPart
		{
			public GroupEnd([NotNull] GroupEnd source)
				: this(source.TableIndex, source.RowIndex, source.PartIndex,
				       source.IsBackward, source.InvolvedRows) { }

			protected GroupEnd(int tableIndex, long rowIndex, int partIndex,
			                   bool isBackward,
			                   [NotNull] IList<InvolvedRow> involvedRows)
			{
				TableIndex = tableIndex;
				RowIndex = rowIndex;
				PartIndex = partIndex;
				IsBackward = isBackward;
				InvolvedRows = involvedRows;
			}

			public int TableIndex { get; }

			public long RowIndex { get; }

			public int PartIndex { get; }

			private bool IsBackward { get; }

			[NotNull]
			public IList<InvolvedRow> InvolvedRows { get; }
		}

		private class InvolvedGroupEnd : GroupEnd
		{
			public InvolvedGroupEnd(int tableIndex, long rowIndex, int partIndex,
			                        bool isBackward,
			                        [NotNull] IList<InvolvedRow> involvedRows,
			                        [NotNull] IPoint endPoint,
			                        [NotNull] IPoint anyPoint)
				: base(tableIndex, rowIndex, partIndex, isBackward, involvedRows)
			{
				EndPoint = endPoint;
				AnyPoint = anyPoint;
			}

			[NotNull]
			public IPoint EndPoint { get; }

			[NotNull]
			public IPoint AnyPoint { get; }

			public long FirstOID => InvolvedRows.FirstOrDefault()?.OID ?? -1;

			public override string ToString()
			{
				var sb = new StringBuilder();
				if (InvolvedRows.Count > 0)
				{
					sb.Append("Involved:");
					foreach (InvolvedRow row in InvolvedRows)
					{
						sb.Append($"{row.OID},");
					}

					sb.Remove(sb.Length - 1, 1);
				}
				else
				{
					sb.Append($"RowOID:{RowIndex}");
				}

				return
					$"{sb}; End: [{EndPoint.X:N0}, {EndPoint.Y:N0}]; Any: [{AnyPoint.X:N0}, {AnyPoint.Y:N0}]";
			}
		}

		private class InvolvedGroupEnds : List<InvolvedGroupEnd>
		{
			public InvolvedGroupEnds() { }

			public InvolvedGroupEnds([NotNull] IEnumerable<InvolvedGroupEnd> collection) :
				base(collection) { }

			public bool Cyclic { get; set; }
		}

		private abstract class NetRow : INetElementXY
		{
			[NotNull]
			public ConnectedLine Row { get; }

			public bool Handled { get; set; }

			protected NetRow([NotNull] ConnectedLine row)
			{
				Row = row;
			}

			double INetElementXY.X => X;

			double INetElementXY.Y => Y;

			public IPoint GetFromPoint()
			{
				return Row.CreatePoint(IsBackward);
			}

			public IPoint GetToPoint()
			{
				return Row.CreatePoint(! IsBackward);
			}

			protected abstract double X { get; }

			protected abstract double Y { get; }

			public abstract bool Connected { get; set; }

			public abstract bool IsBackward { get; }

			public override string ToString()
			{
				return $"{X:N1},{Y:N1}";
			}
		}

		private class FromRow : NetRow
		{
			public FromRow([NotNull] ConnectedLine row) : base(row) { }

			protected override double X => Row.FromX;

			protected override double Y => Row.FromY;

			public override bool IsBackward => false;

			public override bool Connected
			{
				get { return Row.FromConnected; }
				set { Row.FromConnected = value; }
			}

			public override string ToString()
			{
				return $"{base.ToString()} ->";
			}
		}

		private class ToRow : NetRow
		{
			public ToRow([NotNull] ConnectedLine row) : base(row) { }

			protected override double X => Row.ToX;

			protected override double Y => Row.ToY;

			public override bool IsBackward => true;

			public override bool Connected
			{
				get { return Row.ToConnected; }
				set { Row.ToConnected = value; }
			}

			public override string ToString()
			{
				return $"-> {base.ToString()}";
			}
		}
	}
}
