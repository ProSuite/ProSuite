using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class DatasetCategoryRepository : NHibernateRepository<DatasetCategory>,
	                                         IDatasetCategoryRepository
	{
		#region IDatasetCategoryRepository Members

		public DatasetCategory Get([NotNull] string name)
		{
			return GetUniqueResult("Name", name, true);
		}

		public DatasetCategory GetByAbbreviation([NotNull] string abbreviation)
		{
			return GetUniqueResult("Abbreviation", abbreviation, true);
		}

		#endregion
	}
}
