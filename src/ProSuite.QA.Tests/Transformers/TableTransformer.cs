using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public abstract class TableTransformer<T> : InvolvesTablesBase, ITableTransformer<T>
	{
		private T _transformed;

		protected TableTransformer(IList<ITable> involvedTables)
			: base(involvedTables)
		{
			_tableConstraints = new Dictionary<int, string>();
			_tableCaseSensitivity = new Dictionary<int, bool>();
		}

		[NotNull] private readonly Dictionary<int, string> _tableConstraints;
		[NotNull] private readonly Dictionary<int, bool> _tableCaseSensitivity;
		private string _transformerName;

		public T GetTransformed()
		{
			if (_transformed == null)
			{
				T transformed = GetTransformedCore(_transformerName);
				if ((transformed as TransformedFeatureClass)?.BackingDataset is TransformedBackingDataset
				    tbds)
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

			return _transformed;
		}

		protected abstract T GetTransformedCore(string name);

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
