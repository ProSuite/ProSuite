using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ProSuite.Commons.AGP.Storage
{
	public abstract class XmlRepository<T> : IRepository<T> where T : class
	{
		private readonly IList<T> items;

		protected XmlRepository(string filename)
		{
			FileName = filename;

			XmlSerializer serializer =
				new XmlSerializer(typeof(List<T>), new XmlRootAttribute("Items"));
			using (StreamReader myWriter = new StreamReader(FileName))
			{
				items = (IList<T>) serializer.Deserialize(myWriter);
				myWriter.Close();
			}
		}

		internal string FileName { get; private set; }

		public void SaveChanges()
		{
			XmlSerializer serializer =
				new XmlSerializer(items.GetType(), new XmlRootAttribute("Items"));
			using (StreamWriter myWriter = new StreamWriter(FileName))
			{
				serializer.Serialize(myWriter, items);
				myWriter.Close();
			}
		}

		public IList<T> GetAll()
		{
			return items;
		}

		public void Add(T item)
		{
			items.Add(item);
		}

		public void Update(T item)
		{
			throw new NotImplementedException();
		}

		public void Delete(T item)
		{
			items.Remove(item);
		}
	}
}
