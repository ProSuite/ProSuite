using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrDissolve : ITableTransformer<IFeatureClass>
	{
		public enum SearchOption
		{
			Tile,
			All
		}

		private const SearchOption _defaultSearchOption = SearchOption.Tile;
		private readonly Tfc _dissolvedFc;
		private IList<string> _attributes;
		private IList<string> _groupBy;

		public IList<ITable> InvolvedTables { get; }

		public TrDissolve([NotNull] IFeatureClass featureclass)
		{
			InvolvedTables = new List<ITable> {(ITable) featureclass};

			_dissolvedFc = new Tfc(featureclass);
			NeighborSearchOption = _defaultSearchOption;
		}

		[TestParameter]
		public double Search
		{
			get => _dissolvedFc.SearchDistance;
			set => _dissolvedFc.SearchDistance = value;
		}

		[TestParameter(_defaultSearchOption)]
		public SearchOption NeighborSearchOption
		{
			get => _dissolvedFc.NeighborSearchOption;
			set => _dissolvedFc.NeighborSearchOption = value;
		}

		[TestParameter]
		public IList<string> Attributes
		{
			get => _attributes;
			set
			{
				_attributes = value;
				AddFields(value);
			}
		}

		[TestParameter]
		public IList<string> GroupBy
		{
			get => _groupBy;
			set { _groupBy = value; }
		}

		// TODO: handle multiple method calls
		// TODO: Unify with TrSpatialJoin
		private void AddFields([CanBeNull] IList<string> fieldNames)
		{
			if (fieldNames == null)
			{
				return;
			}

			ITable fc = InvolvedTables[0];

			Dictionary<string, string> expressionDict = ExpressionUtils.GetFieldDict(fieldNames);
			Dictionary<string, string> aliasFieldDict =
				ExpressionUtils.CreateAliases(expressionDict);

			TableView tv =
				TableViewFactory.Create(fc, expressionDict, aliasFieldDict, isGrouped: true);

			_dissolvedFc.TableView = tv;

			foreach (string field in expressionDict.Keys)
			{
				_dissolvedFc.AddCustomField(field);
			}
		}

		[TestParameter]
		public string Constraint
		{
			get => _dissolvedFc.Constraint;
			set => _dissolvedFc.Constraint = value;
		}

		[TestParameter]
		public bool CreateMultipartFeatures
		{
			get => _dissolvedFc.CreateMultipartFeatures;
			set => _dissolvedFc.CreateMultipartFeatures = value;
		}

		public IFeatureClass GetTransformed() => _dissolvedFc;

		object ITableTransformer.GetTransformed() => GetTransformed();

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			_dissolvedFc.BackingDs.SetConstraint(tableIndex, condition);
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			_dissolvedFc.BackingDs.SetSqlCaseSensitivity(tableIndex, useCaseSensitiveQaSql);
		}

		private class Tfc : GdbFeatureClass, ITransformedTable, ITransformedValue, IHasSearchDistance
		{
			public TableView TableView { get; set; }

			public Tfc(IFeatureClass dissolve)
				: base(-1, "dissolveResult", dissolve.ShapeType,
				       createBackingDataset: (t) => new Transformed((Tfc) t, dissolve),
				       workspace: new GdbWorkspace(new TransformedWs()))
			{
				InvolvedTables = new List<ITable> {(ITable) dissolve};

				IGeometryDef geomDef =
					dissolve.Fields.Field[
						dissolve.Fields.FindField(dissolve.ShapeFieldName)].GeometryDef;
				Fields.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						dissolve.ShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			public double SearchDistance { get; set; }
			public SearchOption NeighborSearchOption { get; set; }
			public bool CreateMultipartFeatures { get; set; }
			public List<FieldInfo> CustomFields { get; private set; }

			public void AddCustomField(string field)
			{
				IField f =
					FieldUtils.CreateField(
						field, FieldUtils.GetFieldType(TableView.GetColumn(field).DataType));
				Fields.AddFields(f);

				CustomFields = CustomFields ?? new List<FieldInfo>();
				CustomFields.Add(new FieldInfo(field, Fields.FindField(field), -1));
			}

			public string Constraint
			{
				get => ((Transformed) BackingDataset)?.Constraint;
				set
				{
					if (BackingDs is Transformed ds)
					{
						ds.Constraint = value;
					}
				}
			}

			public IList<ITable> InvolvedTables { get; }

			public ISearchable DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			public TransformedFeatureClass BackingDs => (Transformed) BackingDataset;

						[CanBeNull]
			public BoxTree<IFeature> KnownRows { get; private set; }

			public void SetKnownTransformedRows(IEnumerable<IRow> knownRows)
			{
				if (NeighborSearchOption != SearchOption.All)
				{
					return;
				}

				KnownRows = BoxTreeUtils.CreateBoxTree(
					knownRows?.Select(x => x as IFeature),
					getBox: x => x?.Shape != null
						             ? QaGeometryUtils.CreateBox(x.Shape)
						             : null);
			}
		}

		private class TransformedWs : BackingDataStore
		{
			public override void ExecuteSql(string sqlStatement)
			{
				throw new NotImplementedException();
			}

			public override IEnumerable<IDataset> GetDatasets(esriDatasetType datasetType)
			{
				throw new NotImplementedException();
			}

			public override ITable OpenQueryTable(string relationshipClassName)
			{
				throw new NotImplementedException();
			}

			public override ITable OpenTable(string name)
			{
				throw new NotImplementedException();
			}
		}

		private class Transformed : TransformedFeatureClass<Tfc>
		{
			private static readonly TableIndexRowComparer
				_rowComparer = new TableIndexRowComparer();

			private readonly IFeatureClass _dissolve;
			private NetworkBuilder _builder;
			private string _constraint;
			private QueryFilterHelper _constraitHelper;

			public Transformed(
				[NotNull] Tfc gdbTable,
				[NotNull] IFeatureClass dissolve) :
				base(gdbTable, ProcessBase.CastToTables(dissolve))
			{
				_dissolve = dissolve;

				Resulting.SpatialReference = ((IGeoDataset) _dissolve).SpatialReference;
			}

			public override IEnvelope Extent => ((IGeoDataset) _dissolve).Extent;

			public override IRow GetRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _dissolve.FeatureCount(queryFilter);
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

			private IEnumerable<IRow> GetBaseFeatures(IQueryFilter filter, bool recycling)
			{
				if (DataContainer != null)
				{
					var ext = DataContainer.GetLoadedExtent((ITable) _dissolve);
					if (filter is ISpatialFilter sf &&
					    ((IRelationalOperator) ext).Contains(sf.Geometry))
					{
						return DataContainer.Search(
							(ITable) _dissolve, filter, QueryHelpers[0]);
					}
				}

				IQueryFilter f = (IQueryFilter) ((IClone) filter).Clone();
				f.WhereClause = QueryHelpers[0].TableView.Constraint;
				return new EnumCursor((ITable) _dissolve, f, recycle: recycling);
			}

			public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
			{
				// TODO: implement GroupBy
				_builder = _builder ?? new NetworkBuilder(includeBorderNodes: true);
				_builder.ClearAll();
				IRelationalOperator queryEnv =
					(IRelationalOperator) (filter as ISpatialFilter)?.Geometry.Envelope;
				IEnvelope fullBox = null;
				foreach (var baseRow in GetBaseFeatures(filter, recycling))
				{
					IFeature baseFeature = (IFeature) baseRow;
					if (Resulting.KnownRows != null)
					{
						bool isKnown = false;
						foreach (BoxTree<IFeature>.TileEntry entry in
							Resulting.KnownRows.Search(QaGeometryUtils.CreateBox(baseFeature.Extent)))
						{
							if (((IRelationalOperator2) baseFeature.Shape).Overlaps(
								entry.Value.Shape))
							{
								isKnown = true;
								break;
							}
						}

						if (isKnown)
						{
							continue;
						}
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
					foreach (BoxTree<IFeature>.TileEntry entry in
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
					List<IRow> rows = new List<IRow>(dissolvedRows.Count);
					foreach (DirectedRow dirRow in dissolvedRows)
					{
						rows.Add(dirRow.Row.Row);
					}

					GdbFeature dissolved = Resulting.CreateFeature();
					if (rows.Count == 1)
					{
						dissolved.Shape = ((IFeature) rows[0]).Shape;
					}
					else
					{
						List<IPolyline> paths = rows.Select(x => (IPolyline) ((IFeature) x).Shape)
						                            .ToList();
						IPolyline line = (IPolyline) GeometryFactory.CreateUnion(paths);
						line.SimplifyNetwork();
						dissolved.Shape = line;
					}

					dissolved.set_Value(
						Resulting.FindField(InvolvedRowUtils.BaseRowField),
						rows);

					Tfc r = Resulting;
					if (r.CustomFields?.Count > 0)
					{
						r.TableView.ClearRows();
						DataRow tableRow = null;
						foreach (IRow row in rows)
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

					if (_constraitHelper?.MatchesConstraint(dissolved) != false)
					{
						dissolved.Store();
						yield return dissolved;
					}
				}
			}

			private class ConnectedBuilder
			{
				private readonly Dictionary<DirectedRow, List<DirectedRow>> _dissolvedDict;
				private readonly Transformed _r;
				private readonly HashSet<DirectedRow> _handledRows;
				private readonly List<DirectedRow> _missing;

				private ISpatialFilter _filter;

				public ConnectedBuilder(Transformed r)
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
						Add(connectedRows, queryEnv);
						connectedRows.ForEach(x => _handledRows.Add(x));
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
						List<IRow> baseFeatures =
							new List<IRow>(_r.GetBaseFeatures(f, recycling: false));

						if (baseFeatures.Count == 1)
						{
							Add(new List<DirectedRow> {directedRow}, queryEnv: null);
							_handledRows.Add(directedRow);
							continue;
						}

						if (baseFeatures.Count > 2)
						{
							if (!_r.Resulting.CreateMultipartFeatures)
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
						IEnvelope b = GeometryFactory.Clone(f.Geometry.Envelope);
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

							Add(directedRows, queryEnv);
							directedRows.ForEach(x => _handledRows.Add(x));
						}
					}
				}

				private void Add(List<DirectedRow> connectedRows, IRelationalOperator queryEnv)
				{
					if (! _r.Resulting.CreateMultipartFeatures)
					{
						AddSinglepart(connectedRows, queryEnv);
					}
					else
					{
						JoinConnectedRows(connectedRows, queryEnv);
					}
				}

				private void AddSinglepart(List<DirectedRow> connectedRows, IRelationalOperator queryEnv)
				{

					if (connectedRows.Count == 2)
					{
						_dissolvedDict.TryGetValue(connectedRows[0],
						                           out List<DirectedRow> connected0);

						_dissolvedDict.TryGetValue(connectedRows[1],
						                           out List<DirectedRow> connected1);

						if (connected0 == null && connected1 == null)
						{
							List<DirectedRow> connected =
								new List<DirectedRow> { connectedRows[0], connectedRows[1] };
							_dissolvedDict.Add(connectedRows[0], connected);
							_dissolvedDict.Add(connectedRows[1], connected);
						}
						else if (connected0 == null)
						{
							connected1.Add(connectedRows[0]);
							_dissolvedDict.Add(connectedRows[0], connected1);
						}
						else if (connected1 == null)
						{
							connected0.Add(connectedRows[1]);
							_dissolvedDict.Add(connectedRows[1], connected0);
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
						if (_r.Resulting.NeighborSearchOption == SearchOption.All &&
						    connectedRows.Count == 1 &&
						    queryEnv?.Contains(connectedRows[0].FromPoint) == false)
						{
							_missing.Add(connectedRows[0]);
							return;
						}

						foreach (DirectedRow connectedRow in connectedRows)
						{
							if (!_dissolvedDict.ContainsKey(connectedRow))
							{
								_dissolvedDict.Add(connectedRow,
								                   new List<DirectedRow> { connectedRow });
							}
						}
					}
				}

				private void JoinConnectedRows(List<DirectedRow> connectedRows, IRelationalOperator queryEnv)
				{
					List<DirectedRow> allRows = null;
					bool updateNeeded = false;
					foreach (DirectedRow connectedRow in connectedRows)
					{
						if (_dissolvedDict.TryGetValue(connectedRow,
						                               out List<DirectedRow> connecteds))
						{
							if (allRows == null)
							{
								allRows = connecteds;
								if (connectedRows.Count > 1)
								{
									allRows.AddRange(connectedRows);
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
								allRows = new List<DirectedRow>(connectedRows);
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

					if (_r.Resulting.NeighborSearchOption == SearchOption.All &&
					    connectedRows.Count > 0 &&
					    queryEnv?.Contains(connectedRows[0].FromPoint) == false)
					{
						_missing.Add(connectedRows[0]);
					}
				}
			}
		}
	}
}
