using System;
using ProSuite.QA.ServiceManager.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProSuite.QA.SpecificationProviderFile
{
	public class QASpecificationProviderXml : IQASpecificationProvider
	{
		// temporary specification storage - string (xml path)
		private Dictionary<string,string> _availableSpecifications = null;

		private Dictionary<string, string> AvailableSpecifications =>
			_availableSpecifications ?? (_availableSpecifications = ReadSpecifications());

		// TODO reset speclist when folder changes
		private string SpecificationsFolder { get; set; }

		public QASpecificationProviderXml(string specificationsFolder)
		{
			SpecificationsFolder = specificationsFolder;
		}

		private Dictionary<string,string> ReadSpecifications()
		{
			try
			{
				var tempData = new Dictionary<string,string>();

				List<string> files =
					new List<string>(Directory.EnumerateFiles(SpecificationsFolder));
				foreach (var file in files)
				{
					var specName = Path.GetFileNameWithoutExtension(file).Replace(".qa", "");
					if (!tempData.ContainsKey(specName))
					{
						tempData.Add(specName,file);
					}
				}
				return tempData;
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public IList<string> GetQASpecificationNames()
		{
			if (AvailableSpecifications.Count == 0)
				return new List<string>(){ "not available"};

			return AvailableSpecifications.Keys.ToList();
		}

		public string GetQASpecificationsConnection(string name)
		{
			return AvailableSpecifications.FirstOrDefault(spec=>spec.Key == name).Value;
		}
	}
}
