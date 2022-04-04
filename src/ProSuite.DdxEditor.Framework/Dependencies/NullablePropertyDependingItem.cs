using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Dependencies
{
	public class NullablePropertyDependingItem : PropertyDependingItem
	{
		private readonly Entity _entity;
		private readonly Action _setPropertyToNull;
		private readonly IUnitOfWork _unitOfWork;

		public NullablePropertyDependingItem([NotNull] Entity entity,
		                                     [CanBeNull] string entityName,
		                                     [NotNull] string propertyName,
		                                     [NotNull] Action setPropertyToNull,
		                                     [NotNull] IUnitOfWork unitOfWork)
			: base(entity, entityName, propertyName)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(setPropertyToNull, nameof(setPropertyToNull));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_entity = entity;
			_setPropertyToNull = setPropertyToNull;
			_unitOfWork = unitOfWork;
		}

		public override bool RemovedByCascadingDeletion => false;

		protected override void RemoveDependencyCore()
		{
			_unitOfWork.Reattach(_entity);

			_setPropertyToNull();
		}
	}
}
