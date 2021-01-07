using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[UsedImplicitly]
	public class AttributeRepository : NHibernateRepository<Attribute>,
	                                   IAttributeRepository
	{
		#region IAttributeRepository Members

		public IList<T> GetAll<T>() where T : Attribute
		{
			return GetAllCore<T>();
		}

		public IList<ObjectAttribute> Get([NotNull] AttributeType type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              " from ObjectAttribute att " +
					              "where att.ObjectAttributeType = :type")
				              .SetEntity("type", type)
				              .List<ObjectAttribute>();
			}
		}

		#endregion
	}
}
