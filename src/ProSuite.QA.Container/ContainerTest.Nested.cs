using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
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
				[NotNull] IFeatureClass featureClass)
				: base(test, featureClass) { }

			protected sealed override void ValidateError(object sender, QaErrorEventArgs args)
			{
				if (args.QaError.Geometry != null)
				{
					EnsureFilter();
					_filter.Geometry = args.QaError.Geometry;
					foreach (IRow row in Test.Search((ITable) FeatureClass, _filter, _helper))
					{
						if (! (row is IFeature f)) continue;

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

				ITable table = (ITable) FeatureClass;
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
				[NotNull] IFeatureClass featureClass)
			{
				Test = test;
				FeatureClass = featureClass;

				ITable table = (ITable) FeatureClass;
				_tableIndex = Test.AddInvolvedTable(
					table, default(string), default(bool), queriedOnly: true);

				test.PostProcessError += ValidateError;
			}

			protected abstract void ValidateError(object sender, QaErrorEventArgs args);

			[NotNull]
			public ContainerTest Test { get; }

			[NotNull]
			public IFeatureClass FeatureClass { get; }

			protected int TableIndex => _tableIndex;

			public void Dispose()
			{
				Test.PostProcessError -= ValidateError;
			}
		}
	}
}
