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
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrDissolve : ITableTransformer<IFeatureClass>
	{
		public enum Option
		{
			WithinTile,
			WithinSearch,
			LoadMissing
		}

		private const Option _defaultOption = Option.WithinSearch;
		private readonly Tfc _dissolvedFc;
		private IList<string> _attributes;
		private IList<string> _groupBy;

		public IList<ITable> InvolvedTables { get; }

		public TrDissolve([NotNull] IFeatureClass featureclass)
		{
			InvolvedTables = new List<ITable> {(ITable) featureclass};

			_dissolvedFc = new Tfc(featureclass);
			DissolveOption = _defaultOption;
		}

		[TestParameter]
		public double Search
		{
			get => _dissolvedFc.SearchDistance;
			set => _dissolvedFc.SearchDistance = value;
		}

		[TestParameter(_defaultOption)]
		public Option DissolveOption
		{
			get => _dissolvedFc.DissolveOption;
			set => _dissolvedFc.DissolveOption = value;
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
		public bool CompleteMissingParts { get; set; } // TODO: implement behavior

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

		private class Tfc : GdbFeatureClass, ITransformedValue, IHasSearchDistance
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
			public Option DissolveOption { get; set; }

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

		private class Transformed : TransformedFeatureClass
		{
			private readonly static TableIndexRowComparer
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

				Tfc r = (Tfc) Resulting;

				ConnectedBuilder connectedBuilder = new ConnectedBuilder(this);
				connectedBuilder.Build(_builder, queryEnv);

				HashSet<List<DirectedRow>> dissolvedSet = new HashSet<List<DirectedRow>>();
				foreach (List<DirectedRow> value in connectedBuilder.DissolvedDict.Values)
				{
					dissolvedSet.Add(value);
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
						f.Geometry = directedRow.FromPoint;
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
							// TODO: handle multipart geometries
							// if singlePartGeometry:
							Add(new List<DirectedRow> {directedRow}, queryEnv: null);
							_handledRows.Add(directedRow);
							continue;
						}

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
					if (connectedRows.Count == 2)
					{
						_dissolvedDict.TryGetValue(connectedRows[0],
						                           out List<DirectedRow> connected0);

						_dissolvedDict.TryGetValue(connectedRows[1],
						                           out List<DirectedRow> connected1);

						if (connected0 == null && connected1 == null)
						{
							List<DirectedRow> connected =
								new List<DirectedRow> {connectedRows[0], connectedRows[1]};
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
						if (((Tfc) _r.Resulting).DissolveOption == Option.LoadMissing &&
						    connectedRows.Count == 1 &&
						    queryEnv?.Contains(connectedRows[0].FromPoint) == false)
						{
							_missing.Add(connectedRows[0]);
							return;
						}

						foreach (DirectedRow connectedRow in connectedRows)
						{
							if (! _dissolvedDict.ContainsKey(connectedRow))
							{
								_dissolvedDict.Add(connectedRow,
								                   new List<DirectedRow> {connectedRow});
							}
						}
					}
				}
			}
		}
	}
}
