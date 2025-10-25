using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaLineGroupConstraints : QaGroupNetworkBase<NodesDirectedRow>
	{
		private class NodeGroups
		{
			[CanBeNull] private Dictionary<NodesDirectedRow, List<Group>> _rowsGroups;

			[NotNull]
			public Dictionary<NodesDirectedRow, List<Group>> RowsGroups
				=> _rowsGroups ?? (_rowsGroups = GetRowsGroups(Groups));

			[NotNull]
			public List<NodesDirectedRow> ConnectedLines { get; }

			[NotNull]
			public IDictionary<Group, List<NodesDirectedRow>> Groups { get; }

			public NodeGroups([NotNull] List<NodesDirectedRow> connectedLines,
			                  [NotNull] IDictionary<Group, List<NodesDirectedRow>> groups)
			{
				ConnectedLines = connectedLines;
				Groups = groups;
			}

			[NotNull]
			private static Dictionary<NodesDirectedRow, List<Group>> GetRowsGroups(
				[NotNull] IDictionary<Group, List<NodesDirectedRow>> nodeGroups)
			{
				var result = new Dictionary<NodesDirectedRow, List<Group>>();

				foreach (KeyValuePair<Group, List<NodesDirectedRow>> pair in nodeGroups)
				{
					foreach (NodesDirectedRow directedRow in pair.Value)
					{
						List<Group> rowGroups;
						if (! result.TryGetValue(directedRow, out rowGroups))
						{
							rowGroups = new List<Group>();
							result.Add(directedRow, rowGroups);
						}

						rowGroups.Add(pair.Key);
					}
				}

				return result;
			}
		}

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
			public const string GroupTooSmall = "GroupTooSmall";

			public const string Dangle_NotAtFork_Continued =
				"Dangle.NotAtFork+Continued";

			public const string Dangle_AtFork_Continued = "Dangle.AtFork+Continued";

			public const string Dangle_AtFork_Discontinued =
				"Dangle.AtFork+Discontinued";

			public const string Dangle_NotAtFork_Discontinued =
				"Dangle.NotAtFork+Discontinued";

			public const string Gap_ToSameGroupType_AtFork_NotCoveredByOtherGroup =
				"Gap.ToSameGroupType.AtFork+NotCoveredByOtherGroup";

			public const string Gap_ToSameGroupType_AtFork_CoveredByOtherGroup =
				"Gap.ToSameGroupType.AtFork+CoveredByOtherGroup";

			public const string Gap_ToSameGroupType_NotAtFork_NotCoveredByOtherGroup =
				"Gap.ToSameGroupType.NotAtFork+NotCoveredByOtherGroup";

			public const string Gap_ToSameGroupType_NotAtFork_CoveredByOtherGroup =
				"Gap.ToSameGroupType.NotAtFork+CoveredByOtherGroup";

			public const string Gap_ToOtherGroupType_AtFork =
				"Gap.ToOtherGroupType.AtFork";

			public const string Gap_ToOtherGroupType_NotAtFork =
				"Gap.ToOtherGroupType.NotAtFork";

			public Code() : base("LineGroupConstraints") { }
		}

		#endregion

		private readonly double _minGap;
		private readonly double _minGroupLength;
		private readonly double _minDangleLength;

		private int _groupCompletedErrorsCount;

		private IList<IFeatureClassFilter> _endFilters;
		private IList<QueryFilterHelper> _endHelpers;

		[NotNull] private readonly Dictionary<NodesDirectedRow, DangleProperties>
			_dangleProps;

		private double _minGapToOtherGroupType;
		private double _minGapToOtherGroupTypeAtFork;

		private double _minGapToSameGroupTypeCovered;
		private double _minGapToSameGroupTypeAtFork;
		private double _minGapToSameGroupTypeAtForkCovered;
		private double _minGapToSameGroup;

		private double _maxSameGap;
		private double _maxOtherGap;
		private double _maxGap;

		private double _maximumMinDangleLength;
		private double _maximumMinLength;

		private bool _checkDangleConnections;

		private List<NodeGroups> _nodesGroups;

		[NotNull] private readonly Dictionary<Group, NetGrower<NodesDirectedRow>>
			_groupGrowerDict;

		[Doc(nameof(DocStrings.QaLineGroupConstraints_0))]
		public QaLineGroupConstraints(
			[Doc(nameof(DocStrings.QaLineGroupConstraints_networkFeatureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> networkFeatureClasses,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_minGap))]
			double minGap,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_minGroupLength))]
			double minGroupLength,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_minDangleLength))]
			double minDangleLength,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_groupBy))] [NotNull]
			IList<string> groupBy)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) networkFeatureClasses),
			       groupBy)
		{
			Assert.ArgumentCondition(minGap >= 0, "Invalid minGap value: {0}", minGap);
			Assert.ArgumentCondition(minGroupLength >= 0, "Invalid minGroupLength value: {0}",
			                         minGroupLength);
			Assert.ArgumentCondition(minDangleLength >= 0,
			                         "Invalid minDangleLength value: {0}",
			                         minDangleLength);
			Assert.ArgumentNotNull(groupBy, nameof(groupBy));

			_minGap = minGap;
			_minGroupLength = minGroupLength;
			_minDangleLength = minDangleLength;

			SearchDistance = Math.Max(SearchDistance, minGap);

			_dangleProps = new Dictionary<NodesDirectedRow, DangleProperties>(
				new NodesDirectedRow.NodesDirectedRowComparer(
					new DirectedRowComparer(PathRowComparer.RowComparer)));

			var groupByComparer = new GroupByComparer();
			_groupGrowerDict =
				new Dictionary<Group, NetGrower<NodesDirectedRow>>(groupByComparer);
		}

		[InternallyUsedTest]
		public QaLineGroupConstraints(QaLineGroupConstraintsDefinition definition)
			: this(definition.NetworkFeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.MinGap,
			       definition.MinGroupLength, definition.MinDangleLength,definition.GroupBy)
		{
			ValueSeparator = definition.ValueSeparator;
			GroupConditions = definition.GroupConditions;
			MinGapToOtherGroupType = definition.MinGapToOtherGroupType;
			MinDangleLengthContinued = definition.MinDangleLengthContinued;
			MinDangleLengthAtForkContinued = definition.MinDangleLengthAtForkContinued;
			MinDangleLengthAtFork = definition.MinDangleLengthAtForkContinued;
			MinGapToSameGroupTypeCovered = definition.MinGapToSameGroupTypeCovered;
			MinGapToSameGroupTypeAtFork = definition.MinGapToSameGroupTypeAtFork;
			MinGapToSameGroupTypeAtForkCovered = definition.MinGapToSameGroupTypeAtForkCovered;
			MinGapToOtherGroupTypeAtFork = definition.MinGapToOtherGroupTypeAtFork;
			MinGapToSameGroup = definition.MinGapToSameGroupTypeAtFork;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_ValueSeparator))]
		public string ValueSeparator
		{
			get { return ValueSeparatorBase; }
			set { ValueSeparatorBase = value; }
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_GroupConditions))]
		public IList<string> GroupConditions
		{
			get { return GroupConditionsBase; }
			set { GroupConditionsBase = value; }
		}

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToOtherGroupType))]
		public double MinGapToOtherGroupType
		{
			get { return _minGapToOtherGroupType; }
			set
			{
				_minGapToOtherGroupType = value;
				SearchDistance = Math.Max(SearchDistance, value);
			}
		}

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinDangleLengthContinued))]
		public double MinDangleLengthContinued { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinDangleLengthAtForkContinued))]
		public double MinDangleLengthAtForkContinued { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinDangleLengthAtFork))]
		public double MinDangleLengthAtFork { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroupTypeCovered))]
		public double MinGapToSameGroupTypeCovered
		{
			get { return _minGapToSameGroupTypeCovered; }
			set
			{
				_minGapToSameGroupTypeCovered = value;
				SearchDistance = Math.Max(SearchDistance, value);
			}
		}

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroupTypeAtFork))]
		public double MinGapToSameGroupTypeAtFork
		{
			get { return _minGapToSameGroupTypeAtFork; }
			set
			{
				_minGapToSameGroupTypeAtFork = value;
				SearchDistance = Math.Max(SearchDistance, value);
			}
		}

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroupTypeAtForkCovered))]
		public double MinGapToSameGroupTypeAtForkCovered
		{
			get { return _minGapToSameGroupTypeAtForkCovered; }
			set
			{
				_minGapToSameGroupTypeAtForkCovered = value;
				SearchDistance = Math.Max(SearchDistance, value);
			}
		}

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToOtherGroupTypeAtFork))]
		public double MinGapToOtherGroupTypeAtFork
		{
			get { return _minGapToOtherGroupTypeAtFork; }
			set
			{
				_minGapToOtherGroupTypeAtFork = value;
				SearchDistance = Math.Max(SearchDistance, value);
			}
		}

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroup))]
		public double MinGapToSameGroup
		{
			get { return _minGapToSameGroup; }
			set
			{
				_minGapToSameGroup = value;
				SearchDistance = Math.Max(SearchDistance, value);
			}
		}

		protected override NodesDirectedRow ConvertRow(DirectedRow row)
		{
			return new NodesDirectedRow(row.TopologicalLine,
			                            row.TopologicalLine.Row,
			                            row.IsBackward);
		}

		protected override NodesDirectedRow Reverse(NodesDirectedRow row)
		{
			return row.Reverse();
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			_groupCompletedErrorsCount = 0;

			int errorCount = base.CompleteTileCore(args);

			if (args.State == TileState.Initial)
			{
				_maximumMinDangleLength =
					Math.Max(_minDangleLength,
					         Math.Max(MinDangleLengthAtFork,
					                  Math.Max(MinDangleLengthContinued,
					                           MinDangleLengthAtForkContinued)
					         ));

				if (_maximumMinDangleLength > 0)
				{
					_checkDangleConnections = true;
				}

				_maximumMinLength = Math.Max(_maximumMinDangleLength, _minGroupLength);

				_maxOtherGap = Math.Max(_minGapToOtherGroupTypeAtFork, _minGapToOtherGroupType);

				_maxSameGap = Math.Max(_minGapToSameGroupTypeCovered,
				                       _minGapToSameGroupTypeAtForkCovered);
				_maxSameGap = Math.Max(_maxSameGap, _minGapToSameGroupTypeAtFork);
				_maxSameGap = Math.Max(_maxSameGap, _minGap);

				_maxGap = Math.Max(_maxSameGap, _maxOtherGap);

				_dangleProps.Clear();
				_groupGrowerDict.Clear();

				return errorCount;
			}

			_nodesGroups = new List<NodeGroups>();
			errorCount += ResolveNodes();

			InitEndFilters();
			errorCount += ReportAlongNetMinGapDistanceErrors(_nodesGroups);

			if (args.State != TileState.Final)
			{
				errorCount += ReduceNets();

				return errorCount + _groupCompletedErrorsCount;
			}

			if (args.AllBox != null)
			{
				errorCount += GetIncompleteLeafAndNearErrors((IRelationalOperator) args.AllBox);
			}

			return errorCount + _groupCompletedErrorsCount;
		}

		private int ReduceNets()
		{
			var errorCount = 0;
			foreach (KeyValuePair<Group, NetGrower<NodesDirectedRow>> pair in _groupGrowerDict)
			{
				NetGrower<NodesDirectedRow> netGrower = pair.Value;

				var nets = new List<List<NodesDirectedRow>>(netGrower.GetNets());
				foreach (List<NodesDirectedRow> net in nets)
				{
					double sumLength = 0;
					foreach (NodesDirectedRow directedRow in net)
					{
						sumLength += directedRow.TopoLine.Length;
					}

					if (sumLength < _minGroupLength)
					{
						continue;
					}

					List<NodesDirectedRow> endRows = netGrower.GetEndNodes(net);
					errorCount += CheckDangles(endRows);

					List<NodesDirectedRow> incomplete = null;

					foreach (NodesDirectedRow directedRow in net)
					{
						if (directedRow.ToNode == null)
						{
							bool isComplete;
							List<NodesDirectedRow> dangleRows =
								GetDangle(directedRow, out isComplete);
							if (! isComplete && dangleRows.Count > 1)
							{
								incomplete = dangleRows;
								continue;
							}

							ReplaceEnd(netGrower, directedRow, dangleRows, sumLength,
							           bothEndsIncomplete: false);
						}
					}

					if (incomplete != null)
					{
						ReplaceEnd(netGrower, incomplete[0], incomplete, sumLength,
						           bothEndsIncomplete: true);
					}

					net.Clear();
					netGrower.Remove(net);
				}
			}

			return errorCount;
		}

		private void ReplaceEnd(
			NetGrower<NodesDirectedRow> netGrower, NodesDirectedRow incompleteRow,
			List<NodesDirectedRow> dangleRows, double sumLength, bool bothEndsIncomplete)
		{
			double dangleLength = 0;
			foreach (NodesDirectedRow dangleRow in dangleRows)
			{
				dangleLength += dangleRow.TopoLine.Length;
			}

			bool keepRows = dangleLength <= _maximumMinDangleLength;

			NodesDirectedRow preRow = incompleteRow;
			NetNode preNode = incompleteRow.FromNode;

			netGrower.RemoveEnd(incompleteRow, removeInList: false);
			if (bothEndsIncomplete)
			{
				netGrower.RemoveEnd(dangleRows[dangleRows.Count - 1], removeInList: false);
			}

			double restLength;
			if (keepRows)
			{
				for (var i = 1; i < dangleRows.Count; i++)
				{
					NodesDirectedRow dangleRow;
					NodesDirectedRow existing = dangleRows[i];
					if (existing.FromNode == preNode)
					{
						preNode = existing.ToNode;
						dangleRow = new NodesDirectedRow(existing.TopoLine, existing.Row,
						                                 existing.IsBackward);
					}
					else
					{
						preNode = existing.ToNode;
						dangleRow = new NodesDirectedRow(existing.TopoLine, existing.Row,
						                                 ! existing.IsBackward);
					}

					var node = new NetNode<NodesDirectedRow>(new List<NodesDirectedRow>
					                                         {preRow, dangleRow});
					netGrower.AddNode(node);

					preRow = dangleRow.Reverse();
				}

				restLength = sumLength - dangleLength;
			}
			else
			{
				if (bothEndsIncomplete)
				{
					NodesDirectedRow otherEnd = dangleRows[dangleRows.Count - 1];
					if (dangleRows.Count > 2)
					{
						IPolyline simpleDangleLine = GeometryFactory.CreatePolyline(
							preRow.FromPoint,
							otherEnd.FromPoint);
						var simpleDangleTopo = new SimpleTopoLine(-1, simpleDangleLine,
							dangleLength -
							preRow.TopoLine.Length -
							otherEnd.TopoLine.Length,
							preRow.FromAngle + 1,
							otherEnd.FromAngle - 1);
						SimpleTableIndexRow simpleDangleRow =
							SimpleTableIndexRow.Create(dangleRows[1].Row);

						var simpleNodesRow = new NodesDirectedRow(simpleDangleTopo,
							simpleDangleRow, false);

						var dangleNode =
							new NetNode<NodesDirectedRow>(new List<NodesDirectedRow>
							                              {
								                              preRow,
								                              simpleNodesRow
							                              });
						netGrower.AddNode(dangleNode);
						preRow = simpleNodesRow.Reverse();
					}

					var otherEndNode =
						new NetNode<NodesDirectedRow>(new List<NodesDirectedRow>
						                              {
							                              preRow,
							                              otherEnd
						                              });
					netGrower.AddNode(otherEndNode);
				}
				else if (dangleRows.Count > 1)
				{
					IPolyline simpleDangleLine = GeometryFactory.CreatePolyline(preRow.FromPoint,
						preRow.FromPoint);
					var simpleDangleTopo = new SimpleTopoLine(-1, simpleDangleLine,
					                                          dangleLength -
					                                          preRow.TopoLine.Length,
					                                          preRow.FromAngle + 1,
					                                          preRow.FromAngle - 1);
					SimpleTableIndexRow simpleDangleRow =
						SimpleTableIndexRow.Create(dangleRows[1].Row);

					var simpleNodesRow = new NodesDirectedRow(simpleDangleTopo,
					                                          simpleDangleRow, false);

					var dangleNode =
						new NetNode<NodesDirectedRow>(new List<NodesDirectedRow>
						                              {
							                              preRow,
							                              simpleNodesRow
						                              });
					netGrower.AddNode(dangleNode);
					preRow = simpleNodesRow.Reverse();
				}

				restLength = Math.Max(0, sumLength - dangleLength);
			}

			if (! bothEndsIncomplete)
			{
				IPolyline simpleLine = GeometryFactory.CreatePolyline(preRow.FromPoint,
					preRow.FromPoint);
				var simpleTopoLine = new SimpleTopoLine(-1, simpleLine, restLength,
				                                        preRow.FromAngle + 1,
				                                        preRow.FromAngle - 1);
				var simpleRow = new SimpleTableIndexRow(0, -1);

				var simpleLarge = new NodesDirectedRow(simpleTopoLine, simpleRow,
				                                       false);
				var lastNode =
					new NetNode<NodesDirectedRow>(new List<NodesDirectedRow>
					                              {
						                              preRow,
						                              simpleLarge,
						                              simpleLarge.Reverse()
					                              });
				netGrower.AddNode(lastNode);
			}
		}

		protected override int OnNodeAssembled(List<NodesDirectedRow> directedRows,
		                                       Dictionary<Group, List<NodesDirectedRow>>
			                                       groupDict)
		{
			var errorCount = 0;

			NodeAssembled(_nodesGroups, directedRows, groupDict, _checkDangleConnections);

			foreach (KeyValuePair<Group, List<NodesDirectedRow>> pair in groupDict)
			{
				Group group = pair.Key;
				List<NodesDirectedRow> groupRows = pair.Value;

				NetGrower<NodesDirectedRow> grower;
				if (! _groupGrowerDict.TryGetValue(group, out grower))
				{
					grower = new NetGrower<NodesDirectedRow>();
					grower.GeometryCompleted += GroupCompleted;

					_groupGrowerDict.Add(group, grower);
				}

				errorCount += AddGroups(group, groupRows, grower);
			}

			var toRemoves = new List<Group>();
			foreach (KeyValuePair<Group, NetGrower<NodesDirectedRow>> pair in _groupGrowerDict)
			{
				NetGrower<NodesDirectedRow> grower = pair.Value;
				if (grower.NetsCount == 0)
				{
					grower.GeometryCompleted -= GroupCompleted;
					toRemoves.Add(pair.Key);
				}
			}

			foreach (Group toRemove in toRemoves)
			{
				_groupGrowerDict.Remove(toRemove);
			}

			return errorCount;
		}

		private int AddGroups([NotNull] Group group,
		                      [NotNull] IList<NodesDirectedRow> directedRows,
		                      [NotNull] NetGrower<NodesDirectedRow> grower)
		{
			int errorCount = ReportAddGroupsErrors(group, directedRows);

			int lineCount = directedRows.Count;

			if (lineCount > 0)
			{
				var node = new NetNode<NodesDirectedRow>(directedRows);
				grower.AddNode(node);
			}

			return errorCount;
		}

		private void NodeAssembled([NotNull] IList<NodeGroups> nodesGroups,
		                           [NotNull] List<NodesDirectedRow> connectedRows,
		                           [NotNull] IDictionary<Group, List<NodesDirectedRow>>
			                           groupDict,
		                           bool checkDangleConnections)
		{
			var nodeGroups = new NodeGroups(connectedRows, groupDict);
			nodesGroups.Add(nodeGroups);

			if (checkDangleConnections)
			{
				if (nodeGroups.ConnectedLines.Count == 1)
				{
					// there is no continuation
					return;
				}

				bool isForked = IsForked(nodeGroups.ConnectedLines);
				bool isContinued = IsContinued(nodeGroups);

				foreach (List<NodesDirectedRow> groupRows in nodeGroups.Groups.Values)
				{
					if (IsGroupEnd(groupRows))
					{
						NodesDirectedRow row = groupRows[0];
						ITopologicalLine line = row.TopoLine;
						IPolyline simpleLine = GeometryFactory.CreatePolyline(line.FromPoint,
							line.ToPoint);
						var simpleTopoLine = new SimpleTopoLine(
							line.PartIndex, simpleLine, line.Length,
							line.FromAngle, line.ToAngle);
						SimpleTableIndexRow simpleRow = SimpleTableIndexRow.Create(row.Row);

						var simpleDangle = new NodesDirectedRow(simpleTopoLine, simpleRow,
						                                        row.IsBackward);
						_dangleProps[simpleDangle] = new DangleProperties
						                             {
							                             IsContinued = isContinued,
							                             IsForked = isForked
						                             };
					}
				}
			}
		}

		private int ReportAlongNetMinGapDistanceErrors(
			[NotNull] IList<NodeGroups> nodesGroups)
		{
			if (_maxGap <= 0)
			{
				return NoError;
			}

			var rowNodeDict = new RowNodeDict(nodesGroups);

			var errorCount = 0;
			foreach (NodeGroups nodeGroups in nodesGroups)
			{
				errorCount += CheckNode(nodeGroups, rowNodeDict);
			}

			return errorCount;
		}

		private int CheckNode([NotNull] NodeGroups nodeGroups,
		                      [NotNull] RowNodeDict rowNodeDict)
		{
			if (nodeGroups.ConnectedLines.Count == 1)
			{
				// there is no continuation
				return NoError;
			}

			bool isContinued = IsContinued(nodeGroups);

			if (isContinued && _maxSameGap <= 0)
			{
				// this node is OK, skip further evalation
				return NoError;
			}

			bool isForked = IsForked(nodeGroups.ConnectedLines);

			var errorCount = 0;
			foreach (KeyValuePair<Group, List<NodesDirectedRow>> pair in nodeGroups.Groups)
			{
				errorCount += CheckNodeGroup(pair.Key, pair.Value, nodeGroups, rowNodeDict,
				                             isContinued, isForked);
			}

			return errorCount;
		}

		private int CheckNodeGroup([NotNull] Group group,
		                           [NotNull] IList<NodesDirectedRow> rows,
		                           [NotNull] NodeGroups nodeGroups,
		                           [NotNull] RowNodeDict rowNodeDict,
		                           bool isContinued,
		                           bool isForked)
		{
			bool isEnd = IsGroupEnd(rows);
			if (! isEnd)
			{
				return NoError;
			}

			double minGapOther = _maxOtherGap;

			if (isContinued)
			{
				minGapOther = 0;
			}

			double minGap = _maxGap;
			double minGapSame = _maxSameGap;

			NodesDirectedRow endingRow = rows[0];
			var endValidator = new EndValidator(
				endingRow, nodeGroups, group, rowNodeDict,
				minGap, minGapOther, minGapSame,
				GetNodeGroups, isForked);

			NearError error = null;
			foreach (NearError errorCandidate in endValidator.GetErrors())
			{
				if (errorCandidate.Near == null)
				{
					// TODO: add for handling in later tile
					continue;
				}

				NearError nearError = CompleteErrorInfo(errorCandidate);
				if (nearError == null)
				{
					continue;
				}

				if (error == null)
				{
					error = nearError;
				}
				else
				{
					// TODO revise, error description can get very large

					if (nearError.Offset < error.Offset)
					{
						error.Offset = nearError.Offset;
						error.Geometry = nearError.Geometry;
						error.Description = nearError.Description;
						//error.Description = nearError.Description + Environment.NewLine +
						//                    error.Description;
					}
					//else
					//{
					//	error.Description = error.Description + Environment.NewLine +
					//	                    nearError.Description;
					//}

					error.DifferentGroup &= nearError.DifferentGroup;
				}
			}

			return error == null
				       ? NoError
				       : ReportError(error);
		}

		private static bool IsApplicable([NotNull] NearError error,
		                                 double gap,
		                                 bool preCondition,
		                                 out bool isValid,
		                                 out double gapCopy)
		{
			gapCopy = gap;
			isValid = false;
			if (gap <= 0 || ! preCondition)
			{
				return false;
			}

			isValid = error.Offset >= gap;
			return true;
		}

		[CanBeNull]
		private NearError CompleteErrorInfo([NotNull] NearError error)
		{
			double minDistance;
			double gapCopy;
			bool isValid;
			string description;
			IssueCode issueCode;
			if (error.DifferentGroup)
			{
				if (IsApplicable(error, MinGapToOtherGroupTypeAtFork, error.IsAtFork,
				                 out isValid,
				                 out gapCopy))
				{
					if (isValid)
					{
						return null;
					}

					minDistance = gapCopy;
				}
				else if (IsApplicable(error, MinGapToOtherGroupType, true,
				                      out isValid,
				                      out gapCopy))
				{
					if (isValid)
					{
						return null;
					}

					minDistance = gapCopy;
				}
				else
				{
					return null;
				}

				if (error.IsAtFork)
				{
					issueCode = LocalCodes[Code.Gap_ToOtherGroupType_AtFork];
					description = "Small gap (starting at a fork)";
				}
				else
				{
					issueCode = LocalCodes[Code.Gap_ToOtherGroupType_NotAtFork];
					description = "Small gap";
				}
			}
			else
			{
				// gap to group of same type
				if (IsApplicable(error, MinGapToSameGroupTypeAtForkCovered,
				                 error.IsAtFork && error.IsCovered,
				                 out isValid,
				                 out gapCopy))
				{
					if (isValid)
					{
						return null;
					}

					minDistance = gapCopy;
				}
				else if (IsApplicable(error, MinGapToSameGroupTypeAtFork,
				                      error.IsAtFork,
				                      out isValid,
				                      out gapCopy))
				{
					if (isValid)
					{
						return null;
					}

					minDistance = gapCopy;
				}
				else if (IsApplicable(error, MinGapToSameGroupTypeCovered,
				                      error.IsCovered,
				                      out isValid,
				                      out gapCopy))
				{
					if (isValid)
					{
						return null;
					}

					minDistance = gapCopy;
				}
				else
				{
					if (error.Offset >= _minGap)
					{
						return null;
					}

					minDistance = _minGap;
				}

				if (error.IsAtFork)
				{
					if (error.IsCovered)
					{
						issueCode = LocalCodes[Code.Gap_ToSameGroupType_AtFork_CoveredByOtherGroup];
						description = "Small gap (starting at a fork, covered by other group)";
					}
					else
					{
						issueCode =
							LocalCodes[Code.Gap_ToSameGroupType_AtFork_NotCoveredByOtherGroup];
						description = "Small gap (starting at a fork)";
					}
				}
				else
				{
					if (error.IsCovered)
					{
						issueCode =
							LocalCodes[Code.Gap_ToSameGroupType_NotAtFork_CoveredByOtherGroup];
						description = "Small gap (covered by other group)";
					}
					else
					{
						issueCode =
							LocalCodes[Code.Gap_ToSameGroupType_NotAtFork_NotCoveredByOtherGroup];
						description = "Small gap";
					}
				}
			}

			error.Description = GetGapIssueDescription(error,
			                                           description,
			                                           minDistance);
			error.IssueCode = issueCode;

			return error;
		}

		[NotNull]
		private string GetGapIssueDescription([NotNull] NearError error,
		                                      [NotNull] string baseDescription,
		                                      double minDistance)
		{
			ISpatialReference spatialReference =
				error.DirectedRow.TopoLine.GetPath().SpatialReference;

			if (! error.DifferentGroup)
			{
				return string.Format(
					"{0} from end of group {1} to other group of same type is {2}",
					baseDescription,
					error.Group.GetInfo(GroupBys, true),
					FormatLengthComparison(
						error.Offset, "<", minDistance, spatialReference).Trim()
				);
			}

			return string.Format(
				"{0} from end of group {1} to group of type {2} is {3}",
				baseDescription,
				error.Group.GetInfo(GroupBys, true),
				error.OtherGroup.GetInfo(GroupBys, true),
				FormatLengthComparison(
					error.Offset, "<", minDistance, spatialReference).Trim()
			);
		}

		private static bool IsGroupEnd([NotNull] IList<NodesDirectedRow> rows)
		{
			if (rows.Count == 0)
			{
				return false;
			}

			if (rows.Count != 1)
			{
				NodesDirectedRow pre = null;
				var multiAngle = false;
				foreach (NodesDirectedRow row in rows)
				{
					if (pre != null && Math.Abs(row.FromAngle - pre.FromAngle) > 0)
					{
						multiAngle = true;
						break;
					}

					pre = row;
				}

				if (multiAngle)
				{
					// this is no group end
					return false;
				}
			}

			return true;
		}

		private static bool IsForked([NotNull] IEnumerable<NodesDirectedRow> nodeRows)
		{
			var angleRows = new Dictionary<double, List<NodesDirectedRow>>();

			foreach (NodesDirectedRow row in nodeRows)
			{
				List<NodesDirectedRow> rows;
				if (! angleRows.TryGetValue(row.FromAngle, out rows))
				{
					rows = new List<NodesDirectedRow>();
					angleRows.Add(row.FromAngle, rows);
				}

				rows.Add(row);
			}

			return angleRows.Count > 2;
		}

		private static bool IsContinued([NotNull] NodeGroups nodeGroups)
		{
			double angle0 = double.NaN;

			foreach (List<NodesDirectedRow> groupRows in nodeGroups.Groups.Values)
			{
				foreach (NodesDirectedRow groupRow in groupRows)
				{
					if (double.IsNaN(angle0))
					{
						angle0 = groupRow.FromAngle;
					}
					else if (Math.Abs(angle0 - groupRow.FromAngle) > 0)
					{
						return true;
					}
				}
			}

			return false;
		}

		[CanBeNull]
		private NodeGroups GetNodeGroups([NotNull] NodesDirectedRow dirRow)
		{
			double tolerance = Tolerance;
			var connectedList = new List<DirectedRow>();

			var p = (IProximityOperator) dirRow.FromPoint;

			IEnvelope search = dirRow.FromPoint.Envelope;
			search.Expand(tolerance, tolerance, false);

			for (var tableIndex = 0; tableIndex < InvolvedTables.Count; tableIndex++)
			{
				if (NonNetworkClassIndexList.Contains(tableIndex))
				{
					continue;
				}

				IFeatureClassFilter filter = _endFilters[tableIndex];
				filter.FilterGeometry = search;

				foreach (IReadOnlyRow row in Search(InvolvedTables[tableIndex],
				                                    filter,
				                                    _endHelpers[tableIndex]))
				{
					foreach (
						DirectedRow directedRow in GetDirectedRows(
							new TableIndexRow(row, tableIndex)))
					{
						if (p.ReturnDistance(directedRow.FromPoint) < tolerance)
						{
							connectedList.Add(directedRow);
						}

						if (p.ReturnDistance(directedRow.ToPoint) < tolerance)
						{
							connectedList.Add(directedRow.Reverse());
						}
					}
				}
			}

			if (connectedList.Count == 1)
			{
				return null;
			}

			var groups = new Dictionary<Group, List<NodesDirectedRow>>(new GroupByComparer());

			List<NodesDirectedRow> converted;
			FillGroupDict(connectedList, groups, out converted, reportErrors: false);

			var node = new NodeGroups(converted, groups);
			return node;
		}

		private class RowNodeDict
		{
			[NotNull] private readonly IList<NodeGroups> _nodeRows;

			[CanBeNull] private Dictionary<NodesDirectedRow, NodeGroups> _rowNodeDict;

			public RowNodeDict([NotNull] IList<NodeGroups> nodeRows)
			{
				_nodeRows = nodeRows;
			}

			[CanBeNull]
			public NodeGroups GetNode([NotNull] NodesDirectedRow row)
			{
				if (_rowNodeDict == null)
				{
					var rowNodeDict =
						new Dictionary<NodesDirectedRow, NodeGroups>(
							new NodesDirectedRow.NodesDirectedRowComparer(
								new DirectedRowComparer(new TableIndexRowComparer())));

					foreach (NodeGroups rows in _nodeRows)
					{
						foreach (NodesDirectedRow connected in rows.ConnectedLines)
						{
							rowNodeDict[connected] = rows;
						}
					}

					_rowNodeDict = rowNodeDict;
				}

				NodeGroups node;
				return _rowNodeDict.TryGetValue(row, out node)
					       ? node
					       : null;
			}
		}

		private class EndValidator
		{
			[NotNull] private readonly NodesDirectedRow _startRow;
			[NotNull] private readonly NodeGroups _startNode;
			[NotNull] private readonly Group _group;
			[NotNull] private readonly RowNodeDict _rowNodeDict;
			private readonly double _minGapDistance;
			private readonly double _minGapDistanceOther;
			private readonly double _minGapDistanceSameGroup;

			[NotNull] private readonly GroupByComparer _groupComparer;
			[NotNull] private readonly PathRowComparer _pathRowComparer;
			[NotNull] private readonly Func<NodesDirectedRow, NodeGroups> _getNodeGroups;

			private readonly bool _isAtFork;

			public EndValidator([NotNull] NodesDirectedRow startRow,
			                    [NotNull] NodeGroups startNode,
			                    [NotNull] Group group,
			                    [NotNull] RowNodeDict rowNodeDict,
			                    double minGapDistance, double minGapDistanceOther,
			                    double minGapDistanceSameGroup,
			                    [NotNull] Func<NodesDirectedRow, NodeGroups> getNodeGroups,
			                    bool isAtFork)
			{
				_startRow = startRow;
				_startNode = startNode;
				_group = group;
				_rowNodeDict = rowNodeDict;
				_minGapDistance = minGapDistance;
				_minGapDistanceOther = minGapDistanceOther;
				_minGapDistanceSameGroup = minGapDistanceSameGroup;

				_getNodeGroups = getNodeGroups;

				_isAtFork = isAtFork;

				_groupComparer = new GroupByComparer();
				_pathRowComparer = new PathRowComparer(new TableIndexRowComparer());
			}

			[CanBeNull]
			private NodeGroups GetNextNode([NotNull] NodesDirectedRow row, out int errorCount)
			{
				errorCount = 0;

				NodeGroups node = _rowNodeDict.GetNode(row) ?? _getNodeGroups(row);

				return node;
			}

			public IEnumerable<NearError> GetErrors()
			{
				var preNodes = new Dictionary<NodeGroups, object> {{_startNode, 1}};

				foreach (NearError errorInfo in
				         GetErrors(_startRow, preNodes, _startNode, 0, _minGapDistance,
				                   _minGapDistanceOther, new List<IDirectedRow>(), _isAtFork,
				                   isAllCovered: true))
				{
					yield return errorInfo;
				}
			}

			private IEnumerable<NearError> GetErrors(
				[NotNull] IDirectedRow row,
				[NotNull] IDictionary<NodeGroups, object> preNodes,
				[NotNull] NodeGroups node,
				double sumLength,
				double minEndDistance,
				double minEndDistanceOther,
				[NotNull] IList<IDirectedRow> alongNet,
				bool isAtFork,
				bool isAllCovered)
			{
				if (sumLength >= minEndDistance && sumLength >= minEndDistanceOther)
				{
					yield break;
				}

				foreach (NodesDirectedRow connected in node.ConnectedLines)
				{
					if (_pathRowComparer.Equals(connected, row))
					{
						continue;
					}

					double length = connected.TopoLine.Length;
					double fullLength = sumLength + length;
					if (fullLength >= minEndDistance && fullLength >= minEndDistanceOther)
					{
						continue;
					}

					var isCovered = false;
					List<Group> rowGroups;
					if (isAllCovered && node.RowsGroups.TryGetValue(connected, out rowGroups) &&
					    rowGroups.Count > 0)
					{
						isCovered = true;
					}

					var inverted = new NodesDirectedRow(connected.TopoLine, connected.Row,
					                                    ! connected.IsBackward)
					               {
						               FromNode = connected.ToNode,
						               ToNode = connected.FromNode
					               };

					NodeGroups nextNode = GetNextNode(inverted, out int _);

					if (nextNode == null)
					{
						continue;
					}

					double nextMinEndDistance = minEndDistance;
					double nextMinEndDistanceOther = minEndDistanceOther;

					foreach (KeyValuePair<Group, List<NodesDirectedRow>> pair in nextNode.Groups)
					{
						Group group = pair.Key;
						if (_groupComparer.Equals(_group, group))
						{
							if (fullLength < nextMinEndDistance)
							{
								double sameGroupLength = ConnectedLineLength(nextNode);

								if (fullLength < sameGroupLength &&
								    sameGroupLength > nextMinEndDistance &&
								    _minGapDistanceSameGroup > 0 &&
								    fullLength < _minGapDistanceSameGroup)
								{
									NodesDirectedRow neighbor = pair.Value[0];
									yield return new NearError
									             {
										             Geometry = GetGeometry(alongNet, connected),
										             DirectedRow = _startRow,
										             Group = _group,
										             Near = neighbor.Row,
										             DifferentGroup = false,
										             Offset = fullLength,
										             IsAtFork = isAtFork,
										             IsCovered = isAllCovered && isCovered
									             };
								}
							}

							nextMinEndDistance = 0;
						}
						else
						{
							if (fullLength < nextMinEndDistanceOther)
							{
								NodesDirectedRow neighbor = pair.Value[0];
								yield return new NearError
								             {
									             Geometry = GetGeometry(alongNet, connected),
									             DirectedRow = _startRow,
									             Group = _group,
									             Near = neighbor.Row,
									             OtherGroup = group,
									             DifferentGroup = true,
									             Offset = fullLength,
									             IsAtFork = isAtFork,
									             IsCovered = isAllCovered && isCovered
								             };
							}

							nextMinEndDistanceOther = 0;
						}
					}

					if (nextMinEndDistance <= 0 && nextMinEndDistanceOther <= 0)
					{
						continue;
					}

					if (preNodes.ContainsKey(nextNode))
					{
						continue;
					}

					var pre = new Dictionary<NodeGroups, object>(preNodes)
					          {
						          {nextNode, 1}
					          };

					var nextAlong = new List<IDirectedRow>(alongNet);
					nextAlong.Add(connected);

					foreach (NearError errorInfo in GetErrors(
						         inverted, pre, nextNode, fullLength, nextMinEndDistance,
						         nextMinEndDistanceOther, nextAlong, isAtFork,
						         isAllCovered && isCovered))
					{
						yield return errorInfo;
					}
				}
			}

			private Dictionary<NodeGroups, double> _connectedNodes;
			private bool _connectedNodesBuilt;

			private double ConnectedLineLength([NotNull] NodeGroups endNode)
			{
				_connectedNodes = _connectedNodes ??
				                  new Dictionary<NodeGroups, double> {{_startNode, 0}};

				double connectedLength;
				if (_connectedNodes.TryGetValue(endNode, out connectedLength))
				{
					return connectedLength;
				}

				if (_connectedNodesBuilt)
				{
					return double.MaxValue;
				}

				BuildConnectedNodes();
				_connectedNodesBuilt = true;

				if (_connectedNodes.TryGetValue(endNode, out connectedLength))
				{
					return connectedLength;
				}

				return double.MaxValue;
			}

			private void BuildConnectedNodes()
			{
				var unhandledNodes = new List<NodeGroups>(_connectedNodes.Keys);
				while (unhandledNodes.Count > 0)
				{
					double minLength = double.MaxValue;
					NodeGroups nodeToHandle = null;
					foreach (NodeGroups unhandledNode in unhandledNodes)
					{
						double unhandledLength = _connectedNodes[unhandledNode];
						if (unhandledLength < minLength)
						{
							minLength = unhandledLength;
							nodeToHandle = unhandledNode;
						}
					}

					unhandledNodes.Remove(Assert.NotNull(nodeToHandle));

					double length = minLength;
					if (length > _minGapDistance)
					{
						continue;
					}

					foreach (
						KeyValuePair<Group, List<NodesDirectedRow>> groupPair in nodeToHandle.Groups
					)
					{
						if (! _groupComparer.Equals(groupPair.Key, _group))
						{
							continue;
						}

						foreach (NodesDirectedRow connected in groupPair.Value)
						{
							var inverted = new NodesDirectedRow(connected.TopoLine, connected.Row,
							                                    ! connected.IsBackward)
							               {
								               FromNode = connected.ToNode,
								               ToNode = connected.FromNode
							               };

							NodeGroups nextNode = GetNextNode(inverted, out int _);

							if (nextNode == null)
							{
								continue;
							}

							double nextLength = length + connected.TopoLine.Length;

							double existingLength;
							if (_connectedNodes.TryGetValue(nextNode, out existingLength))
							{
								if (existingLength > nextLength)
								{
									_connectedNodes[nextNode] = nextLength;
								}
							}
							else
							{
								_connectedNodes[nextNode] = nextLength;
								unhandledNodes.Add(nextNode);
							}
						}
					}
				}
			}

			[NotNull]
			private static IPolyline GetGeometry([NotNull] IList<IDirectedRow> start,
			                                     [NotNull] NodesDirectedRow append)
			{
				if (start.Count == 0)
				{
					return append.TopoLine.GetLine();
				}

				List<IPolyline> allLines = start.Select(x => x.TopoLine.GetLine()).ToList();
				allLines.Add(append.TopoLine.GetLine());

				// ReSharper disable once RedundantEnumerableCastCall
				IGeometry combined =
					GeometryFactory.CreateUnion(allLines.Cast<IGeometry>(),
					                            expansionDistance: 0);
				return (IPolyline) combined;
			}
		}

		private void GroupCompleted(
			object sender,
			[NotNull] NetGrower<NodesDirectedRow>.GeometryCompleteEventArgs args)
		{
			NetGrower<NodesDirectedRow> net = args.Net;
			List<NodesDirectedRow> netRows = args.NetRows;
			List<NodesDirectedRow> endRows = net.GetEndNodes(netRows);

			_groupCompletedErrorsCount += CheckDangles(endRows);

			_groupCompletedErrorsCount += CheckGroupLength(netRows);
		}

		private int CheckDangles([NotNull] IEnumerable<NodesDirectedRow> endRows)
		{
			var errorCount = 0;

			foreach (NodesDirectedRow endRow in endRows)
			{
				if (endRow.FromNode.RowsCount == 1)
				{
					errorCount += CheckDangle(endRow);
				}

				if (endRow.ToNode != null && endRow.ToNode.RowsCount == 1)
				{
					errorCount += CheckDangle(endRow.Reverse());
				}
			}

			return errorCount;
		}

		protected override int ReportAddGroupsErrors(Group group,
		                                             IList<NodesDirectedRow> rows)
		{
			// reported in CompleteTileCore
			return NoError;
		}

		private int GetIncompleteLeafAndNearErrors([NotNull] IRelationalOperator allBox)
		{
			var errorCount = 0;

			foreach (NetGrower<NodesDirectedRow> grower in _groupGrowerDict.Values)
			{
				foreach (List<NodesDirectedRow> lineList in grower.GetNets())
				{
					List<NodesDirectedRow> endRows = grower.GetEndNodes(lineList);

					foreach (NodesDirectedRow endRow in endRows)
					{
						errorCount += CheckDangle(endRow, allBox);
					}
				}
			}

			return errorCount;
		}

		private int ReportError([NotNull] NearError error)
		{
			return ReportError(
				error.Description, GetInvolvedRows(error), error.Geometry,
				error.IssueCode, null);
		}

		[NotNull]
		private InvolvedRows GetInvolvedRows([NotNull] NearError error)
		{
			return GetUniqueInvolvedRows(new[] {error.DirectedRow.Row, error.Near});
		}

		private void InitEndFilters()
		{
			if (_endFilters != null)
			{
				return;
			}

			CopyFilters(out _endFilters, out _endHelpers);

			foreach (QueryFilterHelper helper in _endHelpers)
			{
				helper.ForNetwork = true;
			}

			foreach (var filter in _endFilters)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}

		private int CheckGroupLength([NotNull] IEnumerable<NodesDirectedRow> lineList)
		{
			double sumLength = 0;
			var uniqueRows =
				new Dictionary<IDirectedRow, List<NodesDirectedRow>>(PathRowComparer);

			foreach (NodesDirectedRow dirRow in lineList)
			{
				IDirectedRow row = dirRow;
				List<NodesDirectedRow> dirRows;

				if (! uniqueRows.TryGetValue(row, out dirRows))
				{
					dirRows = new List<NodesDirectedRow>();
					uniqueRows.Add(row, dirRows);

					double length = dirRow.TopoLine.Length;
					sumLength += length;
					if (sumLength > _minGroupLength)
					{
						return NoError;
					}
				}

				dirRows.Add(dirRow);
			}

			var joined = new List<IGeometry>();
			var involved = new List<ITableIndexRow>();

			foreach (KeyValuePair<IDirectedRow, List<NodesDirectedRow>> pair in uniqueRows)
			{
				joined.Add(pair.Value[0].TopoLine.GetLine());
				involved.Add(pair.Key.Row);
			}

			var joinedLine = (IPolyline) GeometryFactory.CreateUnion(joined, 0);

			string comparison = FormatLengthComparison(
				sumLength, "<", _minGroupLength, joinedLine.SpatialReference).Trim();
			string description = string.Format("Group length too small: {0}", comparison);

			return ReportError(description, GetUniqueInvolvedRows(involved), joinedLine,
			                   LocalCodes[Code.GroupTooSmall], null);
		}

		private int CheckDangle([NotNull] NodesDirectedRow endRow,
		                        [CanBeNull] IRelationalOperator validArea = null)
		{
			bool isComplete;
			List<NodesDirectedRow> dangle = GetDangle(endRow, out isComplete);
			if (! isComplete)
			{
				return NoError;
			}

			double sumLength = GetLength(dangle, validArea);

			var isContiued = false;
			var isForked = false;
			DangleProperties props;
			if (_dangleProps.TryGetValue(dangle[0], out props))
			{
				isContiued = props.IsContinued;
				isForked = props.IsForked;
				_dangleProps.Remove(dangle[0]);
			}

			if (double.IsNaN(sumLength) || sumLength >= _maximumMinLength)
			{
				return NoError;
			}

			double minimumLength;
			string description;
			IssueCode issueCode;

			if (isForked && isContiued)
			{
				// at fork, continued

				if (MinDangleLengthAtForkContinued > 0)
				{
					minimumLength = MinDangleLengthAtForkContinued;
				}
				else if (MinDangleLengthContinued > 0)
				{
					minimumLength = MinDangleLengthContinued;
				}
				else if (MinDangleLengthAtFork > 0)
				{
					minimumLength = MinDangleLengthAtFork;
				}
				else
				{
					minimumLength = _minDangleLength;
				}

				description =
					"Dangle length too small (dangle ends at a fork and connects to other groups)";
				issueCode = LocalCodes[Code.Dangle_AtFork_Continued];
			}
			else if (isForked)
			{
				// at fork, discontinued

				minimumLength = MinDangleLengthAtFork > 0
					                ? MinDangleLengthAtFork
					                : _minDangleLength;

				description = "Dangle length too small (dangle ends at a fork)";
				issueCode = LocalCodes[Code.Dangle_AtFork_Discontinued];
			}
			else if (isContiued)
			{
				// not at fork, continued

				minimumLength = MinDangleLengthContinued > 0
					                ? MinDangleLengthContinued
					                : _minDangleLength;

				description = "Dangle length too small (dangle connects to other groups)";
				issueCode = LocalCodes[Code.Dangle_NotAtFork_Continued];
			}
			else
			{
				// not at fork, discontinued

				minimumLength = _minDangleLength;

				description = "Dangle length too small";
				issueCode = LocalCodes[Code.Dangle_NotAtFork_Discontinued];
			}

			if (sumLength >= minimumLength)
			{
				return NoError;
			}

			return ReportDangleError(
				dangle, sumLength, minimumLength, description, issueCode);
		}

		private int ReportDangleError([NotNull] IEnumerable<NodesDirectedRow> dangle,
		                              double sumLength,
		                              double minLeafLength,
		                              [NotNull] string baseDescription,
		                              [CanBeNull] IssueCode issueCode)
		{
			var joined = new List<IGeometry>();
			var involved = new List<ITableIndexRow>();

			foreach (NodesDirectedRow row in dangle)
			{
				joined.Add(row.TopoLine.GetLine());
				involved.Add(row.Row);
			}

			var joinedLine = (IPolyline) GeometryFactory.CreateUnion(joined, 0);

			string comparison = FormatLengthComparison(
				sumLength, "<", minLeafLength,
				joinedLine.SpatialReference).Trim();
			string description = string.Format("{0}: {1}", baseDescription, comparison);

			return ReportError(description, GetUniqueInvolvedRows(involved), joinedLine,
			                   issueCode, null);
		}

		private static double GetLength([NotNull] IEnumerable<NodesDirectedRow> leaf,
		                                IRelationalOperator validArea = null)
		{
			double sumLength = 0;

			foreach (NodesDirectedRow row in leaf)
			{
				IPolyline line = row.TopoLine.GetLine();
				if (validArea != null && ! validArea.Contains(line))
				{
					return double.NaN;
				}

				sumLength += row.TopoLine.Length;
			}

			return sumLength;
		}

		private class NearError
		{
			public bool DifferentGroup { get; set; }
			public string Description { get; set; }
			public IssueCode IssueCode { get; set; }

			public IGeometry Geometry { get; set; }
			public NodesDirectedRow DirectedRow { get; set; }
			public ITableIndexRow Near { get; set; }

			public double Offset { get; set; }
			public Group Group { get; set; }
			public Group OtherGroup { get; set; }

			public bool IsAtFork { get; set; }
			public bool IsCovered { get; set; }
		}

		private class DangleProperties
		{
			public bool IsContinued { get; set; }

			public bool IsForked { get; set; }
		}

		private class SimpleTableIndexRow : ITableIndexRow
		{
			public static SimpleTableIndexRow Create([NotNull] ITableIndexRow row)
			{
				return new SimpleTableIndexRow(row.TableIndex, row.RowOID);
			}

			public SimpleTableIndexRow(int tableIndex, long rowOid)
			{
				TableIndex = tableIndex;
				RowOID = rowOid;
			}

			public int TableIndex { get; }

			public IReadOnlyRow GetRow(IList<IReadOnlyTable> tableIndexTables)
			{
				return tableIndexTables[TableIndex].GetRow(RowOID);
			}

			public IReadOnlyRow CachedRow => null;

			public long RowOID { get; }
		}

		private class SimpleTopoLine : ITopologicalLine
		{
			[NotNull] private readonly IPolyline _simpleLine;

			public SimpleTopoLine(int partIndex, [NotNull] IPolyline simpleLine,
			                      double length, double fromAngle, double toAngle)
			{
				PartIndex = partIndex;
				_simpleLine = simpleLine;
				Length = length;

				FromAngle = fromAngle;
				ToAngle = toAngle;
			}

			public double Length { get; }

			public int PartIndex { get; }

			public IPoint FromPoint => _simpleLine.FromPoint;

			public IPoint ToPoint => _simpleLine.ToPoint;

			public double FromAngle { get; }

			public double ToAngle { get; }

			public IPolyline GetLine()
			{
				return _simpleLine;
			}

			public ICurve GetPath()
			{
				return _simpleLine;
			}
		}
	}
}
