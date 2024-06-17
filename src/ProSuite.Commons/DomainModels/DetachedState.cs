using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.DomainModels
{
	public class DetachedState : IDetachedState
	{
		private readonly IEnumerable<Entity> _entities;

		public DetachedState(Entity entity)
		{
			_entities = new[] { entity };
		}

		public DetachedState(IEnumerable<Entity> entities)
		{
			_entities = entities;
		}

		public DetachedState(params Entity[] entities)
		{
			_entities = entities;
		}

		public void ReattachState(IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			foreach (Entity entity in _entities)
			{
				unitOfWork.Reattach(entity);
			}
		}
	}
}
