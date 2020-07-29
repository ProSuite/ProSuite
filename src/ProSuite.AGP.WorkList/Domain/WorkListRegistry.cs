using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	/// <summary>
	/// This singleton class serves as a registry for WorkLists:
	/// you can add, lookup, and remove Work Lists by name.
	/// This class is thread-safe.
	/// </summary>
	public sealed class WorkListRegistry : IWorkListRegistry
	{
		private static volatile WorkListRegistry _instance;
		private static readonly object _singletonLock = new object();
		private static readonly object _registryLock = new object();

		private readonly IDictionary<string, IWorkList> _map;

		private WorkListRegistry()
		{
			_map = new Dictionary<string, IWorkList>();
		}

		public IWorkList Get(string name)
		{
			if (name == null) return null;

			lock (_registryLock)
			{
				return _map.TryGetValue(name, out var value) ? value : null;
			}
		}

		public void Add(IWorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			lock (_registryLock)
			{
				var name = workList.Name;
				if (_map.ContainsKey(name))
					throw new InvalidOperationException($"WorkList by that name already registered: '{name}'");
				_map.Add(name, workList);
			}
		}

		public bool Remove(IWorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			return Remove(workList.Name);
		}

		public bool Remove(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			lock (_registryLock)
			{
				return _map.Remove(name);
			}
		}

		public IList<IWorkList> GetAll()
		{
			lock (_registryLock)
			{
				return _map.Values.ToList();
			}
		}

		public static WorkListRegistry Instance
		{
			get
			{
				// Notice: the "double-check" approach solves thread concurrency problems
				// while avoiding the locking overhead in each access to this property.
				// See: https://msdn.microsoft.com/en-us/library/ff650316.aspx

				if (_instance == null)
				{
					lock (_singletonLock)
					{
						if (_instance == null)
						{
							_instance = new WorkListRegistry();
						}
					}
				}

				return _instance;
			}
		}
	}
}
