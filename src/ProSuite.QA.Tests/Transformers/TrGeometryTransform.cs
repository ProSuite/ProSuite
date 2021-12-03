using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public abstract class TrGeometryTransform : ITableTransformer<IFeatureClass>,
	                                            IGeometryTransformer, IContainerTransformer
	{
		private readonly Tfc _transformedFc;

		public IList<ITable> InvolvedTables { get; }

		protected TrGeometryTransform([NotNull] IFeatureClass fc, esriGeometryType derivedShapeType)
		{
			InvolvedTables = new List<ITable> {(ITable) fc};

			_transformedFc = new Tfc(fc, derivedShapeType, this);
			// ReSharper disable once VirtualMemberCallInConstructor
			AddCustomAttributes(_transformedFc);
		}

		[TestParameter]
		[Doc(nameof(DocStrings.TrGeometryTransform_Attributes))]
		public IList<string> Attributes
		{
			get => ((Transformed) _transformedFc.BackingDs).Attributes;
			set => ((Transformed) _transformedFc.BackingDs).Attributes = value;
		}

		public IFeatureClass GetTransformed() => _transformedFc;

		object ITableTransformer.GetTransformed() => GetTransformed();

		protected virtual void AddCustomAttributes(GdbFeatureClass transformedFc) { }

		protected IFeature CreateFeature()
		{
			return _transformedFc.CreateFeature();
		}

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			_transformedFc.BackingDs.SetConstraint(tableIndex, condition);
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			_transformedFc.BackingDs.SetSqlCaseSensitivity(tableIndex, useCaseSensitiveQaSql);
		}

		IEnumerable<IFeature> IGeometryTransformer.Transform(IGeometry source)
			=> Transform(source);

		protected abstract IEnumerable<IFeature> Transform(IGeometry source);

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

		private class Tfc : GdbFeatureClass, ITransformedValue, ITransformedTable
		{
			public Tfc(IFeatureClass fc, esriGeometryType derivedShapeType,
			           IGeometryTransformer transformer)
				: base(-1, "derivedGeometry", derivedShapeType,
				       createBackingDataset: (t) => new Transformed((Tfc) t, fc),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				Transformer = transformer;
				InvolvedTables = new List<ITable> {(ITable) fc};

				IGeometryDef geomDef =
					fc.Fields.Field[
						fc.Fields.FindField(fc.ShapeFieldName)].GeometryDef;
				Fields.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						derivedShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));

				AddFields(fc);
			}

			public IGeometryTransformer Transformer { get; }

			protected override IObject CreateObject(int oid)
			{
				return new TfcFeature(oid, this);
			}

			private void AddFields(IFeatureClass fc)
			{
				for (int iField = 0; iField < fc.Fields.FieldCount; iField++)
				{
					IField f = fc.Fields.Field[iField];
					Fields.AddFields(FieldUtils.CreateField($"t0.{f.Name}", f.Type));
				}
			}

			public IList<ITable> InvolvedTables { get; }

			public ISearchable DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			[CanBeNull]
			public BoxTree<IFeature> KnownRows { get; private set; }

			public void SetKnownTransformedRows(IEnumerable<IRow> knownRows)
			{
				KnownRows = BoxTreeUtils.CreateBoxTree(
					knownRows?.Select(x => x as IFeature),
					getBox: x => x?.Shape != null
						             ? QaGeometryUtils.CreateBox(x.Shape)
						             : null);
			}

			public TransformedFeatureClass BackingDs => (Transformed) BackingDataset;
		}

		private class TfcFeature : GdbFeature
		{
			public TfcFeature(int oid, Tfc featureClass)
				: base(oid, featureClass) { }

			public override object get_Value(int index)
			{
				IField f = Table.Fields.Field[index];
				if (f.Name.StartsWith("t0."))
				{
					int baseRowsIdx = Table.Fields.FindField(InvolvedRowUtils.BaseRowField);
					IList<IRow> baseRows = (IList<IRow>) get_Value(baseRowsIdx);
					IRow sourceRow = baseRows[0];

					int idx = sourceRow.Table.FindField(f.Name.Substring(3));
					return sourceRow.get_Value(idx);
				}

				return base.get_Value(index);
			}
		}

		private class Transformed : TransformedFeatureClass<Tfc>
		{
			private readonly IFeatureClass _t0;

			public Transformed(
				[NotNull] Tfc tfc,
				[NotNull] IFeatureClass t0)
				: base(tfc, ProcessBase.CastToTables(t0))
			{
				_t0 = t0;
				Resulting.SpatialReference = ((IGeoDataset) t0).SpatialReference;
			}

			public override IEnvelope Extent => ((IGeoDataset) _t0).Extent;

			public override IRow GetRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _t0.FeatureCount(queryFilter);
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
									$"Field {attr} does not exist in {DatasetUtils.GetName(_t0)}");
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

			public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
			{
				var involvedDict = new Dictionary<IFeature, Involved>();

				foreach (var row in DataContainer.Search(
					(ITable) _t0, filter, QueryHelpers[0]))
				{
					IFeature baseFeature = (IFeature) row;

					if (IsKnown(baseFeature, involvedDict))
					{
						continue;
					}

					IGeometry geom = baseFeature.Shape;
					foreach (IFeature featureWithTransformedGeom
						in Resulting.Transformer.Transform(geom))
					{
						IFeature f = featureWithTransformedGeom;

						List<IRow> involved = new List<IRow> {row};
						f.Value[IdxBaseRowField] = involved;

						foreach (var pair in AttrDict)
						{
							int iTarget = pair.Key;
							int iSource = pair.Value;
							f.Value[iTarget] = baseFeature.Value[iSource];
						}

						f.Store();

						yield return f;
					}
				}

				if ((Resulting.Transformer as IContainerTransformer)?.HandlesContainer == true &&
				    Resulting.KnownRows != null && filter is ISpatialFilter sp)
				{
					foreach (BoxTree<IFeature>.TileEntry entry in
						Resulting.KnownRows.Search(QaGeometryUtils.CreateBox(sp.Geometry)))
					{
						yield return entry.Value;
					}
				}
			}

			private bool IsKnown(
				[NotNull] IFeature baseFeature,
				[NotNull] Dictionary<IFeature, Involved> involvedDict)
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
