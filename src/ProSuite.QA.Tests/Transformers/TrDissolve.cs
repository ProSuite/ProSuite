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

				ITable fc = InvolvedTables[0];
				List<IField> attrs = new List<IField>();
				if (Attributes != null)
				{
					foreach (string attribute in value)
					{
						int iField = fc.FindField(attribute);
						if (iField < 0)
						{
							throw new InvalidOperationException(
								$"Unkown field '{attribute}' in '{DatasetUtils.GetName(fc)}'");
						}

						attrs.Add(fc.Fields.Field[iField]);
					}
				}

				_dissolvedFc.AddFields(attrs);
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
			public Tfc(IFeatureClass dissolve)
				: base(1, "dissolveResult", dissolve.ShapeType,
				       createBackingDataset: (t) => new Transformed(t, dissolve),
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

			public void AddFields(List<IField> fields)
			{
				foreach (var field in fields)
				{
					Fields.AddFields(FieldUtils.CreateField(field.Name, field.Type));
					Fields.AddFields(
						FieldUtils.CreateField($"{field.Name}_unique",
						                       esriFieldType.esriFieldTypeSmallInteger));
				}
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
				[NotNull] GdbTable gdbTable,
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

					// first 3 fields: baseRows, oid, shape
					for (int iField = 3; iField < Resulting.Fields.FieldCount; iField++)
					{
						IField f = Resulting.Fields.get_Field(iField);
						int iOrig = rows[0].Table.FindField(f.Name);
						if (iOrig >= 0)
						{
							object value = null;
							bool unique = true;
							foreach (IRow row in rows)
							{
								object v = row.Value[iOrig];
								if (value == null)
								{
									value = v;
								}
								else if (! value.Equals(v))
								{
									unique = false;
								}
							}

							dissolved.set_Value(iField, value);
							dissolved.set_Value(iField + 1, unique ? 1 : 0);
						}
					}

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
