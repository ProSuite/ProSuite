using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[InternallyUsedTest] //This shall be removed when the transformer is ready for use
	[GeometryTransformer]
	public class TrGetNodes : TableTransformer<TransformedFeatureClass>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyFeatureClass _toDissolve;

		[DocTr(nameof(DocTrStrings.TrGetNodes_0))]
		public TrGetNodes(
			[NotNull] [DocTr(nameof(DocTrStrings.TrGetNodes_lineClass))]
			IReadOnlyFeatureClass lineClass)
			: base(new List<IReadOnlyTable> { lineClass })
		{
			_toDissolve = lineClass;
		}

		[InternallyUsedTest]
		public TrGetNodes(
			[NotNull] TrGetNodesDefinition definition)
			: this((IReadOnlyFeatureClass)definition.LineClass)
		{
			Attributes = definition.Attributes;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGetNodes_Attributes))]
		public IList<string> Attributes { get; set; }

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			var dissolvedFc = new TransformedFc(_toDissolve, name);

			TransformedTableFields tableFields = new TransformedTableFields(_toDissolve)
			                                     {
				                                     AreResultRowsGrouped = true
			                                     };

			tableFields.AddOIDField(dissolvedFc, "OBJECTID", true);
			tableFields.AddShapeField(dissolvedFc, "SHAPE", true);

			if (Attributes != null)
			{
				tableFields.AddUserDefinedFields(Attributes, dissolvedFc);
			}

			var dissolvedDataset = (TransformedDataset) Assert.NotNull(dissolvedFc.BackingDataset);
			dissolvedDataset.TableFields = tableFields;

			return dissolvedFc;
		}

		private class TransformedFc : TransformedFeatureClass,
		                              IDataContainerAware
		{
			public TransformedFc(IReadOnlyFeatureClass dissolve, string name = null)
				: base(null, ! string.IsNullOrEmpty(name) ? name : "dissolveResult",
				       esriGeometryType.esriGeometryPoint,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, dissolve),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<IReadOnlyTable> { dissolve };
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public IDataContainer DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			private TransformedDataset BackingDs => (TransformedDataset) BackingDataset;
		}

		private class UniqueIdKey : IUniqueIdKey
		{
			bool IUniqueIdKey.IsVirtuell => BaseOid < 0;

			public long BaseOid { get; }
			public IReadOnlyList<long> BaseOids => _baseOids;
			private readonly List<long> _baseOids;
			private readonly IReadOnlyTable _table;

			public UniqueIdKey(IReadOnlyTable table, IEnumerable<DirectedRow> baseRows)
			{
				_baseOids = new List<long>();
				DirectedRow minRow = null;
				foreach (DirectedRow directedRow in baseRows)
				{
					minRow = minRow ?? directedRow;
					if (minRow.Row.RowOID > directedRow.Row.RowOID)
					{
						minRow = directedRow;
					}

					_baseOids.Add(directedRow.Row.Row.OID);
				}

				Assert.True(_baseOids.Count > 0, "empty List");
				_baseOids.Sort();
				Assert.NotNull(minRow);
				BaseOid = 2 * minRow.Row.RowOID + (minRow.IsBackward ? 1 : 0);
				_table = table;
			}

			public IList<InvolvedRow> GetInvolvedRows()
			{
				List<InvolvedRow> involveds =
					new List<InvolvedRow>(
						_baseOids.Select(oid => new InvolvedRow(_table.Name, oid)));
				return involveds;
			}
		}

		private class UniqueIdKeyComparer : IEqualityComparer<UniqueIdKey>
		{
			public bool Equals(UniqueIdKey x, UniqueIdKey y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				if (x.BaseOid != y.BaseOid)
				{
					return false;
				}

				return true;
			}

			public int GetHashCode(UniqueIdKey obj)
			{
				return obj.BaseOid.GetHashCode();
			}
		}

		private class TransformedDataset : TransformedBackingDataset<TransformedFc>
		{
			private readonly SimpleUniqueIdProvider<UniqueIdKey> _uniqueIdProvider =
				new SimpleUniqueIdProvider<UniqueIdKey>(new UniqueIdKeyComparer());

			private readonly IReadOnlyFeatureClass _dissolve;
			private NetworkBuilder _builder;

			public TransformedDataset(
				[NotNull] TransformedFc gdbTable,
				[NotNull] IReadOnlyFeatureClass dissolve) :
				base(gdbTable, CastToTables(dissolve))
			{
				_dissolve = dissolve;

				Resulting.SpatialReference = _dissolve.SpatialReference;

				QueryHelpers[0].FullGeometrySearch = false;
			}

			public override IEnvelope Extent => _dissolve.Extent;

			public override VirtualRow GetUncachedRow(long id)
			{
				throw new NotImplementedException();
			}

			public override long GetRowCount(ITableFilter queryFilter)
			{
				return Search(queryFilter, true).Count();
				// TODO: Consider new Method GetRowCountEstimate()? Or add progress token to Search()?
				//return _dissolve.RowCount(queryFilter);
			}

			private IEnumerable<IReadOnlyRow> GetBaseFeatures(ITableFilter filter, bool recycling)
			{
				if (DataContainer != null)
				{
					var ext = DataContainer.GetLoadedExtent(_dissolve);
					if (QueryHelpers[0].FullGeometrySearch ||
					    (filter is IFeatureClassFilter sf &&
					     ((IRelationalOperator) ext).Contains(sf.FilterGeometry)))
					{
						return DataContainer.Search(_dissolve, filter, QueryHelpers[0]);
					}
				}

				ITableFilter f = filter.Clone();
				f.WhereClause = QueryHelpers[0].TableView.Constraint;
				return _dissolve.EnumRows(f, recycle: recycling);
			}

			public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
			{
				foreach (VirtualRow resultRow in DissolveSearchedFeatures(filter, recycling))
				{
					resultRow.Store();
					yield return resultRow;
				}
			}

			private IEnumerable<VirtualRow> DissolveSearchedFeatures(
				ITableFilter filter, bool recycling)
			{
				foreach (VirtualRow virtualRow in DissolveSearchedLineFeatures(
					         filter, recycling))
				{
					yield return virtualRow;
				}
			}

			private IEnumerable<IReadOnlyFeature> DissolveSearchedLineFeatures(
				ITableFilter filter, bool recycling)
			{
				if (! (filter is IFeatureClassFilter fcFilter) ||
				    fcFilter.FilterGeometry == null)
				{
					yield break;
				}

				// TODO: implement GroupBy
				_builder = _builder ?? new NetworkBuilder(includeBorderNodes: true);
				_builder.ClearAll();

				foreach (var baseRow in GetBaseFeatures(filter, recycling))
				{
					_builder.AddNetElements(baseRow, 0);
				}

				fcFilter.FilterGeometry.Envelope.QueryWKSCoords(out WKSEnvelope fullWks);

				_builder.BuildNet(fullWks, fullWks, 0);
				if (_builder.ConnectedLinesList.Count == 0)
				{
					yield break;
				}

				foreach (List<DirectedRow> connectedLines in _builder.ConnectedLinesList)
				{
					yield return CreateResultRow(connectedLines);
				}
			}

			private IReadOnlyFeature CreateResultRow(List<DirectedRow> directedRows)
			{
				Assert.True(directedRows.Count > 0, "No rows");
				IGeometry shape = directedRows[0].FromPoint;

				return CreateResultRow(shape, directedRows);
			}

			public TransformedTableFields TableFields { get; set; }

			private IDictionary<int, int> _shapeDict;
			private IDictionary<int, int> ShapeDict => _shapeDict ?? (_shapeDict = GetShapeDict());

			private IDictionary<int, int> GetShapeDict()
			{
				IDictionary<int, int> shapeDict = new Dictionary<int, int>(1);
				shapeDict.Add(TableFields.ShapeFieldIndex, 0);
				return shapeDict;
			}

			private IReadOnlyFeature CreateResultRow(IGeometry shape,
			                                         IList<DirectedRow> directedRows)
			{
				Assert.True(directedRows.Count > 0, "No rows");

				IList<IReadOnlyRow> rows = directedRows.Select(directedRow => directedRow.Row.Row)
				                                       .ToList();

				var joinedValueList = new MultiListValues();
				{
					joinedValueList.AddList(new PropertySetValueList(), ShapeDict);
				}

				var calculatedValues = TableFields.GetCalculatedValues(rows).ToList();

				//// Consider using the one with the smallest OID?
				//IReadOnlyRow mainRow = minRow.Row.Row;

				//// For potential GroupBy value(s) and the ObjectID field.
				//IValueList rowValues = new ReadOnlyRowBasedValues(mainRow);
				// joinedValueList.AddList(rowValues, TableFields.FieldIndexMapping);

				UniqueIdKey key = new UniqueIdKey(rows[0].Table, directedRows);
				long oid = _uniqueIdProvider.GetUniqueId(key);

				// Add the BaseRows:
				calculatedValues.Add(new CalculatedValue(BaseRowsFieldIndex, rows));

				// Add space for the generated OID
				calculatedValues.Add(new CalculatedValue(Resulting.OidFieldIndex, oid));

				IValueList simpleList =
					TransformedAttributeUtils.ToSimpleValueList(
						calculatedValues, out IDictionary<int, int> calculatedCopyMatrix);

				joinedValueList.AddList(simpleList, calculatedCopyMatrix);

				var dissolved = Resulting.CreateObject(oid, joinedValueList);

				dissolved.Shape = shape;

				long objectId = dissolved.OID;

				return (IReadOnlyFeature) dissolved;
			}
		}
	}
}
