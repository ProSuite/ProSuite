using System;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Xml
{
	public class XmlSettingsPersister<T> : ISettingsPersister<T> where T : class, new()
	{
		[NotNull] private readonly string _filePath;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public XmlSettingsPersister([NotNull] string directory, [NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.ArgumentCondition(Directory.Exists(directory),
			                         "Directory does not exist: {0}",
			                         (object) directory);

			_filePath = Path.Combine(directory, fileName);
		}

		public T Read()
		{
			if (! File.Exists(_filePath))
			{
				return new T();
			}

			var serializer = new XmlSerializationHelper<T>();

			try
			{
				return serializer.ReadFromFile(_filePath);
			}
			catch (Exception e)
			{
				_msg.WarnFormat("Error reading options from file: {0}", e.Message);

				return new T();
			}
		}

		public void Write(T settings)
		{
			var serializer = new XmlSerializationHelper<T>();

			serializer.SaveToFile(settings, _filePath);
		}
	}
}