using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Internal.Mapping.Locate;

namespace ProSuite.Commons.AGP.Storage
{
	// now for only one dataset
	public abstract class GdbRepository<T, TDataset> : IRepository<T> where T : class
																	  where TDataset : Dataset	
	{
		private readonly string _gdbPath;
		private readonly string _name;
		private bool _repoIsChanged = false;

		protected GdbRepository(string gdbPath, string className = null)
		{
			_gdbPath = gdbPath;
			_name = className;
		}

		private Geodatabase _gdbGeodatabase;
		private Geodatabase GdbGeodatabase
		{
			get
			{
				if (_gdbGeodatabase == null)
				{
					var uri = new Uri(_gdbPath, UriKind.Absolute);
					_gdbGeodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri));
				}
				return _gdbGeodatabase;
			}
		}

		private TDataset _gdbDataset;
		private TDataset GdbDataset
		{
			get
			{
				return _gdbDataset ?? (_gdbDataset = GdbGeodatabase.OpenDataset<TDataset>(_name));
			}
		}

		public Definition GdbTableDefinition => GdbDataset.GetDefinition();

		private IList<T> _query;
		private IList<T> Query
		{
			get => _query ?? (_query = QueryGdbTable<TDataset>(GdbDataset));
			set => _query = value;
		}

		private QueryFilter _filter = null;
		public QueryFilter Filter
		{
			get => _filter;
			set
			{
				_filter = value;
				Query = QueryGdbTable<TDataset>(GdbDataset);
			}
		}

		#region overrides - mapping to/from Row to T

		public virtual T ParseRow(Row currentRow)
		{
			throw new NotImplementedException();
		}

		public virtual Row CreateRow(T item)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IRepository<T> Members

		public IQueryable<T> GetAll()
		{
			return Query.AsQueryable();
		}

		public void Add(T item)
		{
			Query.Add(item);
			_repoIsChanged = true;
		}

		public void Delete(T item)
		{
			var current = Query.FirstOrDefault(i => i.Equals(item));
			if (current == null) return;

			Query.Remove(current);
			_repoIsChanged = true;
		}

		public void SaveChanges()
		{
			if (_repoIsChanged)
				CommitChanges();
		}

		public void Update(T item)
		{
			var current = Query.FirstOrDefault(i => i.Equals(item));
			if (current == null) return;

			Query.Remove(current);
			Query.Add(item);
			_repoIsChanged = true;
		}

		#endregion

		#region private methods

		private IList<T> QueryGdbTable<TData>(TData value)
		{
			_repoIsChanged = false;
			if (value is FeatureClass features)
			{
				return ReadFeatureClassItems(features);
			}
			else if (value is Table table)
			{
				return ReadTableItems(table);
			}
			return new List<T>();
		}

		private IList<T> ReadTableItems(Table table)
		{
			var items = new List<T>();
			using (RowCursor cursor = table.Search(Filter, true))
			{
				while (cursor.MoveNext())
				{
					using (Row currentRow = cursor.Current)
					{
						T item = ParseRow(currentRow);
						items.Add(item);
					}
				}
			}
			return items;
		}

		private IList<T> ReadFeatureClassItems(FeatureClass featureClass)
		{
			var items = new List<T>();
			using (RowCursor cursor = featureClass.Search(Filter, true))
			{
				while (cursor.MoveNext())
				{
					using (Row currentRow = cursor.Current)
					{
						T item = ParseRow(currentRow);
						items.Add(item);
					}
				}
			}
			return items;
		}

		private void CommitChanges()
		{
			if (_repoIsChanged)
			{
				// check shema and build RowBuffer
				foreach (var item in Query)
				{
					//var row = CreateRow(item);
				}
			}
		}


		#endregion

	}

}
