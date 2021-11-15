using ProSuite.DomainModel.Core.QA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.QA.SpecificationProviderFile
{

	public class QASpecificationProviderXml : IQualitySpecificationReferencesProvider
	{
		public string BackendDisplayName => SpecificationsFolder;
		public bool CanGetSpecifications()
		{
			return AvailableSpecifications?.Any() == true;
		}

		public async Task<IList<IQualitySpecificationReference>> GetQualitySpecifications()
		{
			return AvailableSpecifications.Values.ToList();
		}

		public async Task<IQualitySpecificationReference> GetQualitySpecification(string name)
		{
			return AvailableSpecifications.FirstOrDefault(spec => spec.Key == name).Value;
		}

		private string SpecificationsFolder { get; set; }
		private IDictionary<string, IQualitySpecificationReference> _availableSpecifications;

		private IDictionary<string, IQualitySpecificationReference> AvailableSpecifications =>
			_availableSpecifications ?? (_availableSpecifications = ReadSpecifications());

		public QASpecificationProviderXml(string specificationsFolder)
		{
			SpecificationsFolder = specificationsFolder;
		}
		
		private IDictionary<string, IQualitySpecificationReference> ReadSpecifications()
		{
			try
			{
				var tempData = new Dictionary<string, IQualitySpecificationReference>();

				List<string> files =
					new List<string>(Directory.EnumerateFiles(SpecificationsFolder));
				foreach (var file in files)
				{
					var specName = Path.GetFileNameWithoutExtension(file).Replace(".qa", "");
					if (!tempData.ContainsKey(specName))
					{
						tempData.Add(specName, new QualitySpecificationReference(-1, specName, file));
					}
				}
				return tempData;
			}
			catch (Exception)
			{
				return null;
			}
		}

	}

}
