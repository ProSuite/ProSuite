using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class WorkListRegistry : IWorkListRegistry
	{
		private static volatile WorkListRegistry _instance;
		private static readonly object _singletonLock = new object();
		private static readonly object _registryLock = new object();

		private readonly IDictionary<string, IWorkListFactory> _map =
			new Dictionary<string, IWorkListFactory>();

		public static IWorkListRegistry Instance
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

		[CanBeNull]
		public IWorkList Get(string name)
		{
			lock (_registryLock)
			{
				return _map.TryGetValue(name, out IWorkListFactory factory) ? factory.Get() : null;
			}
		}

		[ItemCanBeNull]
		public async Task<IWorkList> GetAsync(string name)
		{
			bool exists;
			IWorkListFactory factory;

			lock (_registryLock)
			{
				exists = _map.TryGetValue(name, out factory);
			}

			if (exists)
			{
				return await factory.GetAsync();
			}

			return null;
		}

		public IEnumerable<IWorkList> Get()
		{
			ICollection<IWorkListFactory> factories;

			lock (_registryLock)
			{
				factories = _map.Values;
			}

			foreach (IWorkListFactory factory in factories)
			{
				yield return factory.Get();
			}
		}

		public async IAsyncEnumerable<IWorkList> GetAsync()
		{
			ICollection<IWorkListFactory> factories;

			lock (_registryLock)
			{
				factories = _map.Values;
			}

			foreach (IWorkListFactory factory in factories)
			{
				yield return await factory.GetAsync();
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

		public bool WorklistExists(string name)
		{
			lock (_registryLock)
			{
				if (_map.TryGetValue(name, out IWorkListFactory factory))
				{
					// In this case the work list has been created.
					// XmlBasedWorkListFactory would create it in a non-canonical way (no schema info etc.)
					// which might be fine for layer display purposes, but not for the NavigatorView. 
					return factory is WorkListFactoryBase;
				}

				return false;
			}
		}

		public bool AddOrReplace(IWorkList worklist)
		{
			lock (_registryLock)
			{
				if (_map.ContainsKey(worklist.Name))
				{
					_map[worklist.Name] = new WorkListFactory(worklist);
				}
				else
				{
					_map.Add(worklist.Name, new WorkListFactory(worklist));
				}
			}

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

		public bool Contains(string name)
		{
			lock (_registryLock)
			{
				return ! string.IsNullOrEmpty(name) && _map.ContainsKey(name);
			}
		}

		public override string ToString()
		{
			lock (_registryLock)
			{
				return $"{_map.Count}";
			}
		}
	}
}
