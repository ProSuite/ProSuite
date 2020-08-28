using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ArcGIS.Desktop.Internal.Mapping.Locate;

namespace ProSuite.Commons.AGP.Storage
{
	public abstract class XmlRepository<T> : IRepository<T> where T : class
	{
		private readonly IList<T> items;

		protected XmlRepository(string filename)
		{
			FileName = filename;

			if (File.Exists(FileName))
			{
				XmlSerializer serializer =
					new XmlSerializer(typeof(List<T>), new XmlRootAttribute("Items"));
				using (StreamReader myWriter = new StreamReader(FileName))
				{
					items = (IList<T>) serializer.Deserialize(myWriter);
					myWriter.Close();
				}
			}
			else
				items = new List<T>();
		}

		internal string FileName { get; private set; }

		public void SaveChanges()
		{
			if (items.Count == 0) return;

			if (! File.Exists(FileName))
				File.CreateText(FileName);

			XmlSerializer serializer = new XmlSerializer(items.GetType(), new XmlRootAttribute("Items"));
			using (StreamWriter myWriter = new StreamWriter(FileName))
			{
				serializer.Serialize(myWriter, items);
				myWriter.Close();
			}
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
	}

}
