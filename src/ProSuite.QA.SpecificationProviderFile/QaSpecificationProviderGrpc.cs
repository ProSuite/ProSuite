using System.Collections.Generic;
using System.Linq;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.QA.ServiceManager.Interfaces;

namespace ProSuite.QA.SpecificationProviderFile
{
	public class QaSpecificationProviderGrpc : IQASpecificationProvider
	{
		private readonly IQualityVerificationEnvironment _verificationEnvironment;

		public QaSpecificationProviderGrpc(
			IQualityVerificationEnvironment verificationEnvironment)
		{
			_verificationEnvironment = verificationEnvironment;
		}

		public IList<string> GetQASpecificationNames()
		{
			return _verificationEnvironment.QualitySpecifications.Select(qs => qs.Name).ToList();
		}

		public string GetQASpecificationsConnection(string name)
		{
			return null;
		}
	}
}
