using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.GdbSchema.RowValues;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public abstract class TrGeometryTransform : TableTransformer<TransformedFeatureClass>,
	                                            IGeometryTransformer, IContainerTransformer
	{
		private readonly esriGeometryType _derivedShapeType;

		protected TrGeometryTransform([NotNull] IReadOnlyFeatureClass fc,
		                              esriGeometryType derivedShapeType)
			: base(new List<IReadOnlyTable> {fc})
		{
			_derivedShapeType = derivedShapeType;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGeometryTransform_Attributes))]
		public IList<string> Attributes { get; set; }


		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			IReadOnlyFeatureClass fc = (IReadOnlyFeatureClass) InvolvedTables[0];
			var transformedFc = new TransformedFc(fc, _derivedShapeType, this, name);

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

		protected GdbFeature CreateFeature()
		{
			return GetTransformed().CreateFeature(); // _transformedFc.CreateFeature();
		}

		IEnumerable<GdbFeature> IGeometryTransformer.Transform(IGeometry source)
			=> Transform(source);

		protected abstract IEnumerable<GdbFeature> Transform(IGeometry source);

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

		private class TransformedFc : TransformedFeatureClass, IDataContainerAware,
		                              ITransformedTable
		{
			public Dictionary<IReadOnlyTable, TransformedTableFields> TableFieldsBySource { get; }
				= new Dictionary<IReadOnlyTable, TransformedTableFields>();

			public TransformedFc(IReadOnlyFeatureClass fc, esriGeometryType derivedShapeType,
			                     IGeometryTransformer transformer, string name)
				: base(-1, ! string.IsNullOrWhiteSpace(name) ? name : "derivedGeometry",
				       derivedShapeType,
				       createBackingDataset: (t) => new TransformedDataset((TransformedFc) t, fc),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				Transformer = transformer;
				InvolvedTables = new List<IReadOnlyTable> {fc};
				AddStandardFields(fc, derivedShapeType);
			}

			private void AddStandardFields(IReadOnlyFeatureClass fc, esriGeometryType derivedShapeType)
			{
				IGeometryDef geomDef =
					fc.Fields.Field[
						fc.Fields.FindField(fc.ShapeFieldName)].GeometryDef;

				AddFieldT(FieldUtils.CreateOIDField());
				AddFieldT(FieldUtils.CreateShapeField(
					          derivedShapeType,
					          geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ,
					          geomDef.HasM));
			}

			public IGeometryTransformer Transformer { get; }

			public override GdbRow CreateObject(int oid,
			                                    IValueList valueList = null)
			{
				var joinedValueList = new MultiListValues();
				{
					joinedValueList.AddList(new SimpleValueList(CustomValuesDict.Count), CustomValuesDict);
				}
				{
					joinedValueList.AddList(new PropertySetValueList(), ShapeDict);
				}
				// BaseRowValues are add in Search()
				return new TransformedFeature(oid, this, joinedValueList);
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

			[CanBeNull]
			public BoxTree<VirtualRow> KnownRows { get; private set; }

			public void SetKnownTransformedRows(IEnumerable<VirtualRow> knownRows)
			{
				KnownRows = BoxTreeUtils.CreateBoxTree(
					knownRows?.Select(x => x as VirtualRow),
					getBox: x => x?.Shape != null
						             ? QaGeometryUtils.CreateBox(x.Shape)
						             : null);
			}

			public TransformedDataset BackingDs => (TransformedDataset) BackingDataset;
		}

		private class TransformedFeature : GdbFeature
		{
			public TransformedFeature(int oid, TransformedFc featureClass, MultiListValues valueList)
				: base(oid, featureClass, valueList) { }

			public new TransformedFc Table => (TransformedFc) base.Table;

			public void SetBaseValues(IReadOnlyRow baseRow)
			{
				((MultiListValues) ValueSet).AddList(
					new ReadOnlyRowBasedValues(baseRow),
					Assert.NotNull(Table.BaseRowValuesDict, "BaseRowValuesDict not set"));
			}
		}

		private class TransformedDataset : TransformedBackingDataset<TransformedFc>
		{
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

			public override VirtualRow GetUncachedRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _t0.RowCount(queryFilter);
			}

			private IList<string> _attributes;

			public IList<string> Attributes
			{
				get => _attributes;
				set
				{
					_attributes = value;
					if (_attributes != null)
					{
						foreach (string attr in _attributes)
						{
							int iSource = _t0.FindField(attr);
							if (iSource < 0)
							{
								throw new InvalidOperationException(
									$"Field {attr} does not exist in {_t0.Name}");
							}

							Resulting.AddField(_t0.Fields.Field[iSource]);
						}
					}

					_attrDict = null;
				}
			}

			private Dictionary<int, int> _attrDict;

			[NotNull]
			private Dictionary<int, int> AttrDict
			{
				get
				{
					if (_attrDict == null)
					{
						Dictionary<int, int> dict = new Dictionary<int, int>();
						if (_attributes != null)
						{
							foreach (string attr in _attributes)
							{
								int iSource = _t0.FindField(attr);
								int iTarget = Resulting.FindField(attr);
								dict.Add(iTarget, iSource);
							}
						}

						_attrDict = dict;
					}

					return _attrDict;
				}
			}

			private int? _idxBaseRowField;

			private int IdxBaseRowField =>
				_idxBaseRowField ??
				(_idxBaseRowField = Resulting.FindField(InvolvedRowUtils.BaseRowField)).Value;

			public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
			{
				var involvedDict = new Dictionary<VirtualRow, Involved>();

				filter = filter ?? new QueryFilterClass();

				Assert.NotNull(DataContainer, "DataContainer has not been set.");

				foreach (var row in DataContainer.Search(_t0, filter, QueryHelpers[0]))
				{
					IReadOnlyFeature baseFeature = (IReadOnlyFeature) row;

					if (IsKnown(baseFeature, involvedDict))
					{
						continue;
					}

					IGeometry geom = baseFeature.Shape;
					foreach (GdbFeature featureWithTransformedGeom
					         in Resulting.Transformer.Transform(geom))
					{
						TransformedFeature f = (TransformedFeature)featureWithTransformedGeom;

						List<IReadOnlyRow> involved = new List<IReadOnlyRow> {row};
						f.set_Value(IdxBaseRowField, involved);

						if (Resulting.BaseRowValuesDict != null)
						{
							f.SetBaseValues(baseFeature);
						}
						f.Store();

						yield return f;
					}
				}

				if ((Resulting.Transformer as IContainerTransformer)?.HandlesContainer == true &&
				    Resulting.KnownRows != null && filter is ISpatialFilter sp)
				{
					foreach (BoxTree<VirtualRow>.TileEntry entry in
					         Resulting.KnownRows.Search(QaGeometryUtils.CreateBox(sp.Geometry)))
					{
						yield return entry.Value;
					}
				}
			}

			private bool IsKnown(
				[NotNull] IReadOnlyFeature baseFeature,
				[NotNull] Dictionary<VirtualRow, Involved> involvedDict)
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
						InvolvedRowUtils.EnumInvolved(new[] {baseFeature}).First();
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
