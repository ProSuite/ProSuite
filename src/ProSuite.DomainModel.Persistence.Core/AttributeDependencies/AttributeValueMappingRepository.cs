using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.AttributeDependencies
{
	[UsedImplicitly]
	public class AttributeValueMappingRepository :
		NHibernateRepository<AttributeValueMapping>, IAttributeValueMappingRepository { }
}
