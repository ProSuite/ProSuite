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
						// ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
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
			return _map.TryGetValue(name, out IWorkListFactory factory) ? factory.Get() : null;
		}

		[ItemCanBeNull]
		public async Task<IWorkList> GetAsync(string name)
		{
			bool exists = _map.TryGetValue(name, out IWorkListFactory factory);

			if (exists)
			{
				return await factory.GetAsync();
			}

			return null;
		}

		public bool TryGet<T>(string name, out T workList) where T : class, IWorkList
		{
			foreach (var kvp in _map)
			{
				if (kvp.Value is not WorkListFactoryBase workListFactory)
				{
					// Not registered with an actual factory
					continue;
				}

				if (! workListFactory.IsWorkListCreated)
				{
					// Not instantiated
					continue;
				}

				T workListWithDesiredType = GetWorkList<T>(workListFactory);

				if (! string.IsNullOrEmpty(name) && ! string.Equals(kvp.Key, name))
				{
					// Name does not match
					continue;
				}

				if (workListWithDesiredType == null)
				{
					continue;
				}

				workList = workListWithDesiredType;
				return true;
			}

			workList = null;

			return false;
		}

		public bool WorklistExists(string name)
		{
			// NOTE: This has been observed to deadlock between CIM-threads (without background loading)!
			//       Never lock on something you cannot control who has access to
			//lock (_registryLock)
			if (_map.TryGetValue(name, out IWorkListFactory factory))
			{
				// In this case the work list has been created.
				// XmlBasedWorkListFactory would create it in a non-canonical way (no schema info etc.)
				// which might be fine for layer display purposes, but not for the NavigatorView. 
				return factory is WorkListFactoryBase workListFactory &&
				       workListFactory.IsWorkListCreated;
			}

			return false;
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

		public bool AddOrReplace(IWorkList worklist)
		{
			if (_map.TryGetValue(worklist.Name, out IWorkListFactory factory))
			{
				// NOTE: The saving in UnWire can result in 'The process cannot access the file
				// because it is being used by another process'!

				if (ReferenceEquals(factory.Get(), worklist))
				{
					return false;
				}

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

		private void Add(IWorkListFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			string name = factory.Name;

			if (! _map.TryAdd(name, factory))
			{
				throw new InvalidOperationException(
					$"WorkList by that name already registered: '{name}'");
			}
		}

		[CanBeNull]
		private static T GetWorkList<T>([NotNull] WorkListFactoryBase workListFactory)
			where T : class, IWorkList
		{
			IWorkList candidate = workListFactory.Get();

			return candidate as T;
		}

		private bool Remove(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			UnWire(name);

			return _map.Remove(name);
		}

		private void UnWire(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_map.TryGetValue(name, out IWorkListFactory factory);

			factory?.UnWire();
		}

		public override string ToString()
		{
			return $"Work List Registry ({_map.Count}) registered work lists";
		}
	}
}
