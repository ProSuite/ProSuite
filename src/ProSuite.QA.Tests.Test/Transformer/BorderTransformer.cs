using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Container;
using System;
using System.Collections.Generic;

namespace ProSuite.QA.Tests.Test.Transformer
{
	internal class BorderTransformer : ITableTransformer<IReadOnlyFeatureClass>
	{
		private readonly IReadOnlyFeatureClass _borderFc;
		private readonly PolyFc _polyFc;

		public IList<IReadOnlyTable> InvolvedTables { get; }

		public BorderTransformer(IReadOnlyFeatureClass borderFc)
		{
			_borderFc = borderFc;
			InvolvedTables = new List<IReadOnlyTable> { _borderFc };

			_polyFc = new PolyFc(borderFc);
		}

		public IReadOnlyFeatureClass GetTransformed() => _polyFc;

		object ITableTransformer.GetTransformed() => GetTransformed();

		string ITableTransformer.TransformerName { get; set; }

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			// TODO
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			//TODO
		}

		internal class PolyFc : FeatureClassMock, IDataContainerAware
		{
			private readonly IReadOnlyFeatureClass _borderFc;
			private readonly QueryFilterHelper _filterHelper;

			public PolyFc(IReadOnlyFeatureClass borderFc)
				: base(-1, borderFc.Name + "_poly", esriGeometryType.esriGeometryPolygon)
			{
				_borderFc = borderFc;
				InvolvedTables = new List<IReadOnlyTable> { _borderFc };

				// TODO: Handle constraints on _borderFc
				_filterHelper = new QueryFilterHelper(_borderFc, null, false);
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }
			public IDataContainer DataContainer { get; set; }

			protected override IEnvelope Extent => _borderFc.Extent;

			protected override ICursor Search(IQueryFilter filter, bool recycling)
			{
				if (DataContainer == null)
				{
					throw new NotImplementedException("Not Implemented for missing DataContainer");
				}

				List<IReadOnlyRow> baseFeatures =
					new List<IReadOnlyRow>(DataContainer.Search(_borderFc, filter, _filterHelper));
				List<IRow> searchFeatures = new List<IRow>();
				foreach (IReadOnlyFeature baseFeature in baseFeatures)
				{
					FeatureMock f = CreateFeature();

					IPolygon poly =
						GeometryFactory.CreatePolygon(GeometryFactory.Clone(baseFeature.Shape));
					f.Shape = poly;
					f.Store();

					searchFeatures.Add(f);
				}

				return new MockCursor(searchFeatures);
			}
		}

		private class MockCursor : ICursor
		{
			private readonly IList<IRow> _rows;

			private readonly IEnumerator<IRow> _enumRows;

			public MockCursor(IList<IRow> rows)
			{
				_rows = rows;
				_enumRows = rows.GetEnumerator();
			}

			int ICursor.FindField(string Name)
			{
				throw new NotImplementedException();
			}

			IRow ICursor.NextRow()
			{
				if (_enumRows.MoveNext())
				{
					return _enumRows.Current;
				}

				return null;
			}

			void ICursor.UpdateRow(IRow Row)
			{
				throw new NotImplementedException();
			}

			void ICursor.DeleteRow()
			{
				throw new NotImplementedException();
			}

			object ICursor.InsertRow(IRowBuffer buffer)
			{
				throw new NotImplementedException();
			}

			void ICursor.Flush()
			{
				throw new NotImplementedException();
			}

			IFields ICursor.Fields => throw new NotImplementedException();
		}
	}
}
