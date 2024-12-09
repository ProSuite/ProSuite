using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public abstract class TrGeometryTransform : TableTransformer<TransformedFeatureClass>,
	                                            IGeometryTransformer, IContainerTransformer
	{
		private readonly esriGeometryType _derivedShapeType;
		private readonly ISpatialReference _derivedSpatialReference;

		protected TrGeometryTransform([NotNull] IReadOnlyFeatureClass fc,
		                              esriGeometryType derivedShapeType,
		                              ISpatialReference derivedSpatialReference = null)
			: base(new List<IReadOnlyTable> { fc })
		{
			_derivedShapeType = derivedShapeType;
			_derivedSpatialReference = derivedSpatialReference;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGeometryTransform_Attributes))]
		public IList<string> Attributes { get; set; }

		protected virtual TransformedFc InitTransformedFc(IReadOnlyFeatureClass fc, string name)
		{
			return new ShapeTransformedFc(fc, _derivedShapeType, this, name);
		}

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			IReadOnlyFeatureClass fc = (IReadOnlyFeatureClass) InvolvedTables[0];
			TransformedFc transformedFc = InitTransformedFc(fc, name);

			IDictionary<int, int> customValuesDict = new ConcurrentDictionary<int, int>();

			// ReSharper disable once VirtualMemberCallInConstructor
			IList<int> customAttrIndexes = AddCustomAttributes(transformedFc);

			int idx = 0;
			customValuesDict.Add(transformedFc.FindField(InvolvedRowUtils.BaseRowField), idx);
			idx++;
			customValuesDict.Add(transformedFc.FindField("OBJECTID"), idx);
			idx++;
			foreach (int customAttrIndex in customAttrIndexes)
			{
				customValuesDict.Add(customAttrIndex, idx);
				idx++;
			}

			transformedFc.CustomValuesDict = customValuesDict;

			TransformedTableFields fcFields = new TransformedTableFields(fc);
			if (Attributes != null)
			{
				fcFields.AddUserDefinedFields(Attributes, transformedFc);
			}
			else
			{
				fcFields.AddAllFields(transformedFc);
			}

			transformedFc.BaseRowValuesDict = fcFields.FieldIndexMapping;

			return transformedFc;
		}

		[NotNull]
		protected virtual IList<int> AddCustomAttributes(TransformedFeatureClass transformedFc)
		{
			return new List<int>();
		}

		protected GdbFeature CreateFeature(long? oid = null)
		{
			TransformedFeatureClass fc = GetTransformed();
			GdbRow row = oid.HasValue ? fc.CreateObject(oid.Value) : fc.CreateFeature();
			return (GdbFeature) row; // GetTransformed().CreateFeature();
		}

		IEnumerable<GdbFeature> IGeometryTransformer.Transform(IGeometry source, long? sourceOid)
			=> Transform(source, sourceOid);

		protected abstract IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid);

		bool IContainerTransformer.IsGeneratedFrom(Involved involved, Involved source) =>
			IsGeneratedFrom(involved, source);

		protected virtual bool IsGeneratedFrom(Involved involved, Involved source)
		{
			if (! (involved is InvolvedNested i))
			{
				return false;
			}

			bool isGenereated = i.BaseRows.Contains(source);
			return isGenereated;
		}

		bool IContainerTransformer.HandlesContainer => HandlesContainer;
		protected virtual bool HandlesContainer => true;

		private class ShapeTransformedFc : TransformedFc
		{
			private readonly esriGeometryType _derivedShapeType;

			public ShapeTransformedFc(IReadOnlyFeatureClass fc, esriGeometryType derivedShapeType,
			                          IGeometryTransformer transformer,
			                          string name)
				: base(fc, derivedShapeType, (t) => new TransformedDataset((TransformedFc) t, fc),
				       transformer, name)
			{
				_derivedShapeType = derivedShapeType;
				AddStandardFields(fc);
			}

			protected override IField CreateShapeField(IReadOnlyFeatureClass involvedFc)
			{
				IGeometryDef geomDef =
					involvedFc.Fields.Field[
						involvedFc.Fields.FindField(involvedFc.ShapeFieldName)].GeometryDef;
				return FieldUtils.CreateShapeField(
					_derivedShapeType,
					geomDef.SpatialReference, hasZ: geomDef.HasZ, hasM: geomDef.HasM);
			}
		}

		protected class TransformedFc : TransformedFeatureClass, IDataContainerAware,
		                                ITransformedTable
		{
			protected TransformedFc(
				IReadOnlyFeatureClass fc, esriGeometryType derivedShapeType,
				[NotNull] Func<GdbTable, TransformedBackingDataset> createBackingDataset,
				IGeometryTransformer transformer, string name)
				: base(null, ! string.IsNullOrWhiteSpace(name) ? name : "derivedGeometry",
				       derivedShapeType,
				       createBackingDataset: createBackingDataset,
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				Transformer = transformer;
				InvolvedTables = new List<IReadOnlyTable> { fc };
			}

			protected void AddStandardFields(IReadOnlyFeatureClass involvedFc)
			{
				AddFieldT(FieldUtils.CreateOIDField());
				AddFieldT(CreateShapeField(involvedFc));
			}

			protected virtual IField CreateShapeField(IReadOnlyFeatureClass involvedFc)
			{
				IGeometryDef geomDef =
					involvedFc.Fields.Field[
						involvedFc.Fields.FindField(involvedFc.ShapeFieldName)].GeometryDef;
				return FieldUtils.CreateShapeField(
					involvedFc.ShapeType,
					geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM);
			}

			public IGeometryTransformer Transformer { get; }

			public override GdbRow CreateObject(long oid,
			                                    IValueList valueList = null)
			{
				var joinedValueList = new MultiListValues();
				{
					joinedValueList.AddList(new SimpleValueList(CustomValuesDict.Count),
					                        CustomValuesDict);
				}
				{
					joinedValueList.AddList(new PropertySetValueList(), ShapeDict);
				}
				// BaseRowValues are add in Search()
				return TransformedFeature.CreateFeature(oid, this, joinedValueList);
			}

			public IDictionary<int, int> CustomValuesDict { get; set; }
			public IDictionary<int, int> BaseRowValuesDict { get; set; }
			public IDictionary<int, int> ShapeDict => _shapeDict ?? (_shapeDict = GetShapeDict());
			private IDictionary<int, int> _shapeDict;

			private IDictionary<int, int> GetShapeDict()
			{
				IDictionary<int, int> shapeDict = new ConcurrentDictionary<int, int>();
				shapeDict.Add(FieldsT.FindField(ShapeFieldName), 0);
				return shapeDict;
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public IDataContainer DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			bool ITransformedTable.NoCaching => false;
			bool ITransformedTable.IgnoreOverlappingCachedRows => false;

			[CanBeNull]
			public BoxTree<IReadOnlyFeature> KnownRows { get; private set; }

			public void SetKnownTransformedRows(IEnumerable<IReadOnlyRow> knownRows)
			{
				KnownRows = BoxTreeUtils.CreateBoxTree(
					knownRows?.Select(x => x as IReadOnlyFeature),
					getBox: x => x?.Shape != null
						             ? ProxyUtils.CreateBox(x.Shape)
						             : null);
			}

			public TransformedDataset BackingDs => (TransformedDataset) BackingDataset;
		}

		protected abstract class TransformedFeature : GdbFeature
		{
			private class PolycurveFeature : TransformedFeature, IIndexedPolycurveFeature
			{
				private IndexedPolycurve _indexedPolycurve;

				public PolycurveFeature(long oid, TransformedFc featureClass,
				                        MultiListValues valueList)
					: base(oid, featureClass, valueList) { }

				bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => _indexedPolycurve == null;

				IIndexedSegments IIndexedSegmentsFeature.IndexedSegments
					=> _indexedPolycurve ??
					   (_indexedPolycurve = new IndexedPolycurve((IPointCollection4) Shape));
			}

			private class MultiPatchFeature : TransformedFeature, IIndexedMultiPatchFeature
			{
				private IndexedMultiPatch _indexedMultiPatch;

				public MultiPatchFeature(long oid, TransformedFc featureClass,
				                         MultiListValues valueList)
					: base(oid, featureClass, valueList) { }

				bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => true;

				IIndexedSegments IIndexedSegmentsFeature.IndexedSegments => IndexedMultiPatch;

				public IIndexedMultiPatch IndexedMultiPatch
					=> _indexedMultiPatch ??
					   (_indexedMultiPatch = new IndexedMultiPatch((IMultiPatch) Shape));
			}

			private class AnyFeature : TransformedFeature
			{
				public AnyFeature(long oid, TransformedFc featureClass, MultiListValues valueList)
					: base(oid, featureClass, valueList) { }
			}

			public static TransformedFeature CreateFeature(
				long oid, [NotNull] TransformedFc featureClass,
				[NotNull] MultiListValues valueList)
			{
				esriGeometryType geometryType = featureClass.ShapeType;

				TransformedFeature result;

				switch (geometryType)
				{
					case esriGeometryType.esriGeometryMultiPatch:
						result = new MultiPatchFeature(oid, featureClass, valueList);
						break;

					case esriGeometryType.esriGeometryPolygon:
					case esriGeometryType.esriGeometryPolyline:
						result = new PolycurveFeature(oid, featureClass, valueList);
						break;

					default:
						result = new AnyFeature(oid, featureClass, valueList);
						break;
				}

				return result;
			}

			private TransformedFeature(long oid, [NotNull] TransformedFc featureClass,
			                           [NotNull] MultiListValues valueList)
				: base(oid, featureClass, valueList) { }

			private new TransformedFc Table => (TransformedFc) base.Table;

			public void SetBaseValues(IReadOnlyRow baseRow)
			{
				((MultiListValues) ValueSet).AddList(
					new ReadOnlyRowBasedValues(baseRow),
					Assert.NotNull(Table.BaseRowValuesDict, "BaseRowValuesDict not set"));
			}
		}

		protected class TransformedDataset : TransformedBackingDataset<TransformedFc>
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

			private readonly IReadOnlyFeatureClass _t0;

			public TransformedDataset(
				[NotNull] TransformedFc tfc,
				[NotNull] IReadOnlyFeatureClass t0)
				: base(tfc, CastToTables(t0))
			{
				_t0 = t0;
				Resulting.SpatialReference = t0.SpatialReference;
			}

			public override IEnvelope Extent => _t0.Extent;

			protected IReadOnlyFeatureClass SourceFeatureClass => _t0;

			public override VirtualRow GetUncachedRow(long id)
			{
				throw new NotImplementedException();
			}

			public override long GetRowCount(ITableFilter queryFilter)
			{
				// TODO
				return _t0.RowCount(queryFilter);
			}

			private int? _idxBaseRowField;

			protected int IdxBaseRowField =>
				_idxBaseRowField ??
				(_idxBaseRowField = Resulting.FindField(InvolvedRowUtils.BaseRowField)).Value;

			public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
			{
				_msg.VerboseDebug(
					() => $"Transformer {Resulting.Name}: Searching input table {_t0.Name}...");

				var involvedDict = new Dictionary<IReadOnlyFeature, Involved>();

				filter = filter ?? new AoTableFilter();

				Assert.NotNull(DataContainer, "DataContainer has not been set.");

				foreach (var transformedFeature in EnumTransformedFeatures(
					         filter, QueryHelpers[0], involvedDict))
				{
					yield return transformedFeature;
				}

				if ((Resulting.Transformer as IContainerTransformer)?.HandlesContainer == true &&
				    Resulting.KnownRows != null && filter is IFeatureClassFilter sp)
				{
					foreach (BoxTree<IReadOnlyFeature>.TileEntry entry in
					         Resulting.KnownRows.Search(ProxyUtils.CreateBox(sp.FilterGeometry)))
					{
						yield return (VirtualRow) entry.Value;
					}
				}
			}

			private IEnumerable<VirtualRow> EnumTransformedFeatures(
				ITableFilter filter, QueryFilterHelper filterHelper,
				Dictionary<IReadOnlyFeature, Involved> involvedDict)
			{
				IList<TransformedFeature> rawFeatures = new List<TransformedFeature>();

				foreach (var row in DataContainer.Search(_t0, filter, filterHelper))
				{
					_msg.VerboseDebug(() => $"Processing input row <oid> {row.OID}...");

					IReadOnlyFeature baseFeature = (IReadOnlyFeature) row;

					if (IsKnown(baseFeature, involvedDict))
					{
						continue;
					}

					IGeometry geom = baseFeature.Shape;
					foreach (GdbFeature featureWithTransformedGeom
					         in Resulting.Transformer.Transform(geom, row.OID))
					{
						TransformedFeature f = (TransformedFeature) featureWithTransformedGeom;

						List<IReadOnlyRow> involved = new List<IReadOnlyRow> { row };
						f.set_Value(IdxBaseRowField, involved);

						if (Resulting.BaseRowValuesDict != null)
						{
							f.SetBaseValues(baseFeature);
						}

						f.Store();

						rawFeatures.Add(f);
					}
				}

				CompleteRawFeatures(rawFeatures);

				foreach (var f in rawFeatures)
				{
					yield return f;
				}
			}

			protected virtual void CompleteRawFeatures(IList<TransformedFeature> rawFeatures) { }

			protected bool IsKnown(
				[NotNull] IReadOnlyFeature baseFeature,
				[NotNull] Dictionary<IReadOnlyFeature, Involved> involvedDict)
			{
				if (! (Resulting.Transformer is IContainerTransformer ct))
				{
					return false;
				}

				Involved baseInvolved = null;
				foreach (var knownInvolved in EnumKnownInvolveds(
					         baseFeature, Resulting.KnownRows, involvedDict))
				{
					baseInvolved =
						baseInvolved ??
						InvolvedRowUtils.EnumInvolved(new[] { baseFeature }).First();
					if (ct.IsGeneratedFrom(knownInvolved, baseInvolved))
					{
						return true;
					}
				}

				return false;
			}
		}
	}
}
