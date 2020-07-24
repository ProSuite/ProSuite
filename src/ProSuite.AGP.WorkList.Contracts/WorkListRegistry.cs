using System;
using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// This singleton class serves as a registry for WorkLists:
	/// you can add, lookup, and remove Work Lists by name.
	/// This class is thread-safe.
	/// </summary>
	public sealed class WorkListRegistry
	{
		private static volatile WorkListRegistry _instance;
		private static readonly object _singletonLock = new object();
		private static readonly object _registryLock = new object();

		private readonly IDictionary<string, WorkList> _map;

		private WorkListRegistry()
		{
			_map = new Dictionary<string, WorkList>();
		}

		public WorkList Get(string name)
		{
			if (name == null) return null;

			lock (_registryLock)
			{
				return _map.TryGetValue(name, out var value) ? value : null;
			}
		}

		public void Add(WorkList workList)
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

		public void Remove(WorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			Remove(workList.Name);
		}

		public void Remove(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			lock (_registryLock)
			{
				if (!_map.Remove(name))
				{
					throw new InvalidOperationException($"No such WorkList: '{name}'");
				}
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
