using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.ServiceManager.Interfaces;

namespace ProSuite.QA.SpecificationProviderFile
{
	public class QaSpecificationProviderGrpc : IQualitySpecificationReferencesProvider
	{
		private readonly IQualityVerificationEnvironment _verificationEnvironment;

		public QaSpecificationProviderGrpc(
			IQualityVerificationEnvironment verificationEnvironment)
		{
			_verificationEnvironment = verificationEnvironment;
		}

		public string BackendDisplayName => throw new System.NotImplementedException();

		public bool CanGetSpecifications()
		{
			return _verificationEnvironment.QualitySpecifications.Any();
		}

		public async Task<IQualitySpecificationReference> GetQualitySpecification(string name)
		{
			return _verificationEnvironment.QualitySpecifications.FirstOrDefault(spec => spec.Name == name);
		}

		public async Task<IList<IQualitySpecificationReference>> GetQualitySpecifications()
		{
			return _verificationEnvironment.QualitySpecifications;
		}
	}
}
