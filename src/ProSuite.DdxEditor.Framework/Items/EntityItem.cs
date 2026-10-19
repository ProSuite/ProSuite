using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Framework.Items
{
	public abstract class EntityItem<E, BASE> : Item, IEntityItem where BASE : Entity
		where E : BASE
	{
		// ReSharper disable once StaticFieldInGenericType
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IRepository<BASE> _repository;
		private int _entityId;
		private bool _isNew;
		private E _newEntity;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityItem&lt;E, BASE&gt;"/> class.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="repository">The repository.</param>
		protected EntityItem([NotNull] E entity, [NotNull] IRepository<BASE> repository)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(repository, nameof(repository));

			if (! entity.IsPersistent)
			{
				_newEntity = entity;
				_isNew = true;
				_entityId = -1;
			}
			else
			{
				_entityId = entity.Id;
			}

			UpdateText(entity);
			_repository = repository;
		}

		#endregion

		public override bool IsNew => _isNew;

		protected override bool CanDeleteCore => true;

		#region IEntityItem Members

		public bool IsBasedOn(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			if (! (entity is E)) //if (! (typeof(E)).IsInstanceOfType(entity))
			{
				return false;
			}

			if (IsNew && ! entity.IsPersistent)
			{
				return true;
			}

			return _entityId == entity.Id;
		}

		int IEntityItem.EntityId => _entityId;

		#endregion

		[CanBeNull]
		public E GetEntity()
		{
			return _isNew
				       ? _newEntity
				       : GetEntity(_entityId);
		}

		protected override void DeleteCore()
		{
			if (IsNew)
			{
				// do nothing
			}
			else
			{
				E entity = GetEntity();
				if (entity != null)
				{
					_repository.Delete(entity);

					_msg.InfoFormat("{0} deleted", Text);
				}
				else
				{
					_msg.WarnFormat("'{0}' no longer exists in the database", Text);
				}
			}
		}

		protected sealed override bool IsValidForPersistenceCore(
			out Notification notification)
		{
			E entity = GetEntity();

			if (entity == null)
			{
				notification = Notification.Valid();
				return true;
			}

			string preparationError = PrepareForValidation(entity);

			notification = entity.ValidateForPersistence();
			IsValidForPersistenceCore(entity, notification);

			if (preparationError != null)
			{
				notification.RegisterMessage(preparationError, Severity.Error);
			}

			return notification.IsValid();
		}

		protected virtual void IsValidForPersistenceCore([NotNull] E entity,
		                                                 [NotNull] Notification notification) { }

		/// <summary>
		/// Brings the entity into a validatable state right before its persistence
		/// validation runs. Invoked inside the same (read-only) transaction as the
		/// validation itself: the entity may be mutated in memory (e.g. assigning a
		/// reference the editor resolves automatically), but nothing may be persisted
		/// here - persistence-time preparation belongs in <see cref="RequestCommitCore"/>.
		/// The default implementation invokes <see cref="PrepareValidationCallback"/>
		/// (if set); subclasses may override instead of using the callback.
		/// </summary>
		/// <returns>An error message when the entity cannot be prepared for persistence
		/// (registered on the validation result, blocking the save), or <c>null</c>.</returns>
		[CanBeNull]
		protected virtual string PrepareForValidation([NotNull] E entity)
		{
			return PrepareValidationCallback?.Invoke(entity);
		}

		/// <summary>
		/// Optional callback invoked by the default <see cref="PrepareForValidation"/>,
		/// inside the same read-only transaction as the pre-save validation, right
		/// before the entity validates. May mutate the entity in memory and return an
		/// error message that blocks the save (e.g. an algorithm-first editor assigning
		/// a transient, not-yet-persisted descriptor here so its "Required" validation
		/// passes). <c>null</c> (the default) leaves validation behavior unchanged.
		/// </summary>
		[CanBeNull]
		public Func<E, string> PrepareValidationCallback { get; set; }

		//protected override void StartEditingCore()
		//{
		//    //if (GetEntity().IsPersistent)
		//    //{
		//    //    RefreshEntity();
		//    //}
		//}

		protected override void DiscardChangesCore()
		{
			if (_isNew)
			{
				// TODO do this in base class?
				Parent?.RemoveChild(this);
			}
			else
			{
				E entity = GetEntity();

				if (entity != null)
				{
					RefreshEntity();
				}
			}
		}

		protected override void RequestCommitCore()
		{
			E entity = GetEntity();

			if (entity != null)
			{
				PreparePersistenceCallback?.Invoke(entity);
			}

			base.RequestCommitCore();

			if (_isNew)
			{
				_repository.Save(_newEntity);

				//_entityId = _newEntity.Id;
				//_newEntity = null;

				//_isNew = false;
			}
		}

		/// <summary>
		/// Optional callback invoked at the start of <see cref="RequestCommitCore"/>,
		/// inside the item's persistence transaction, before the entity is saved (i.e.
		/// before a new entity is inserted, respectively before the pending changes of
		/// an existing entity are flushed). E.g. an algorithm-first editor uses this to
		/// flush its edit session and resolve/assign a persisted descriptor at save
		/// time. <c>null</c> (the default) leaves persistence behavior unchanged.
		/// </summary>
		[CanBeNull]
		public Action<E> PreparePersistenceCallback { get; set; }

		protected override void EndCommitCore()
		{
			base.EndCommitCore();

			if (_isNew)
			{
				// the saved entity was new, the insert succeeded
				_entityId = _newEntity.Id;
				_newEntity = null;

				_isNew = false;
			}

			RefreshEntity();
		}

		protected override void OnChanged(EventArgs e)
		{
			// update the text
			E entity = GetEntity();

			UpdateText(entity);
			UpdateDescription(entity);
			UpdateItemStateCore(entity);

			base.OnChanged(e);
		}

		protected virtual void UpdateItemStateCore(E entity) { }

		[NotNull]
		protected virtual string GetText([NotNull] E entity)
		{
			var named = entity as INamed;
			return named != null
				       ? named.Name
				       : "<no name defined>";
		}

		[CanBeNull]
		protected virtual string GetDescription([NotNull] E entity)
		{
			var annotated = entity as IAnnotated;
			return annotated != null
				       ? annotated.Description
				       : string.Empty;
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new ShowEntityPropertiesCommand<E, BASE>(this, applicationController));
		}

		private void UpdateText(E entity)
		{
			SetText(entity == null
				        ? "<entity no longer exists>"
				        : GetText(entity));
		}

		private void UpdateDescription([CanBeNull] E entity)
		{
			if (entity == null)
			{
				SetText("<entity no longer exists>");
			}
			else
			{
				SetDescription(GetDescription(entity));
			}
		}

		[CanBeNull]
		private E GetEntity(int id)
		{
			return (E) _repository.Get(id);
		}

		private void RefreshEntity()
		{
			E entity = GetEntity();

			if (entity != null)
			{
				_repository.Refresh(entity);
			}
		}
	}
}
