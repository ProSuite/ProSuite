using System;
using System.Reflection;
using NHibernate;
using NHibernate.Type;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class NHibernateInterceptor : EmptyInterceptor
	{
		[CanBeNull] private ISession _session;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private string _lastStack;

		[UsedImplicitly]
		public bool LogLoadOutsideTransaction { get; set; } = true;

		public override void SetSession(ISession session)
		{
			_session = session;

			base.SetSession(session);
		}

		public override bool OnLoad(object entity, object id, object[] state,
		                            string[] propertyNames, IType[] types)
		{
			if (LogLoadOutsideTransaction)
			{
				if (_session != null && _session.IsOpen && ! _session.Transaction.IsActive)
				{
					string stack = GetStack();

					if (! Equals(_lastStack, stack))
					{
						_msg.DebugFormat(
							"IInterceptor.OnLoad() outside of transaction (type: {0}){1}{1}{2}",
							entity?.GetType().Name ?? "<entity is null>",
							Environment.NewLine, stack);
						_lastStack = stack;
					}
				}
			}

			return base.OnLoad(entity, id, state, propertyNames, types);
		}

		public override bool OnSave(object entity, object id, object[] state,
		                            string[] propertyNames, IType[] types)
		{
			var persistenceAware = entity as IPersistenceAware;
			persistenceAware?.OnCreate();

			var metadata = entity as IEntityMetadata;
			if (metadata != null)
			{
				EntityMetadataUtils.DocumentCreation(metadata, state, propertyNames);
			}

			return base.OnSave(entity, id, state, propertyNames, types);
		}

		public override bool OnFlushDirty(object entity, object id, object[] currentState,
		                                  object[] previousState, string[] propertyNames,
		                                  IType[] types)
		{
			var persistenceAware = entity as IPersistenceAware;
			persistenceAware?.OnUpdate();

			if (previousState == null)
			{
				_msg.DebugFormat("Dirty flushing with unknown previous state ({0})", entity);

				string stack = GetStack();

				_msg.DebugFormat(
					"IInterceptor.OnFlushDirty() found detached entity (type: {0}){1}{1}{2}",
					entity?.GetType().Name ?? "<entity is null>",
					Environment.NewLine, stack);
			}
			else
			{
				var metadata = entity as IEntityMetadata;
				if (metadata != null)
				{
					EntityMetadataUtils.DocumentUpdate(metadata, currentState, previousState,
					                                   propertyNames);
				}
			}

			// NOTE: collections are also reported, but the referenced entities have the *new* state 
			// in both currentState and previousState

			return base.OnFlushDirty(entity, id, currentState, previousState,
			                         propertyNames, types);
		}

		public override void OnDelete(object entity, object id, object[] state,
		                              string[] propertyNames, IType[] types)
		{
			var persistenceAware = entity as IPersistenceAware;
			persistenceAware?.OnDelete();

			base.OnDelete(entity, id, state, propertyNames, types);
		}

		private static string GetStack()
		{
			const int maxLength = 8000;
			const int start = 216;
			string stack = Environment.StackTrace;

			stack = stack.Length > start + maxLength
				        ? stack.Substring(start, maxLength) + " ..."
				        : stack.Substring(start);

			return stack;
		}
	}
}
