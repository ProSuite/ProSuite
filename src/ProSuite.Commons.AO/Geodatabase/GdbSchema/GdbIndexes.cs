using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class GdbIndexes : IIndexes
	{
		private readonly List<IIndex> _indexes; // eager mode
		private readonly Lazy<List<IIndex>> _lazyIndexes; // lazy mode

		public GdbIndexes()
		{
			_indexes = new List<IIndex>();
		}

		public GdbIndexes([NotNull] Func<IEnumerable<IIndex>> loader)
		{
			Assert.ArgumentNotNull(loader, nameof(loader));

			_lazyIndexes = new Lazy<List<IIndex>>(() => new List<IIndex>(loader()));
		}

		private List<IIndex> Items => _indexes ?? _lazyIndexes.Value;

		public int IndexCount => Items.Count;

		public IIndex get_Index(int index) => Items[index];

		public void FindIndex([NotNull] string indexName, out int indexPosition)
		{
			indexPosition =
				Items.FindIndex(i => string.Equals(i.Name, indexName,
				                                   StringComparison.OrdinalIgnoreCase));
		}

		public IEnumIndex FindIndexesByFieldName([NotNull] string fieldName)
		{
			var matching = new List<IIndex>();

			foreach (IIndex index in Items)
			{
				IFields fields = index.Fields;
				for (int i = 0; i < fields.FieldCount; i++)
				{
					if (string.Equals(fields.get_Field(i).Name, fieldName,
					                  StringComparison.OrdinalIgnoreCase))
					{
						matching.Add(index);
						break;
					}
				}
			}

			return new IndexEnum(matching);
		}

		public void Add([NotNull] IIndex index)
		{
			_indexes.Add(index);
		}

		private class IndexEnum : IEnumIndex
		{
			private readonly IList<IIndex> _items;
			private int _position;

			public IndexEnum(IList<IIndex> items)
			{
				_items = items;
			}

			public IIndex Next()
			{
				if (_position >= _items.Count)
				{
					return null;
				}

				return _items[_position++];
			}

			public void Reset()
			{
				_position = 0;
			}
		}
	}
}
