using System.Collections.Generic;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources.WireFrame
{
	/// <summary>
	/// Shared state for wire frame layer and convenient entry points.
	/// Must be singleton! Must be thread-safe!
	/// </summary>
	public sealed class WireFrame
	{
		#region Singleton

		private static volatile WireFrame _instance;
		private static readonly object SyncRoot = new object();

		private WireFrame() // private to prevent outside instantiation
		{
			_cachedIDs = new Dictionary<string, IWireCache>();
		}

		public static WireFrame Instance
		{
			get
			{
				if (_instance is null) // performance optimization
				{
					lock (SyncRoot) // mutual exclusion
					{
						if (_instance is null)
						{
							_instance = new WireFrame();
						}
					}
				}

				return _instance;
			}
		}

		#endregion

		private readonly IDictionary<string, IWireCache> _cachedIDs;

		internal IWireCache GetWireCache(string forMapId)
		{
			lock (SyncRoot)
			{
				if (! _cachedIDs.TryGetValue(forMapId, out var result))
				{
					result = new WireCache();
					_cachedIDs.Add(forMapId, result);
				}

				return result;
			}
		}

		internal interface IWireCache
		{
			long GetID(string sourceClassName, long sourceOid);

			bool GetSource(long id, out string className, out long oid);
		}

		private class WireCache : IWireCache
		{
			private const long FirstId = 1;

			private long _nextId = FirstId;
			private readonly IDictionary<Key, long> _forward = new Dictionary<Key, long>();
			private readonly IDictionary<long, Key> _reverse = new Dictionary<long, Key>();

			private readonly object _syncRoot = new();

			public long GetID(string className, long oid)
			{
				var key = new Key(className, oid);

				lock (_syncRoot)
				{
					if (!_forward.TryGetValue(key, out var id))
					{
						id = _nextId++;
						_forward.Add(key, id);
						_reverse.Add(id, key);
					}

					return id;
				}
			}

			public bool GetSource(long id, out string className, out long oid)
			{
				lock (_syncRoot)
				{
					if (_reverse.TryGetValue(id, out var key))
					{
						className = key.ClassName;
						oid = key.Oid;
						return true;
					}

					className = default;
					oid = default;
					return false;
				}
			}

			private readonly struct Key
			{
				public string ClassName { get; }
				public long Oid { get; }

				public Key(string className, long oid)
				{
					ClassName = className ?? string.Empty;
					Oid = oid;
				}
			}
		}
	}
}
