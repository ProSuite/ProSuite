using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrDissolve : TableTransformer<TransformedFeatureClass>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public enum SearchOption
		{
			Tile,
			All
		}

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		private readonly IReadOnlyFeatureClass _toDissolve;

		[DocTr(nameof(DocTrStrings.TrDissolve_0))]
		public TrDissolve(
			[NotNull] [DocTr(nameof(DocTrStrings.TrDissolve_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(new List<IReadOnlyTable> {featureClass})
		{
			_toDissolve = featureClass;
			NeighborSearchOption = _defaultSearchOption;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_SearchDistance))]
		public new double Search { get; set; }

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrDissolve_NeighborSearchOption))]
		public SearchOption NeighborSearchOption { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_Attributes))]
		public IList<string> Attributes { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_GroupBy))]
		public IList<string> GroupBy { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_Constraint))]
		public string Constraint { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrDissolve_CreateMultipartFeatures))]
		public bool CreateMultipartFeatures { get; set; }

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			var dissolvedFc = new TransformedFc(_toDissolve, name);

			dissolvedFc.SearchDistance = Search;
			dissolvedFc.NeighborSearchOption = NeighborSearchOption;
			if (Attributes != null)
			{
				AddFields(dissolvedFc, Attributes, isGrouped: true);
			}

			if (GroupBy != null)
			{
				AddFields(dissolvedFc, GroupBy, isGrouped: false);
				dissolvedFc.GroupBy = GroupBy;
			}

			if (! string.IsNullOrWhiteSpace(Constraint))
			{
				dissolvedFc.Constraint = Constraint;
			}

			dissolvedFc.CreateMultipartFeatures = CreateMultipartFeatures;

			return dissolvedFc;
		}

		// TODO: Unify with TrSpatialJoin
		private void AddFields([NotNull] TransformedFc dissolveFc,
		                       [CanBeNull] IList<string> fieldNames, bool isGrouped)
		{
			if (fieldNames == null)
			{
				return;
			}

			IReadOnlyTable fc = InvolvedTables[0];

			try
			{
				if (isGrouped)
				{
					AssertValidFieldNames(fieldNames, fc);
				}

				Dictionary<string, string>
					expressionDict = ExpressionUtils.GetFieldDict(fieldNames);
				Dictionary<string, string> aliasFieldDict =
					ExpressionUtils.CreateAliases(expressionDict);

				TableView tv = dissolveFc.TableView;
				if (tv == null)
				{
					tv = TableViewFactory.Create(fc, expressionDict, aliasFieldDict, isGrouped);
					dissolveFc.TableView = tv;
				}
				else
				{
					foreach (string fieldName in fieldNames)
					{
						FieldColumnInfo ci = FieldColumnInfo.Create(fc, fieldName);
						tv.AddColumn(ci);
					}
				}

				foreach (string field in expressionDict.Keys)
				{
					dissolveFc.AddCustomField(field);
				}
			}
			catch (Exception e)
			{
				_msg.Warn(
					$"Error adding fields to {dissolveFc.Name}: {StringUtils.Concatenate(fieldNames, ", ")}.",
					e);
				_msg.Info(PrintFieldList(fc));
				throw;
			}
		}

		private static string PrintFieldList(IReadOnlyTable table)
		{
			var fieldList = DatasetUtils.GetFields(table.Fields)
			                            .Where(f => f.Name != InvolvedRowUtils.BaseRowField)
			                            .Select(f => f.Name).ToList();

			string fieldDisplayList = $"List of fields of {table.Name}: " +
			                          $"{Environment.NewLine}{StringUtils.Concatenate(fieldList, Environment.NewLine)}";

			return fieldDisplayList;
		}

		private static void AssertValidFieldNames([NotNull] IList<string> fieldNames,
		                                          [NotNull] IReadOnlyTable table)
		{
			var fieldList = DatasetUtils.GetFields(table.Fields)
			                            .Where(f => f.Name != InvolvedRowUtils.BaseRowField)
			                            .Select(f => f.Name).ToList();

			foreach (string fieldName in fieldNames)
			{
				if (string.IsNullOrEmpty(fieldName))
				{
					throw new InvalidOperationException(
						$"Null or empty field name defined for {table.Name}");
				}

				// Quick and dirty. TODO: AttributeValidator
				bool isExpression =
					fieldName.IndexOf("AS", StringComparison.OrdinalIgnoreCase) >= 0;

				if (! isExpression &&
				    ! fieldList.Any(
					    f => f.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)))
				{
					_msg.Warn($"Field name {fieldName} does not exist in {table.Name}.");
				}
			}
		}

		private class TransformedFc : TransformedFeatureClass, ITransformedTable,
		                              IDataContainerAware,
		                              IHasSearchDistance
		{
			public TableView TableView { get; set; }
			[CanBeNull] private IList<string> _groupBy;

			public TransformedFc(IReadOnlyFeatureClass dissolve, string name = null)
				: base(-1, ! string.IsNullOrEmpty(name) ? name : "dissolveResult",
				       dissolve.ShapeType,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, dissolve),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<IReadOnlyTable> {dissolve};

				IGeometryDef geomDef =
					dissolve.Fields.Field[
						dissolve.Fields.FindField(dissolve.ShapeFieldName)].GeometryDef;

				FieldsT.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						dissolve.ShapeType,
						geomDef.SpatialReference,
						geomDef.GridCount > 0 ? geomDef.GridSize[0] : 0,
						geomDef.HasZ, geomDef.HasM));
			}

			public double SearchDistance { get; set; }
			public SearchOption NeighborSearchOption { get; set; }
			public bool CreateMultipartFeatures { get; set; }
			public List<FieldInfo> CustomFields { get; private set; }

			public IList<string> GroupBy
			{
				get => _groupBy;
				set => _groupBy = value;
			}

			public void AddCustomField(string field)
			{
				try
				{
					IField f =
						FieldUtils.CreateField(
							field, FieldUtils.GetFieldType(TableView.GetColumnType(field)));
					FieldsT.AddFields(f);

					CustomFields = CustomFields ?? new List<FieldInfo>();
					CustomFields.Add(new FieldInfo(field, Fields.FindField(field), -1));
				}
				catch (Exception e)
				{
					throw new InvalidOperationException(
						$"{this.Name}: Unable to add field {field}.", e);
				}
			}

			public string Constraint
			{
				get => ((TransformedDataset) BackingDataset)?.Constraint;
				set
				{
					if (BackingDs is TransformedDataset ds)
					{
						ds.Constraint = value;
					}
				}
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public IDataContainer DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			bool ITransformedTable.NoCaching => false;

			public TransformedDataset BackingDs => (TransformedDataset) BackingDataset;

			[CanBeNull]
			public BoxTree<VirtualRow> KnownRows { get; private set; }

			public void SetKnownTransformedRows(IEnumerable<VirtualRow> knownRows)
			{
				if (NeighborSearchOption != SearchOption.All)
				{
					return;
				}

				KnownRows = BoxTreeUtils.CreateBoxTree(
					knownRows?.Select(x => x as VirtualRow),
					getBox: x => x?.Shape != null
						             ? QaGeometryUtils.CreateBox(x.Shape)
						             : null);
			}
		}

		private class TransformedDataset : TransformedBackingDataset<TransformedFc>
		{
			private static readonly TableIndexRowComparer
				_rowComparer = new TableIndexRowComparer();

			private readonly IReadOnlyFeatureClass _dissolve;
			private NetworkBuilder _builder;
			private string _constraint;
			private QueryFilterHelper _constraitHelper;

			public TransformedDataset(
				[NotNull] TransformedFc gdbTable,
				[NotNull] IReadOnlyFeatureClass dissolve) :
				base(gdbTable, CastToTables(dissolve))
			{
				_dissolve = dissolve;

				Resulting.SpatialReference = _dissolve.SpatialReference;
			}

			public override IEnvelope Extent => _dissolve.Extent;

			public override VirtualRow GetUncachedRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _dissolve.RowCount(queryFilter);
			}

			public string Constraint
			{
				get => _constraint;
				set
				{
					_constraint = value;
					_constraitHelper = new QueryFilterHelper(Resulting, value, false);
				}
			}

			private IEnumerable<IReadOnlyRow> GetBaseFeatures(IQueryFilter filter, bool recycling)
			{
				if (DataContainer != null)
				{
					var ext = DataContainer.GetLoadedExtent(_dissolve);
					if (filter is ISpatialFilter sf &&
					    ((IRelationalOperator) ext).Contains(sf.Geometry))
					{
						return DataContainer.Search(_dissolve, filter, QueryHelpers[0]);
					}
				}

				IQueryFilter f = (IQueryFilter) ((IClone) filter).Clone();
				f.WhereClause = QueryHelpers[0].TableView.Constraint;
				return _dissolve.EnumRows(f, recycle: recycling);
			}

			public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
			{
				foreach (VirtualRow resultRow in DissolveSearchedFeatures(filter, recycling))
				{
					if (_constraitHelper?.MatchesConstraint(resultRow) != false)
					{
						resultRow.Store();
						yield return resultRow;
					}
				}
			}

			private IEnumerable<VirtualRow> DissolveSearchedFeatures(
				IQueryFilter filter, bool recycling)
			{
				// Quick & dirty implementation for polygons. TODO: Optimize
				if (_dissolve.ShapeType == esriGeometryType.esriGeometryPolygon)
				{
					foreach (VirtualRow dissolvedArea in DissolveSearchedAreaFeatures(
						         filter, recycling))
					{
						yield return dissolvedArea;
					}
				}
				else
				{
					foreach (VirtualRow virtualRow in DissolveSearchedLineFeaatures(
						         filter, recycling))
					{
						yield return virtualRow;
					}
				}
			}

			private IEnumerable<VirtualRow> DissolveSearchedLineFeaatures(
				IQueryFilter filter, bool recycling)
			{
				// TODO: implement GroupBy
				_builder = _builder ?? new NetworkBuilder(includeBorderNodes: true);
				_builder.ClearAll();
				IRelationalOperator queryEnv =
					(IRelationalOperator) (filter as ISpatialFilter)?.Geometry.Envelope;
				IEnvelope fullBox = null;
				Dictionary<VirtualRow, Involved> involvedDict =
					new Dictionary<VirtualRow, Involved>();
				foreach (var baseRow in GetBaseFeatures(filter, recycling))
				{
					IReadOnlyFeature baseFeature = (IReadOnlyFeature) baseRow;
					Involved baseInvolved = null;
					bool isKnown = false;
					// Alternative: Involved Rows check
					foreach (Involved knownInvolved in EnumKnownInvolveds(
						         baseFeature, Resulting.KnownRows, involvedDict))
					{
						baseInvolved =
							baseInvolved ??
							InvolvedRowUtils.EnumInvolved(new[] {baseFeature}).First();

						if ((knownInvolved as InvolvedNested)?.BaseRows
						                                     .Contains(baseInvolved) == true)
						{
							isKnown = true;
							break;
						}
					}

					// Alternative: Geometric check
					//if (Resulting.KnownRows != null)
					//{
					//	foreach (BoxTree<IFeature>.TileEntry entry in
					//		Resulting.KnownRows.Search(
					//			QaGeometryUtils.CreateBox(baseFeature.Extent)))
					//	{
					//		if (((IRelationalOperator) baseFeature.Shape).Within(entry.Value.Shape))
					//			// Remark: use Within ! (Overlaps() = false, if relation is within!)
					//		{
					//			isKnown = true;
					//			break;
					//		}
					//	}
					//}

					if (isKnown)
					{
						continue;
					}

					_builder.AddNetElements(baseRow, 0);
					if (fullBox == null)
					{
						fullBox = GeometryFactory.Clone(baseFeature.Shape.Envelope);
					}
					else
					{
						fullBox.Union(baseFeature.Shape.Envelope);
					}
				}

				if (Resulting.KnownRows != null && filter is ISpatialFilter sp)
				{
					foreach (BoxTree<VirtualRow>.TileEntry entry in
					         Resulting.KnownRows.Search(QaGeometryUtils.CreateBox(sp.Geometry)))
					{
						yield return entry.Value;
					}
				}

				if (fullBox == null)
				{
					yield break;
				}

				fullBox.QueryWKSCoords(out WKSEnvelope fullWks);

				_builder.BuildNet(fullWks, fullWks, 0);
				if (_builder.ConnectedLinesList.Count == 0)
				{
					yield break;
				}

				ConnectedBuilder connectedBuilder = new ConnectedBuilder(this);
				connectedBuilder.Build(_builder, queryEnv);

				// Get unique dissolved set
				HashSet<List<DirectedRow>> dissolvedSet = new HashSet<List<DirectedRow>>();
				foreach (List<DirectedRow> value in connectedBuilder.DissolvedDict.Values)
				{
					if (dissolvedSet.Add(value))
					{
						if (Resulting.CreateMultipartFeatures)
						{
							// there can be duplicate directedRows in value, remove them
							HashSet<DirectedRow> simpleSet =
								new HashSet<DirectedRow>(new PathRowComparer(_rowComparer));
							value.ForEach(x => simpleSet.Add(x));
							value.Clear();
							value.AddRange(simpleSet);
						}
					}
				}

				foreach (List<DirectedRow> dissolvedRows in dissolvedSet)
				{
					List<IReadOnlyRow> rows = new List<IReadOnlyRow>(dissolvedRows.Count);
					foreach (DirectedRow dirRow in dissolvedRows)
					{
						rows.Add(dirRow.Row.Row);
					}

					yield return CreateResultRow(rows);
				}
			}

			private VirtualRow CreateResultRow(List<IReadOnlyRow> rows)
			{
				IGeometry shape;
				if (rows.Count == 1)
				{
					shape = ((IReadOnlyFeature) rows[0]).Shape;
				}
				else
				{
					List<IGeometry> geometries = rows
					                             .Select(
						                             x => ((IReadOnlyFeature) x).Shape)
					                             .ToList();

					IGeometry union = GeometryFactory.CreateUnion(geometries);
					GeometryUtils.Simplify(union, true, false);
					shape = union;
				}

				return CreateResultRow(shape, rows);
			}

			private VirtualRow CreateResultRow(IGeometry shape, List<IReadOnlyRow> rows)
			{
				GdbFeature dissolved = Resulting.CreateFeature();

				dissolved.Shape = shape;

				dissolved.set_Value(
					Resulting.FindField(InvolvedRowUtils.BaseRowField),
					rows);

				TransformedFc r = Resulting;
				if (r.CustomFields?.Count > 0)
				{
					r.TableView.ClearRows();
					DataRow tableRow = null;
					foreach (IReadOnlyRow row in rows)
					{
						tableRow = r.TableView.Add(row);
					}

					if (tableRow != null)
					{
						foreach (FieldInfo fieldInfo in r.CustomFields)
						{
							dissolved.set_Value(fieldInfo.Index, tableRow[fieldInfo.Name]);
						}
					}

					r.TableView.ClearRows();
				}

				return dissolved;
			}

			private IEnumerable<VirtualRow> DissolveSearchedAreaFeatures(IQueryFilter filter,
				bool recycling)
			{
				// TODO:
				// Use proper geometry grouping as it is used in CleanMultipatchUtils or some optimized polygon network builder
				foreach (List<IReadOnlyRow> rowsToDissolve in GroupedBaseFeatures(filter, recycling)
					         .ToList())
				{
					IGeometry fullUnion = GeometryUtils.Union(
						rowsToDissolve.Select(r => ((IReadOnlyFeature) r).Shape).ToList());

					foreach (IGeometry dissolvedGeometry in GeometryUtils.Explode(fullUnion))
					{
						yield return CreateResultRow(dissolvedGeometry, rowsToDissolve);
					}
				}
			}

			private IEnumerable<List<IReadOnlyRow>> GroupedBaseFeatures(IQueryFilter filter,
				bool recycling)
			{
				if (Resulting.GroupBy == null || Resulting.GroupBy.Count == 0)
				{
					List<IReadOnlyRow> singleGroup = GetBaseFeatures(filter, recycling).ToList();
					if (singleGroup.Count > 0)
					{
						yield return singleGroup;
					}
				}
				else
				{
					foreach (List<IReadOnlyRow> group in GdbObjectUtils.GroupRowsByAttributes(
						         GetBaseFeatures(filter, recycling), r => r, Resulting.GroupBy))
					{
						yield return group;
					}
				}
			}

			private class ConnectedBuilder
			{
				private readonly Dictionary<DirectedRow, List<DirectedRow>> _dissolvedDict;
				private readonly TransformedDataset _r;
				private readonly HashSet<DirectedRow> _handledRows;
				private readonly List<DirectedRow> _missing;

				private ISpatialFilter _filter;

				public ConnectedBuilder(TransformedDataset r)
				{
					_dissolvedDict =
						new Dictionary<DirectedRow, List<DirectedRow>>(
							new PathRowComparer(_rowComparer));
					_missing = new List<DirectedRow>();
					_handledRows = new HashSet<DirectedRow>(new DirectedRowComparer(_rowComparer));

					_r = r;
				}

				public Dictionary<DirectedRow, List<DirectedRow>> DissolvedDict => _dissolvedDict;

				public void Build(NetworkBuilder network, IRelationalOperator queryEnv)
				{
					_dissolvedDict.Clear();
					_missing.Clear();
					_handledRows.Clear();
					foreach (List<DirectedRow> connectedRows in network.ConnectedLinesList)
					{
						if (Add(connectedRows, queryEnv))
						{
							connectedRows.ForEach(x => _handledRows.Add(x));
						}
					}

					while (_missing.Count > 0)
					{
						HandleMissing(queryEnv);
					}
				}

				private void HandleMissing(IRelationalOperator queryEnv)
				{
					List<DirectedRow> missing = new List<DirectedRow>(_missing);
					_missing.Clear();

					foreach (DirectedRow directedRow in missing)
					{
						_filter = _filter ?? new SpatialFilterClass();
						ISpatialFilter f = _filter;
						f.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
						IEnvelope queryGeom = directedRow.FromPoint.Envelope;
						double tolerance = GeometryUtils.GetXyTolerance(queryGeom);
						queryGeom.Expand(tolerance, tolerance, false);
						f.Geometry = queryGeom;
						List<IReadOnlyRow> baseFeatures =
							new List<IReadOnlyRow>(_r.GetBaseFeatures(f, recycling: false));

						if (baseFeatures.Count == 1)
						{
							Add(new List<DirectedRow> {directedRow}, queryEnv: null);
							_handledRows.Add(directedRow);
							continue;
						}

						if (baseFeatures.Count > 2)
						{
							if (! _r.Resulting.CreateMultipartFeatures)
							{
								Add(new List<DirectedRow> {directedRow}, queryEnv: null);
								_handledRows.Add(directedRow);
								continue;
							}
						}

						// TODO: handle multipart geometries
						// baseFeatures.Count = 2
						var localBuilder = new NetworkBuilder(includeBorderNodes: true);
						baseFeatures.ForEach(
							x => localBuilder.AddNetElements(x, 0));
						IEnvelope b = GeometryFactory.Clone(queryGeom);
						b.Expand(1, 1, asRatio: false);
						b.QueryWKSCoords(out WKSEnvelope box);
						localBuilder.BuildNet(box, box, 0);

						foreach (List<DirectedRow> directedRows in localBuilder
							         .ConnectedLinesList)
						{
							if (directedRows.FirstOrDefault(x => ! _handledRows.Contains(x)) ==
							    null)
							{
								continue;
							}

							Add(directedRows, (IRelationalOperator) queryGeom);
							directedRows.ForEach(x => _handledRows.Add(x));
						}
					}
				}

				private bool Add(List<DirectedRow> connectedRows, IRelationalOperator queryEnv)
				{
					if (! _r.Resulting.CreateMultipartFeatures)
					{
						return AddSinglepart(connectedRows, queryEnv);
					}
					else
					{
						JoinConnectedRows(connectedRows, queryEnv);
						return true;
					}
				}

				private bool AddSinglepart(List<DirectedRow> connectedRows,
				                           IRelationalOperator queryEnv)
				{
					List<List<DirectedRow>> groups =
						new List<List<DirectedRow>>(GetGroupedRows(connectedRows));
					if (_r.Resulting.NeighborSearchOption == SearchOption.All &&
					    queryEnv?.Contains(connectedRows[0].FromPoint) == false &&
					    groups.Any(x => x.Count < 3))
					{
						_missing.Add(connectedRows[0]);
						return false;
					}

					foreach (List<DirectedRow> groupedRows in groups)
					{
						if (groupedRows.Count == 2)
						{
							_dissolvedDict.TryGetValue(groupedRows[0],
							                           out List<DirectedRow> connected0);

							_dissolvedDict.TryGetValue(groupedRows[1],
							                           out List<DirectedRow> connected1);

							if (connected0 == null && connected1 == null)
							{
								List<DirectedRow> connected =
									new List<DirectedRow> {groupedRows[0], groupedRows[1]};
								_dissolvedDict.Add(groupedRows[0], connected);
								_dissolvedDict.Add(groupedRows[1], connected);
							}
							else if (connected0 == null)
							{
								connected1.Add(groupedRows[0]);
								_dissolvedDict.Add(groupedRows[0], connected1);
							}
							else if (connected1 == null)
							{
								connected0.Add(groupedRows[1]);
								_dissolvedDict.Add(groupedRows[1], connected0);
							}
							else
							{
								if (connected0 != connected1)
								{
									connected0.AddRange(connected1);
									foreach (DirectedRow row in connected0)
									{
										_dissolvedDict[row] = connected0;
									}

									connected1.Clear();
								}
								else
								{
									// no action needed
								}
							}
						}
						else
						{
							foreach (DirectedRow connectedRow in groupedRows)
							{
								if (! _dissolvedDict.ContainsKey(connectedRow))
								{
									_dissolvedDict.Add(connectedRow,
									                   new List<DirectedRow> {connectedRow});
								}
							}
						}
					}

					return true;
				}

				private void JoinConnectedRows(List<DirectedRow> connectedRows,
				                               IRelationalOperator queryEnv)
				{
					foreach (List<DirectedRow> groupedRows in GetGroupedRows(connectedRows))
					{
						List<DirectedRow> allRows = null;
						bool updateNeeded = false;
						foreach (DirectedRow connectedRow in groupedRows)
						{
							if (_dissolvedDict.TryGetValue(connectedRow,
							                               out List<DirectedRow> connecteds))
							{
								if (allRows == null)
								{
									allRows = connecteds;
									if (groupedRows.Count > 1)
									{
										allRows.AddRange(groupedRows);
										updateNeeded = true;
									}
								}
								else if (allRows != connecteds)
								{
									allRows.AddRange(connecteds);
									updateNeeded = true;
								}
							}
							else
							{
								if (allRows == null)
								{
									allRows = new List<DirectedRow>(groupedRows);
									updateNeeded = true;
								}
							}
						}

						if (updateNeeded)
						{
							foreach (DirectedRow directedRow in allRows)
							{
								_dissolvedDict[directedRow] = allRows;
							}
						}
					}

					if (_r.Resulting.NeighborSearchOption == SearchOption.All &&
					    connectedRows.Count > 0 &&
					    queryEnv?.Contains(connectedRows[0].FromPoint) == false)
					{
						_missing.Add(connectedRows[0]);
					}
				}

				private IEnumerable<List<DirectedRow>> GetGroupedRows(
					List<DirectedRow> connectedRows)
				{
					return GdbObjectUtils.GroupRowsByAttributes(
						connectedRows, c => c.Row.Row, _r.Resulting.GroupBy);
				}
			}
		}
	}
}
