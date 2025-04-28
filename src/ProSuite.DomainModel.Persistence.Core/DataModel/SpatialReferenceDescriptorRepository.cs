using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class SpatialReferenceDescriptorRepository :
		NHibernateRepository<SpatialReferenceDescriptor>, ISpatialReferenceDescriptorRepository
	{
		#region ISpatialReferenceDescriptorRepository Members

		public SpatialReferenceDescriptor Get([NotNull] string name)
		{
			return GetUniqueResult("Name", name, true);
		}

		#endregion
	}
}
