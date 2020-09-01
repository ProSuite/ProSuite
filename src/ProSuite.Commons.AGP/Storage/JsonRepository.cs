using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProSuite.Commons.AGP.Storage
{
	public abstract class JsonRepository<T> : IRepository<T> where T : class
	{
		private readonly IList<T> items;
		private readonly string _filename;
		public readonly JsonConverter _jsonConverter;

		protected JsonRepository(string filename , JsonConverter jsonConverter = null)
		{
			_filename = filename;
			_jsonConverter = jsonConverter;
			if (File.Exists(_filename))
			{
				string jsonText = File.ReadAllText(_filename);
				if (! string.IsNullOrEmpty(jsonText))
				{
					items = (_jsonConverter == null) ?
						        JsonConvert.DeserializeObject<IList<T>>(jsonText) :
						        JsonConvert.DeserializeObject<IList<T>>(jsonText, _jsonConverter);
				}
			}
			items = items ?? new List<T>();
		}

		public IQueryable<T> GetAll()
		{
			return items.AsQueryable();
		}

		public void Add(T item)
		{
			items.Add(item);
		}

		public void Update(T item)
		{
		}

		public void Delete(T item)
		{
			items.Remove(item);
		}

		public void SaveChanges()
		{
			if (items.Count == 0) return;

			if (!File.Exists(_filename))
				File.CreateText(_filename);

			string jsonText = (_jsonConverter == null) ?
				                  (JsonConvert.SerializeObject(items)) :
				                  (JsonConvert.SerializeObject(items, _jsonConverter));

			//File.WriteAllText(_filename,jsonText);
		}
	}
}
