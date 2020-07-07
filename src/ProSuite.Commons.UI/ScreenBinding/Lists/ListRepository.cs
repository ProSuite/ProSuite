using System;
using System.Collections.Generic;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public static class ListRepository
	{
		private static readonly object _locker = new object();

		private static readonly Dictionary<string, IPicklist> _namedLists =
			new Dictionary<string, IPicklist>();

		private static readonly List<IListSource> _sources = new List<IListSource>();

		private static readonly Dictionary<Type, IPicklist> _typedLists =
			new Dictionary<Type, IPicklist>();

		public static void InvalidateList<T>()
		{
			_typedLists.Remove(typeof(T));
		}

		public static IPicklist GetList<T>()
		{
			if (_typedLists.ContainsKey(typeof(T)))
			{
				return _typedLists[typeof(T)];
			}

			lock (_locker)
			{
				foreach (IListSource source in _sources)
				{
					IPicklist list = source.GetList<T>();
					if (list != null)
					{
						SetList<T>(list);
						return list;
					}
				}
			}

			return new Picklist<string>();
		}

		public static void SetList<T>(IPicklist list)
		{
			lock (_locker)
			{
				if (_typedLists.ContainsKey(typeof(T)))
				{
					_typedLists[typeof(T)] = list;
				}
				else
				{
					_typedLists.Add(typeof(T), list);
				}
			}
		}

		public static IPicklist GetList(string key)
		{
			if (_namedLists.ContainsKey(key))
			{
				return _namedLists[key];
			}

			lock (_locker)
			{
				foreach (IListSource source in _sources)
				{
					IPicklist list = source.GetList(key);
					if (list != null)
					{
						SetList(key, list);
						return list;
					}
				}
			}

			return new Picklist<string>();
		}

		public static void SetList(string key, IPicklist list)
		{
			lock (_locker)
			{
				if (_namedLists.ContainsKey(key))
				{
					_namedLists[key] = list;
				}
				else
				{
					_namedLists.Add(key, list);
				}
			}
		}

		public static void ClearLists()
		{
			lock (_locker)
			{
				_namedLists.Clear();
				_typedLists.Clear();
			}
		}

		public static void RegisterListSource(IListSource source)
		{
			lock (_locker)
			{
				_sources.Add(source);
			}
		}

		public static void ClearListSources()
		{
			lock (_locker)
			{
				_sources.Clear();
			}
		}

		public static bool HasListSource(IListSource source)
		{
			return _sources.Contains(source);
		}
	}
}
