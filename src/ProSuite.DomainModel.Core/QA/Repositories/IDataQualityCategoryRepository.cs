using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IDataQualityCategoryRepository :
		IRepository<DataQualityCategory>
	{
		[NotNull]
		IList<DataQualityCategory> GetTopLevelCategories();
	}
}
