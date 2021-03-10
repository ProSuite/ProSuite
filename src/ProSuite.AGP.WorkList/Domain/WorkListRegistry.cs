using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public class WorkListRegistry : IWorkListRegistry
	{
		private static volatile WorkListRegistry _instance;
		private static readonly object _singletonLock = new object();
		private static readonly object _registryLock = new object();

		private readonly IDictionary<string, IWorkListFactory> _map =
			new Dictionary<string, IWorkListFactory>();

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

		public IWorkList Get(string name)
		{
			lock (_registryLock)
			{
				return _map.TryGetValue(name, out IWorkListFactory factory) ? factory.Get() : null;
			}
		}

		public void Add(IWorkList workList)
		{
			if (workList == null)
			{
				throw new ArgumentNullException(nameof(workList));
			}

			lock (_registryLock)
			{
				string name = workList.Name;
				if (_map.ContainsKey(name))
				{
					throw new InvalidOperationException(
						$"WorkList by that name already registered: '{name}'");
				}

				_map.Add(name, new WorkListFactory(workList));
			}
		}

		public void Add(IWorkListFactory factory)
		{
			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}

			lock (_registryLock)
			{
				string name = factory.Name;
				if (_map.ContainsKey(name))
				{
					throw new InvalidOperationException(
						$"WorkList by that name already registered: '{name}'");
				}

				_map.Add(name, factory);
			}
		}

		public bool TryAdd(IWorkListFactory factory)
		{
			lock (_registryLock)
			{
				if (_map.ContainsKey(factory.Name))
				{
					return false;
				}
			}

			Add(factory);
			return true;
		}

		public bool TryAdd(IWorkList worklist)
		{
			lock (_registryLock)
			{
				if (_map.ContainsKey(worklist.Name))
				{
					return false;
				}
			}

			Add(worklist);
			return true;
		}

		public bool Remove(IWorkList workList)
		{
			if (workList == null)
			{
				throw new ArgumentNullException(nameof(workList));
			}

			return Remove(workList.Name);
		}

		public bool Remove(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			lock (_registryLock)
			{
				return _map.Remove(name);
			}
		}

		public IEnumerable<string> GetNames()
		{
			lock (_registryLock)
			{
				return _map.Keys.ToList();
			}
		}

		public bool Exists(string name)
		{
			lock (_registryLock)
			{
				if (String.IsNullOrEmpty(name))
					return false;
				return _map.ContainsKey(name);
			}
		}
	}
}
