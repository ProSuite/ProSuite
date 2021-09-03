using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests.Transformers
{
	public class TrDissolve : ITableTransformer<IFeatureClass>
	{
		private readonly Tfc _dissolvedFc;
		private IList<string> _attributes;
		private IList<string> _groupBy;

		public IList<ITable> InvolvedTables { get; }

		public TrDissolve([NotNull] IFeatureClass featureclass)
		{
			InvolvedTables = new List<ITable> {(ITable) featureclass};

			_dissolvedFc = new Tfc(featureclass);
		}

		[TestParameter]
		public double Search { get; set; }

		[TestParameter]
		public IList<string> Attributes
		{
			get => _attributes;
			set
			{
				_attributes = value;
				AddFields(value, addUniqueField: true);
			}
		}

		[TestParameter]
		public IList<string> GroupBy
		{
			get => _groupBy;
			set
			{
				_groupBy = value;
				AddFields(value, addUniqueField: false);
			}
		}

		private void AddFields([CanBeNull] IList<string> fieldNames, bool addUniqueField)
		{
			if (fieldNames == null)
			{
				return;
			}

			ITable fc = InvolvedTables[0];

			foreach (string fieldName in fieldNames)
			{
				int iField = fc.FindField(fieldName);
				if (iField < 0)
				{
					throw new InvalidOperationException(
						$"Unkown field '{fieldName}' in '{DatasetUtils.GetName(fc)}'");
				}

				_dissolvedFc.AddField(fc.Fields.Field[iField], iField,
				                      addUniqueField);
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

		private class Tfc : GdbFeatureClass, ITransformedValue
		{
			private List<FieldInfo> _dissolveFields;

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

			[NotNull]
			public IList<FieldInfo> DissolveFields =>
				_dissolveFields ?? (_dissolveFields = new List<FieldInfo>());

			public void AddField(IField field, int sourceIndex, bool addUniqueField)
			{
				Fields.AddFields(FieldUtils.CreateField(field.Name, field.Type));
				FieldInfo fi = new FieldInfo(field.Name, Fields.FindField(field.Name), sourceIndex);
				if (addUniqueField)
				{
					string uniqueName = $"{field.Name}_unique";
					Fields.AddFields(
						FieldUtils.CreateField(uniqueName,
						                       esriFieldType.esriFieldTypeSmallInteger));
					fi.SetUnique(uniqueName, Fields.FindField(uniqueName));
				}

				DissolveFields.Add(fi);
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

			public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
			{
				// TODO: implement GroupBy
				_builder = _builder ?? new NetworkBuilder(includeBorderNodes: true);
				_builder.ClearAll();
				IEnvelope fullBox = null;
				foreach (IFeature baseRow in DataContainer.Search(
					(ITable) _dissolve, filter, QueryHelpers[0]))
				{
					_builder.AddNetElements(baseRow, 0);
					if (fullBox == null)
					{
						fullBox = GeometryFactory.Clone(baseRow.Shape.Envelope);
					}
					else
					{
						fullBox.Union(baseRow.Shape.Envelope);
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

				Dictionary<DirectedRow, List<DirectedRow>> dissolvedDict =
					new Dictionary<DirectedRow, List<DirectedRow>>(
						new PathRowComparer(new TableIndexRowComparer()));
				foreach (List<DirectedRow> connectedRows in _builder.ConnectedLinesList)
				{
					if (connectedRows.Count == 2)
					{
						dissolvedDict.TryGetValue(connectedRows[0],
						                          out List<DirectedRow> connected0);

						dissolvedDict.TryGetValue(connectedRows[1],
						                          out List<DirectedRow> connected1);

						if (connected0 == null && connected1 == null)
						{
							List<DirectedRow> connected =
								new List<DirectedRow> {connectedRows[0], connectedRows[1]};
							dissolvedDict.Add(connectedRows[0], connected);
							dissolvedDict.Add(connectedRows[1], connected);
						}
						else if (connected0 == null)
						{
							connected1.Add(connectedRows[0]);
							dissolvedDict.Add(connectedRows[0], connected1);
						}
						else if (connected1 == null)
						{
							connected0.Add(connectedRows[1]);
							dissolvedDict.Add(connectedRows[1], connected0);
						}
						else
						{
							if (connected0 != connected1)
							{
								connected0.AddRange(connected1);
								foreach (DirectedRow row in connected0)
								{
									dissolvedDict[row] = connected0;
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
						foreach (DirectedRow connectedRow in connectedRows)
						{
							if (! dissolvedDict.ContainsKey(connectedRow))
							{
								dissolvedDict.Add(connectedRow,
								                  new List<DirectedRow> {connectedRow});
							}
						}
					}
				}

				HashSet<List<DirectedRow>> dissolvedSet = new HashSet<List<DirectedRow>>();
				foreach (List<DirectedRow> value in dissolvedDict.Values)
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
					dissolved.set_Value(
						Resulting.FindField(InvolvedRowUtils.BaseRowCountField),
						rows.Count);

					FieldInfo.SetGroupValue(dissolved, rows, ((Tfc) Resulting).DissolveFields);

					if (_constraitHelper?.MatchesConstraint(dissolved) != false)
					{
						dissolved.Store();
						yield return dissolved;
					}
				}
			}
		}
	}
}
