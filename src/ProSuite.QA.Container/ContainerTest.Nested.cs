using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	partial class ContainerTest
	{
		protected class ContainsPostProcessor : ErrorPostProcessor
		{
			private QueryFilterHelper _helper;
			private ISpatialFilter _filter;

			public ContainsPostProcessor(
				[NotNull] ContainerTest test,
				[NotNull] IReadOnlyFeatureClass featureClass)
				: base(test, featureClass) { }

			protected sealed override void ValidateError(object sender, QaErrorEventArgs args)
			{
				if (args.QaError.Geometry != null)
				{
					EnsureFilter();
					_filter.Geometry = args.QaError.Geometry;
					foreach (IReadOnlyRow row in Test.Search(FeatureClass, _filter, _helper))
					{
						if (! (row is IReadOnlyFeature f)) continue;

						if (((IRelationalOperator2) f.Shape).ContainsEx(
							args.QaError.Geometry,
							esriSpatialRelationExEnum.esriSpatialRelationExBoundary))
						{
							args.Cancel = true;
							return;
						}
					}
				}
			}

			private void EnsureFilter()
			{
				if (_filter != null)
				{
					return;
				}

				IReadOnlyTable table = FeatureClass;
				_helper = new QueryFilterHelper(
					table, Test.GetConstraint(TableIndex), Test.GetSqlCaseSensitivity(TableIndex));
				_filter = new SpatialFilterClass();
				Test.ConfigureQueryFilter(TableIndex, _filter);
				_filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
			}
		}

		protected abstract class ErrorPostProcessor : IDisposable
		{
			private readonly int _tableIndex;

			protected ErrorPostProcessor(
				[NotNull] ContainerTest test,
				[NotNull] IReadOnlyFeatureClass featureClass)
			{
				Test = test;
				FeatureClass = featureClass;

				IReadOnlyTable table = FeatureClass;
				_tableIndex = Test.AddInvolvedTable(
					table, default, default, queriedOnly: true);

				test.PostProcessError += ValidateError;
			}

			protected abstract void ValidateError(object sender, QaErrorEventArgs args);

			[NotNull]
			public ContainerTest Test { get; }

			[NotNull]
			public IReadOnlyFeatureClass FeatureClass { get; }

			protected int TableIndex => _tableIndex;

			public void Dispose()
			{
				Test.PostProcessError -= ValidateError;
			}
		}
	}
}
