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
	public abstract class TrGeometryTransform : TableTransformer<IReadOnlyFeatureClass>, IGeometryTransformer, IContainerTransformer
	{
		private readonly esriGeometryType _derivedShapeType;

		protected TrGeometryTransform([NotNull] IReadOnlyFeatureClass fc, esriGeometryType derivedShapeType)
			: base(new List<IReadOnlyTable> { fc })
		{
			_derivedShapeType = derivedShapeType;
		}

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrGeometryTransform_Attributes))]
		public IList<string> Attributes { get; set; }

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			IReadOnlyFeatureClass fc = (IReadOnlyFeatureClass)InvolvedTables[0];
			var transformedFc = new TransformedFc(fc, _derivedShapeType, this, name);
			// ReSharper disable once VirtualMemberCallInConstructor
			AddCustomAttributes(transformedFc);
			transformedFc.BackingDs.Attributes = Attributes;

			return transformedFc;
		}

		protected virtual void AddCustomAttributes(TransformedFeatureClass transformedFc) { }

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
			if (!(involved is InvolvedNested i))
			{
				return false;
			}

			bool isGenereated = i.BaseRows.Contains(source);
			return isGenereated;
		}

		bool IContainerTransformer.HandlesContainer => HandlesContainer;
		protected virtual bool HandlesContainer => true;

		private class TransformedFc : TransformedFeatureClass, ITransformedValue, ITransformedTable
		{
			public TransformedFc(IReadOnlyFeatureClass fc, esriGeometryType derivedShapeType,
													 IGeometryTransformer transformer, string name)
				: base(-1, !string.IsNullOrWhiteSpace(name) ? name : "derivedGeometry",
							 derivedShapeType,
							 createBackingDataset: (t) => new TransformedDataset((TransformedFc)t, fc),
							 workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				Transformer = transformer;
				InvolvedTables = new List<IReadOnlyTable> { fc };

				IGeometryDef geomDef =
					fc.Fields.Field[
						fc.Fields.FindField(fc.ShapeFieldName)].GeometryDef;
				FieldsT.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						derivedShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));

				AddFields(fc);
			}

			public IGeometryTransformer Transformer { get; }

			protected override VirtualRow CreateObject(int oid)
			{
				return new TransformedFeature(oid, this);
			}

			private void AddFields(IReadOnlyFeatureClass fc)
			{
				for (int iField = 0; iField < fc.Fields.FieldCount; iField++)
				{
					IField f = fc.Fields.Field[iField];
					FieldsT.AddFields(FieldUtils.CreateField($"t0.{f.Name}", f.Type));
				}
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public ISearchable DataContainer
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

			public TransformedDataset BackingDs => (TransformedDataset)BackingDataset;
		}

		private class TransformedFeature : GdbFeature
		{
			public TransformedFeature(int oid, TransformedFc featureClass)
				: base(oid, featureClass) { }

			public override object get_Value(int index)
			{
				IField f = Table.Fields.Field[index];
				if (f.Name.StartsWith("t0."))
				{
					int baseRowsIdx = Table.Fields.FindField(InvolvedRowUtils.BaseRowField);
					IList<IReadOnlyRow> baseRows = (IList<IReadOnlyRow>)get_Value(baseRowsIdx);
					IReadOnlyRow sourceRow = baseRows[0];

					int idx = sourceRow.Table.FindField(f.Name.Substring(3));
					return sourceRow.get_Value(idx);
				}

				return base.get_Value(index);
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

				foreach (var row in DataContainer.Search(_t0, filter, QueryHelpers[0]))
				{
					IReadOnlyFeature baseFeature = (IReadOnlyFeature)row;

					if (IsKnown(baseFeature, involvedDict))
					{
						continue;
					}

					IGeometry geom = baseFeature.Shape;
					foreach (GdbFeature featureWithTransformedGeom
									 in Resulting.Transformer.Transform(geom))
					{
						GdbFeature f = featureWithTransformedGeom;

						List<IReadOnlyRow> involved = new List<IReadOnlyRow> { row };
						f.set_Value(IdxBaseRowField, involved);

						foreach (var pair in AttrDict)
						{
							int iTarget = pair.Key;
							int iSource = pair.Value;
							f.set_Value(iTarget, baseFeature.get_Value(iSource));
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
				if (!(Resulting.Transformer is IContainerTransformer ct))
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
