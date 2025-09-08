using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class WorkListRegistry : IWorkListRegistry
	{
		private static volatile WorkListRegistry _instance;
		private static readonly object _singletonLock = new object();

		private readonly IDictionary<string, IWorkListFactory> _map =
			new ConcurrentDictionary<string, IWorkListFactory>();

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
			return _map.TryGetValue(name, out IWorkListFactory factory) ? factory.Get() : null;
		}

		[ItemCanBeNull]
		public async Task<IWorkList> GetAsync(string name)
		{
			IWorkListFactory factory;

			bool exists = _map.TryGetValue(name, out factory);

			if (exists)
			{
				return await factory.GetAsync();
			}

			return null;
		}

		public IEnumerable<IWorkList> Get()
		{
			ICollection<IWorkListFactory> factories = _map.Values;

			foreach (IWorkListFactory factory in factories)
			{
				yield return factory.Get();
			}
		}

		public async IAsyncEnumerable<IWorkList> GetAsync()
		{
			ICollection<IWorkListFactory> factories = _map.Values;

			foreach (IWorkListFactory factory in factories)
			{
				yield return await factory.GetAsync();
			}
		}

		public void Add(IWorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			string name = workList.Name;
			if (_map.ContainsKey(name))
			{
				throw new InvalidOperationException(
					$"WorkList by that name already registered: '{name}'");
			}

			_map.Add(name, new WorkListFactory(workList));
		}

		public void Add(IWorkListFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			string name = factory.Name;
			if (_map.ContainsKey(name))
			{
				throw new InvalidOperationException(
					$"WorkList by that name already registered: '{name}'");
			}

			_map.Add(name, factory);
		}

		public bool TryAdd(IWorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			if (_map.ContainsKey(workList.Name))
			{
				return false;
			}

			Add(new WorkListFactory(workList));
			return true;
		}

		public bool TryAdd(IWorkListFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			if (_map.ContainsKey(factory.Name))
			{
				return false;
			}

			Add(factory);
			return true;
		}

		public bool WorklistExists(string name)
		{
			// NOTE: This has been observed to deadlock between CIM-threads (without background loading)!
			//       Never lock on somthing you cannot cantrol who has access to
			//lock (_registryLock)
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
			if (_map.TryGetValue(worklist.Name, out IWorkListFactory factory))
			{
				factory.UnWire();

				_map[worklist.Name] = new WorkListFactory(worklist);
			}
			else
			{
				_map.Add(worklist.Name, new WorkListFactory(worklist));
			}

			return true;
		}

		public bool Remove(IWorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			return Remove(workList.Name);
		}

		public bool Remove(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			UnWire(name);

			return _map.Remove(name);
		}

		public void UnWire(IWorkList workList)
		{
			if (workList == null)
				throw new ArgumentNullException(nameof(workList));

			UnWire(workList.Name);
		}

		public void UnWire(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_map.TryGetValue(name, out IWorkListFactory factory);

			factory?.UnWire();
		}

		public override string ToString()
		{
			return $"{_map.Count}";
		}
	}
}
