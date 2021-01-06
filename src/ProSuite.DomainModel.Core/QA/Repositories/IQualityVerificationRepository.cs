using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IQualityVerificationRepository : IRepository<QualityVerification>
	{
		[NotNull]
		IList<QualityVerification> Get([NotNull] QualityCondition qualityCondition);

		[NotNull]
		IEnumerable<QualityVerification> Get([NotNull] DdxModel model);
	}
}
