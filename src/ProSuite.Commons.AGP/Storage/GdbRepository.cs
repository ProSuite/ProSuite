using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Commons.AGP.Storage
{
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

		// TODO algr: interface for Search(filter, true) for FeatureClass, Table, ...
		private TDataset _gdbDataset;
		private TDataset GdbDataset
		{
			get
			{
				return _gdbDataset ?? (_gdbDataset = GdbGeodatabase.OpenDataset<TDataset>(_name));
			}
		}

		public Definition GdbTableDefinition => GdbDataset.GetDefinition();

		private ICollection<T> _query;
		private ICollection<T> Query
		{
			get => _query ?? (_query = QueryGdbTable<TDataset>());
			set => _query = value;
		}

		private QueryFilter _filter = null;
		public QueryFilter Filter
		{
			get => _filter;
			set
			{
				_filter = value;
				Query = QueryGdbTable<TDataset>();
			}
		}

		private ICollection<T> QueryGdbTable<TT>()
		{
			_repoIsChanged = false;
			//if ( typeof(TT) is FeatureClass)

			//return ReadFeatureClassItems(GdbDataset)(Filter, true);
			//return ReadFeatureClassItems(FeatureClass featureClass);
			return new List<T>();
		}

		private IList<T> ReadTableItems(Table table)
		{
			var items = new List<T>();
			if (items == null) return items;

			using (RowCursor cursor = table.Search(Filter, true))
			{
				while (cursor.MoveNext())
				{
					using (Row currentRow = cursor.Current)
					{
						// override T specific behaviour in derived class (issues.gdb->InvolvedTables field ->WorkItem, ...)
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
			if (items == null) return items;

			using (RowCursor cursor = featureClass.Search(Filter, true))
			{
				while (cursor.MoveNext())
				{
					using (Row currentRow = cursor.Current)
					{
						// override T specific behaviour in derived class (issues.gdb->InvolvedTables field ->WorkItem, ...)
						T item = ParseRow(currentRow);
						items.Add(item);
					}
				}
			}
			return items;
		}

		public virtual T ParseRow(Row currentRow)
		{
			throw new NotImplementedException();
		}

		public virtual Row CreateRow(T item)
		{
			throw new NotImplementedException();
		}

		// Get (from Query)

		// Update (Query)

		// ...

		// Commit - write in gdb, xml, ...

		//IReadOnlyList<Definition> _featureClasses => GdbGeodabase.GetDefinitions<FeatureClassDefinition>();

		//IReadOnlyList<Definition> _tables => GdbGeodabase.GetDefinitions<TableDefinition>();

		#region IRepository<T> Members

		public IEnumerable<T> GetAll()
		{
			return Query;
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

		private void CommitChanges()
		{
			throw new NotImplementedException();
		}

	}

}
