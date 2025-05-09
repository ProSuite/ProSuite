using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface IObjectCategoryAttributeConstraintRepository :
		IRepository<ObjectCategoryAttributeConstraint>
	{
		[NotNull]
		IList<ObjectCategoryAttributeConstraint> Get([NotNull] IDdxDataset dataset);

		[NotNull]
		IList<T> Get<T>([NotNull] IDdxDataset dataset)
			where T : ObjectCategoryAttributeConstraint;

		IList<T> Get<T>(DdxModel model)
			where T : ObjectCategoryAttributeConstraint;
	}
}
