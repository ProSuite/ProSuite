using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrDissolve : TableTransformer<TransformedFeatureClass>
	{
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
			: base(new List<IReadOnlyTable> { featureClass })
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

			if (GroupBy != null)
			{
				tableFields.AddUserDefinedFields(GroupBy, dissolvedFc);
				dissolvedFc.GroupBy = GroupBy;
			}

			var dissolvedDataset = (TransformedDataset) Assert.NotNull(dissolvedFc.BackingDataset);
			dissolvedDataset.TableFields = tableFields;

			if (! string.IsNullOrWhiteSpace(Constraint))
			{
				dissolvedFc.Constraint = Constraint;
			}

			dissolvedFc.CreateMultipartFeatures = CreateMultipartFeatures;

			return dissolvedFc;
		}

		private class TransformedFc : TransformedFeatureClass, ITransformedTable,
		                              IDataContainerAware,
		                              IHasSearchDistance
		{
			public TransformedFc(IReadOnlyFeatureClass dissolve, string name = null)
				: base(null, ! string.IsNullOrEmpty(name) ? name : "dissolveResult",
				       dissolve.ShapeType,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, dissolve),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<IReadOnlyTable> { dissolve };
			}

			public double SearchDistance { get; set; }
			public SearchOption NeighborSearchOption { get; set; }
			public bool CreateMultipartFeatures { get; set; }

			[CanBeNull]
			public IList<string> GroupBy { get; set; }

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

			private TransformedDataset BackingDs => (TransformedDataset) BackingDataset;

			[CanBeNull]
			public BoxTree<IReadOnlyFeature> KnownRows { get; private set; }

			public void SetKnownTransformedRows(IEnumerable<IReadOnlyRow> knownRows)
			{
				if (NeighborSearchOption != SearchOption.All)
				{
					return;
				}

				KnownRows = BoxTreeUtils.CreateBoxTree(
					knownRows?.Select(x => x as IReadOnlyFeature),
					getBox: x => x?.Shape != null
						             ? QaGeometryUtils.CreateBox(x.Shape)
						             : null);
			}
		}

		private class UniqueIdKey : IUniqueIdKey
		{
			bool IUniqueIdKey.IsVirtuell => BaseOid < 0;

			public long BaseOid { get; }
			public IReadOnlyList<long> BaseOids => _baseOids;
			private readonly List<long> _baseOids;

			public UniqueIdKey(IEnumerable<long> baseOids)
			{
				_baseOids = new List<long>(baseOids);
				Assert.True(_baseOids.Count > 0, "empty List");
				_baseOids.Sort();
				BaseOid = _baseOids[0];
			}

			public IReadOnlyRow BaseFeature { get; set; }

			public IList<InvolvedRow> GetInvolvedRows()
			{
				return InvolvedRowUtils.GetInvolvedRows(BaseFeature);
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

				if (x.BaseOids.Count != y.BaseOids.Count)
				{
					return false;
				}

				for (int i = 1; i < x.BaseOids.Count; i++)
				{
					if (x.BaseOids[i] != y.BaseOids[i])
					{
						return false;
					}
				}

				return true;
			}

			public int GetHashCode(UniqueIdKey obj)
			{
				return obj.BaseOid.GetHashCode() + 397 * obj.BaseOids.Count;
			}
		}


		private class TransformedDataset : TransformedBackingDataset<TransformedFc>
		{
			private readonly SimpleUniqueIdProvider<UniqueIdKey> _uniqueIdProvider =
				new SimpleUniqueIdProvider<UniqueIdKey>(new UniqueIdKeyComparer());

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

				QueryHelpers[0].FullGeometrySearch = true;
			}

			public override IEnvelope Extent => _dissolve.Extent;

			public override VirtualRow GetUncachedRow(long id)
			{
				throw new NotImplementedException();
			}

			public override long GetRowCount(IQueryFilter queryFilter)
			{
				return Search(queryFilter, true).Count();
				// TODO: Consider new Method GetRowCountEstimate()? Or add progress token to Search()?
				//return _dissolve.RowCount(queryFilter);
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
					if (QueryHelpers[0].FullGeometrySearch ||
					    (filter is ISpatialFilter sf &&
					     ((IRelationalOperator) ext).Contains(sf.Geometry)))
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
						         filter))
					{
						yield return dissolvedArea;
					}
				}
				else
				{
					foreach (VirtualRow virtualRow in DissolveSearchedLineFeatures(
						         filter, recycling))
					{
						yield return virtualRow;
					}
				}
			}

			private IEnumerable<IReadOnlyFeature> DissolveSearchedLineFeatures(
				IQueryFilter filter, bool recycling)
			{
				// TODO: implement GroupBy
				_builder = _builder ?? new NetworkBuilder(includeBorderNodes: true);
				_builder.ClearAll();
				IRelationalOperator queryEnv =
					(IRelationalOperator) (filter as ISpatialFilter)?.Geometry.Envelope;
				IEnvelope fullBox = null;
				Dictionary<IReadOnlyFeature, Involved> involvedDict =
					new Dictionary<IReadOnlyFeature, Involved>();
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
							InvolvedRowUtils.EnumInvolved(new[] { baseFeature }).First();

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
					foreach (BoxTree<IReadOnlyFeature>.TileEntry entry in
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

			private IReadOnlyFeature CreateResultRow(List<IReadOnlyRow> rows)
			{
				IGeometry shape;
				if (rows.Count == 1)
				{
					shape = ((IReadOnlyFeature) rows[0]).Shape;
				}
				else
				{
					List<IGeometry> geometries =
						rows.Select(x => ((IReadOnlyFeature) x).Shape).ToList();

					IGeometry union = GeometryFactory.CreateUnion(geometries);
					GeometryUtils.Simplify(union, true, false);
					shape = union;
				}

				return CreateResultRow(shape, rows);
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
			                                         IList<IReadOnlyRow> rows)
			{
				Assert.True(rows.Count > 0, "No rows to dissolve");

				var joinedValueList = new MultiListValues();
				{
					joinedValueList.AddList(new PropertySetValueList(), ShapeDict);
				}

				var calculatedValues = TableFields.GetCalculatedValues(rows).ToList();

				// Add the BaseRows:
				calculatedValues.Add(new CalculatedValue(BaseRowsFieldIndex, rows));

				// Add space for the generated OID
				calculatedValues.Add(new CalculatedValue(Resulting.OidFieldIndex, null));

				IValueList simpleList =
					TransformedAttributeUtils.ToSimpleValueList(
						calculatedValues, out IDictionary<int, int> calculatedCopyMatrix);

				joinedValueList.AddList(simpleList, calculatedCopyMatrix);

				// Consider using the one with the smallest OID?
				IReadOnlyRow mainRow = rows[0];

				// For potential GroupBy value(s) and the ObjectID field.
				IValueList rowValues = new ReadOnlyRowBasedValues(mainRow);
				joinedValueList.AddList(rowValues, TableFields.FieldIndexMapping);

				UniqueIdKey key = new UniqueIdKey(rows.Select(r => r.OID));
				int oid = _uniqueIdProvider.GetUniqueId(key);
				var dissolved = Resulting.CreateObject(oid, joinedValueList);

				dissolved.Shape = shape;
				key.BaseFeature = dissolved;

				return (IReadOnlyFeature)dissolved;
			}

			private IEnumerable<VirtualRow> DissolveSearchedAreaFeatures(IQueryFilter filter)
			{
				bool searchAcrossTiles = Resulting.NeighborSearchOption == SearchOption.All;

				foreach (VirtualRow virtualRow in DissolveSearchedAreaFeatures(
					         filter, searchAcrossTiles))
				{
					yield return virtualRow;
				}
			}

			private IEnumerable<IReadOnlyFeature> DissolveSearchedAreaFeatures(IQueryFilter filter,
				bool searchAcrossTiles)
			{
				IEnvelope spatialFilterEnvelope = (filter as ISpatialFilter)?.Geometry?.Envelope;
				EnvelopeXY requestedAreaXy =
					spatialFilterEnvelope == null
						? null
						: GeometryConversionUtils.CreateEnvelopeXY(spatialFilterEnvelope);

				double tolerance = GeometryUtils.GetXyTolerance(_dissolve.SpatialReference);

				foreach (List<IReadOnlyRow> rowsToDissolve in GroupedBaseFeatures(filter, false)
					         .ToList())
				{
					var inputFeaturesByOid =
						rowsToDissolve.ToDictionary(r => (long) r.OID, r => (IReadOnlyFeature) r);

					var geomList = new List<MultiPolycurve>();
					foreach (IReadOnlyRow row in rowsToDissolve)
					{
						MultiPolycurve multiPolycurve = GetMultiPolycurve(row);
						geomList.Add(multiPolycurve);
					}

					// 1. Step: Reduce number of polys per group by assembling union-able group:
					IList<ICollection<MultiPolycurve>> unionablePolygons =
						GetUnionablePolygonGroups(geomList, tolerance);

					//  Enlarge groups if necessary with neighboring tile's features:
					foreach (ICollection<MultiPolycurve> group in unionablePolygons)
					{
						bool couldBeExpanded =
							requestedAreaXy != null &&
							! GeomRelationUtils.AreBoundsContained(
								GeomTopoOpUtils.UnionEnvelopesXY(group),
								requestedAreaXy, tolerance);

						while (searchAcrossTiles && couldBeExpanded)
						{
							couldBeExpanded = AddExtraGeometries(inputFeaturesByOid, group, filter,
							                                     tolerance);
						}

						MultiLinestring union =
							GeomTopoOpUtils.GetUnionAreasXY(
								group.Cast<MultiLinestring>().ToList(), tolerance);

						IPolygon result = GeometryConversionUtils.CreatePolygon(
							inputFeaturesByOid.First().Value.Shape, union.GetLinestrings());

						// TODO: Check in which situation this is really really needed and where we could
						// just set the isKnownSimple flag.
						GeometryUtils.Simplify(result);

						var involvedFeatures = new List<IReadOnlyRow>();

						foreach (MultiPolycurve geom in group)
						{
							involvedFeatures.Add(
								inputFeaturesByOid[Assert.NotNull(geom.Id).Value]);
						}

						yield return CreateResultRow(result, involvedFeatures);
					}
				}
			}

			private static IList<ICollection<MultiPolycurve>> GetUnionablePolygonGroups(
				IEnumerable<MultiPolycurve> polygons, double tolerance)
			{
				IList<ICollection<MultiPolycurve>> unionablePolygons =
					GeomTopoOpUtils.GroupPolygons(
						polygons.ToList(),
						(m1, m2) =>
						{
							bool canUnion = GeomTopoOpUtils.CanDissolveAreasXY(
								m1, m2, tolerance,
								out IList<IntersectionPoint3D> intersectionPoints);

							if (canUnion)
							{
								// TODO: Remember intersection points which are the most expensive part to calculate, usually
							}

							return canUnion;
						}, tolerance);

				return unionablePolygons;
			}

			private static MultiPolycurve GetMultiPolycurve(IReadOnlyRow row)
			{
				IReadOnlyFeature feature = (IReadOnlyFeature) row;
				IPolycurve polycurve = (IPolycurve) feature.Shape;

				MultiPolycurve multiPolycurve =
					GeometryConversionUtils.CreateMultiPolycurve(polycurve);

				multiPolycurve.Id = feature.OID;
				return multiPolycurve;
			}

			/// <summary>
			/// Attempts to find more features in the source table that could be unioned with other
			/// geometries in the specified lists. The features are searched only outside the current
			/// search extent of the filter. The results are added to both <see cref="allFeaturesByOid"/>
			/// and <see cref="groupGeometries"/>.
			/// </summary>
			/// <param name="allFeaturesByOid"></param>
			/// <param name="groupGeometries"></param>
			/// <param name="filter"></param>
			/// <param name="tolerance"></param>
			/// <returns></returns>
			private bool AddExtraGeometries(
				[NotNull] IDictionary<long, IReadOnlyFeature> allFeaturesByOid,
				[NotNull] ICollection<MultiPolycurve> groupGeometries,
				[NotNull] IQueryFilter filter,
				double tolerance)
			{
				Assert.ArgumentCondition(allFeaturesByOid.Count > 0, "No rows to dissolve");
				Assert.ArgumentCondition(groupGeometries.Count > 0, "No geometries to dissolve");

				EnvelopeXY groupEnvelopeXY = GeomTopoOpUtils.UnionEnvelopesXY(groupGeometries);

				IReadOnlyFeature anyFeature = allFeaturesByOid.First().Value;
				ISpatialReference spatialReference = anyFeature.Shape.SpatialReference;

				IEnvelope unionEnvelope =
					GeometryFactory.CreateEnvelope(groupEnvelopeXY, spatialReference);

				// TODO: Filter using difference and where clause for group by attribute values
				// and possibly exclude already found features.
				ISpatialFilter filterClone = (ISpatialFilter) ((IClone) filter).Clone();

				// TODO: Instead of the current tile use the union of the previously searched areas
				IEnvelope currentTile =
					GeometryFactory.CreateEnvelope(DataContainer.CurrentTileExtent);
				currentTile.SpatialReference = unionEnvelope.SpatialReference;

				filterClone.Geometry =
					IntersectionUtils.Difference(GeometryFactory.CreatePolygon(unionEnvelope),
					                             GeometryFactory.CreatePolygon(currentTile));

				// Get extra rows - TODO: Limit to current group-by attribute values using where clause
				foreach (List<IReadOnlyRow> expandedGroup in
				         GroupedBaseFeatures(filterClone, false))
				{
					if (expandedGroup.Count == 0)
					{
						continue;
					}

					var all = new HashSet<IReadOnlyRow>(
						allFeaturesByOid.Values.Concat(expandedGroup));

					var allGrouped =
						GdbObjectUtils.GroupRowsByAttributes(all, r => r, Resulting.GroupBy);

					if (allGrouped.Count() != 1)
					{
						// The new values were not grouped into the old
						continue;
					}

					// Create lists with the new features/polygons
					var expandedPolygonList = new List<MultiPolycurve>(groupGeometries);
					var additionalFeaturesByOid = new Dictionary<long, IReadOnlyFeature>();
					foreach (IReadOnlyRow extraRow in expandedGroup)
					{
						if (! allFeaturesByOid.ContainsKey(extraRow.OID))
						{
							additionalFeaturesByOid.Add(extraRow.OID, (IReadOnlyFeature) extraRow);
							expandedPolygonList.Add(GetMultiPolycurve(extraRow));
						}
					}

					// Determine the new grouping considering the newly gotten features
					IList<ICollection<MultiPolycurve>> newGroupings =
						GetUnionablePolygonGroups(
							expandedPolygonList, tolerance);

					// Now find the (potentially enlarged) original group
					long anyOriginalOid = Assert.NotNull(groupGeometries.First().Id).Value;

					foreach (ICollection<MultiPolycurve> newGroup in newGroupings)
					{
						if (newGroup.Any(g => g.Id == anyOriginalOid))
						{
							// This is the potentially enlarged version of the original group
							bool polygonsHaveBeenAdded = false;
							foreach (MultiPolycurve multiPolycurve in newGroup)
							{
								long oid = Assert.NotNull(multiPolycurve.Id).Value;

								if (! allFeaturesByOid.ContainsKey(oid))
								{
									groupGeometries.Add(multiPolycurve);
									allFeaturesByOid.Add(oid, additionalFeaturesByOid[oid]);
									polygonsHaveBeenAdded = true;
								}
							}

							if (polygonsHaveBeenAdded)
								return true;
						}
					}
				}

				return false;
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
							Add(new List<DirectedRow> { directedRow }, queryEnv: null);
							_handledRows.Add(directedRow);
							continue;
						}

						if (baseFeatures.Count > 2)
						{
							if (! _r.Resulting.CreateMultipartFeatures)
							{
								Add(new List<DirectedRow> { directedRow }, queryEnv: null);
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

						bool anyAdded = false;
						List<DirectedRow> pairDirectedRows = null;
						foreach (List<DirectedRow> directedRows in localBuilder
							         .ConnectedLinesList)
						{
							if (directedRows.Count > 1)
								pairDirectedRows = pairDirectedRows ?? directedRows;
							if (directedRows.FirstOrDefault(x => ! _handledRows.Contains(x)) ==
							    null)
							{
								continue;
							}

							Add(directedRows, (IRelationalOperator) queryGeom);
							directedRows.ForEach(x => _handledRows.Add(x));

							anyAdded = true;
						}

						if (! anyAdded && pairDirectedRows != null)
						{
							Add(pairDirectedRows, (IRelationalOperator)queryGeom);
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
								if (!_dissolvedDict.Comparer.Equals(groupedRows[0], groupedRows[1]))
								{
									List<DirectedRow> connected =
										new List<DirectedRow> { groupedRows[0], groupedRows[1] };
									_dissolvedDict.Add(groupedRows[0], connected);
									_dissolvedDict.Add(groupedRows[1], connected);
								}
								else
								{
									List<DirectedRow> connected =
										new List<DirectedRow> { groupedRows[0] };
									_dissolvedDict.Add(groupedRows[0], connected);
								}
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
									                   new List<DirectedRow> { connectedRow });
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
