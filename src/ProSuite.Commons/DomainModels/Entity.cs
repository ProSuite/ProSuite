using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.DomainModels
{
	public abstract class Entity : IEntity, IEntityTest
	{
		// must not be readonly, updated after construction via reflection
		[UsedImplicitly] private int _id = _unsavedValue;

		private const int _unsavedValue = -1;

		public int Id
		{
			get { return _id; }
		}

		public bool IsPersistent
		{
			get { return _id != _unsavedValue; }
		}

		[NotNull]
		public Notification ValidateForPersistence()
		{
			Notification notification = Validator.ValidateObject(this);

			ValidateForPersistenceCore(notification);

			return notification;
		}

		protected virtual void ValidateForPersistenceCore([NotNull] Notification notification) { }

		#region IEntityTest Members

		void IEntityTest.SetId(int id)
		{
			_id = id;
		}

		#endregion
	}
}