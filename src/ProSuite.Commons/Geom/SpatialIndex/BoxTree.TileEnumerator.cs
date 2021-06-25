using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	partial class BoxTree
	{
		internal class TileEnumerator : IEnumerator<BoxTile>
		{
			private readonly BoxTile _startTile;
			private readonly Box _startBox;
			private bool _init;

			private Box _currentBox;
			private IBox _searchBox;

			public TileEnumerator([NotNull] BoxTree tree, IBox searchBox)
				: this(tree._mainTile, tree._mainBox, searchBox) { }

			internal TileEnumerator(BoxTile startTile, Box startBox, IBox searchBox)
			{
				_startTile = startTile;
				_startBox = startBox;

				_searchBox = searchBox;
				Reset();
			}

			public IBox SearchBox
			{
				get { return _searchBox; }
				set
				{
					Assert.ArgumentNotNull(value, nameof(value));
					Assert.ArgumentCondition(_searchBox.Contains(value),
					                         "new search box must be within current search box");

					_searchBox = value;
				}
			}

			#region IEnumerator Members

			public void Reset()
			{
				_init = false;
			}

			public BoxTile Current { get; private set; }

			object IEnumerator.Current => Current;

			public void Dispose() { }

			public override string ToString()
			{
				if (_currentBox == null)
				{
					return _init
						       ? "Empty"
						       : "Not initialized";
				}

				var sb = new StringBuilder();
				for (var i = 0; i < _currentBox.Dimension; i++)
				{
					double d = _currentBox.Max[i] - _currentBox.Min[i];
					double x = _startBox.Max[i] - _startBox.Min[i];
					double s = _currentBox.Min[i] - _startBox.Min[i];

					var parts = (int) Math.Round(x / d);
					var idx = (int) Math.Round(2 * s / d);
					sb.AppendFormat("{0}/{1}; ", idx, parts);
				}

				return sb.ToString();
			}

			public bool MoveNext()
			{
				if (! _init)
				{
					Current = _startTile;
					_currentBox = _startBox?.Clone();
					_init = true;

					return _currentBox != null;
				}

				if (_currentBox == null)
				{
					return false;
				}

				while (Current != null)
				{
					Current = NextSelTile(Current, _currentBox, _searchBox,
					                      _startTile.Parent);
					return Current != null;
				}

				return false;
			}

			#endregion
		}

		public class TileEntryEnumerator : IEnumerator<TileEntry>
		{
			private readonly BoxTree _tree;
			private readonly BoxTile _startTile;
			private readonly Box _startBox;
			private IBox _searchBox;
			private TileEnumerator _tileEnum;
			private IEnumerator<TileEntry> _entryEnum;

			public TileEntryEnumerator(BoxTree tree, IBox search)
				: this(tree, tree._mainTile, tree._mainBox, search) { }

			internal TileEntryEnumerator(BoxTree tree, BoxTile startTile, Box startBox,
			                             IBox search)
			{
				_tree = tree;
				_startTile = startTile;
				_startBox = startBox;
				_searchBox = search;
				Reset();
			}

			public IBox SearchBox
			{
				get { return _searchBox; }
				set
				{
					if (_searchBox.Contains(value) == false)
					{
						throw new ArgumentException(
							"new search box must be within current search box");
					}

					_searchBox = value;
					_tileEnum.SearchBox = value;
				}
			}

			public void Reset()
			{
				_tileEnum = new TileEnumerator(_startTile, _startBox, _searchBox);
				_entryEnum = null;
			}

			public TileEntry Current => _entryEnum.Current;

			object IEnumerator.Current => Current;

			public void Dispose() { }

			public bool MoveNext()
			{
				while (true)
				{
					while (_entryEnum == null || ! _entryEnum.MoveNext())
					{
						if (! _tileEnum.MoveNext())
						{
							return false;
						}

						BoxTile tile = Assert.NotNull(_tileEnum.Current);

						_entryEnum = _tree.GetTileEnumerator(this, tile.EnumElems());
					}

					if (_searchBox == null)
					{
						return true;
					}

					if (Assert.NotNull(Current).Box.Intersects(_searchBox))
					{
						return true;
					}
				}
			}
		}

		public class TileEntryEnumerable : IEnumerable<TileEntry>
		{
			public TileEntryEnumerable(BoxTree tree, IBox search)
				: this(tree, tree.MainTile, tree.MainBox, search) { }

			internal TileEntryEnumerable(BoxTree tree, BoxTile startTile, Box startBox,
			                             IBox search)
			{
				Tree = tree;
				StartTile = startTile;
				StartBox = startBox;
				Search = search;
			}

			internal BoxTree Tree { get; }

			internal BoxTile StartTile { get; }

			public Box StartBox { get; }

			public IBox Search { get; }

			#region IEnumerable Members

			public TileEntryEnumerator GetEnumerator()
			{
				return GetEnumeratorCore();
			}

			IEnumerator<TileEntry> IEnumerable<TileEntry>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			protected virtual TileEntryEnumerator GetEnumeratorCore()
			{
				return new TileEntryEnumerator(Tree, StartTile, StartBox, Search);
			}

			#endregion
		}
	}

	partial class BoxTree<T>
	{
		public new class TileEntryEnumerator : BoxTree.TileEntryEnumerator,
		                                       IEnumerator<TileEntry>
		{
			internal TileEntryEnumerator(BoxTree tree, BoxTile startTile, Box startBox,
			                             IBox search)
				: base(tree, startTile, startBox, search) { }

			public new TileEntry Current => (TileEntry) base.Current;

			TileEntry IEnumerator<TileEntry>.Current => Current;
		}

		public new class TileEntryEnumerable : BoxTree.TileEntryEnumerable,
		                                       IEnumerable<TileEntry>
		{
			public TileEntryEnumerable(BoxTree<T> tree, IBox search)
				: this(tree, tree.MainTile, tree.MainBox, search) { }

			internal TileEntryEnumerable(BoxTree tree, BoxTile startTile, Box startBox,
			                             IBox search)
				: base(tree, startTile, startBox, search) { }

			internal new BoxTile StartTile => (BoxTile) base.StartTile;

			#region IEnumerable Members

			public new TileEntryEnumerator GetEnumerator()
			{
				return new TileEntryEnumerator(Tree, StartTile, StartBox, Search);
			}

			protected override BoxTree.TileEntryEnumerator GetEnumeratorCore()
			{
				return GetEnumerator();
			}

			IEnumerator<TileEntry> IEnumerable<TileEntry>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}
	}
}