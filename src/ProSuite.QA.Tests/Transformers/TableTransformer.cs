using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public abstract class TableTransformer<T> : InvolvesTablesBase, ITableTransformer<TransformedFeatureClass>
	{
		private TransformedFeatureClass _transformed;

		protected TableTransformer(IList<IReadOnlyTable> involvedTables)
			: base(involvedTables)
		{
			_tableConstraints = new Dictionary<int, string>();
			_tableCaseSensitivity = new Dictionary<int, bool>();
		}

		[NotNull] private readonly Dictionary<int, string> _tableConstraints;
		[NotNull] private readonly Dictionary<int, bool> _tableCaseSensitivity;
		private string _transformerName;

		public TransformedFeatureClass GetTransformed()
		{
			if (_transformed == null)
			{
				try
				{
					TransformedFeatureClass transformed = GetTransformedCore(_transformerName);
					if (transformed.BackingDataset is TransformedBackingDataset tbds)
					{
						foreach (var pair in _tableConstraints)
						{
							tbds.SetConstraint(pair.Key, pair.Value);
						}

						foreach (var pair in _tableCaseSensitivity)
						{
							tbds.SetSqlCaseSensitivity(pair.Key, pair.Value);
						}
					}
					_transformed = transformed;
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException(
						$"Cannot create TableTransformer {_transformerName}", exception);
				}
			}

			return _transformed;
		}

		protected abstract TransformedFeatureClass GetTransformedCore(string name);

		object ITableTransformer.GetTransformed() => GetTransformed();

		string ITableTransformer.TransformerName
		{
			get => _transformerName;
			set => _transformerName = value;
		}

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			Assert.Null(_transformed, "transformed value already initialized");
			_tableConstraints[tableIndex] = condition;
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			Assert.Null(_transformed, "transformed value  already initialized");
			_tableCaseSensitivity[tableIndex] = useCaseSensitiveQaSql;
		}
	}
}
