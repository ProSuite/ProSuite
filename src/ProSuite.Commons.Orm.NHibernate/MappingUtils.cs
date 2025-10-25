using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using NHibernate.Cfg.MappingSchema;

namespace ProSuite.Commons.Orm.NHibernate
{
	public static class MappingUtils
	{
		private const string MappingFileExtension = ".hbm.xml";

		/// <summary>
		/// Write the given NH mappings to individual files in the given folder path.
		/// All existing .hbm.xml files WILL BE DELETED.
		/// </summary>
		/// <remarks>NHibernate has the same, but you cannot specify the target path</remarks>
		public static void WriteAllXmlMapping(this IEnumerable<HbmMapping> mappings, string folderPath)
		{
			if (mappings is null)
				throw new ArgumentNullException(nameof(mappings));
			if (string.IsNullOrEmpty(folderPath))
				throw new ArgumentNullException(nameof(folderPath));

			PrepareFolderPath(folderPath);

			foreach (HbmMapping mapping in mappings)
			{
				string fileName = GetFileName(mapping);
				string document = Serialize(mapping);
				File.WriteAllText(Path.Combine(folderPath, fileName), document);
			}
		}

		private static void PrepareFolderPath(string folderPath)
		{
			if (Directory.Exists(folderPath))
			{
				var pattern = $"*{MappingFileExtension}";

				foreach (var path in Directory.GetFiles(folderPath, pattern))
				{
					File.Delete(path);
				}
			}
			else
			{
				Directory.CreateDirectory(folderPath);
			}
		}

		private static string GetFileName(HbmMapping mapping) // lifted from NHibernate
		{
			string name = "MyMapping";

			HbmClass rc = mapping.RootClasses.FirstOrDefault();
			if (rc != null)
			{
				name = rc.Name;
			}

			HbmSubclass sc = mapping.SubClasses.FirstOrDefault();
			if (sc != null)
			{
				name = sc.Name;
			}

			HbmJoinedSubclass jc = mapping.JoinedSubclasses.FirstOrDefault();
			if (jc != null)
			{
				name = jc.Name;
			}

			HbmUnionSubclass uc = mapping.UnionSubclasses.FirstOrDefault();
			if (uc != null)
			{
				name = uc.Name;
			}

			return name + MappingFileExtension;
		}

		private static string Serialize(HbmMapping mapping)
		{
			var settings = new XmlWriterSettings { Indent = true };
			var serializer = new XmlSerializer(typeof(HbmMapping));

			using (var stream = new MemoryStream(2048))
			{
				using (XmlWriter writer = XmlWriter.Create(stream, settings))
				{
					serializer.Serialize(writer, mapping);
				}
				stream.Position = 0; // rewind
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}
