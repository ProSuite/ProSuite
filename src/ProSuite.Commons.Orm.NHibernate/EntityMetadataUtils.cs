using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	public static class EntityMetadataUtils
	{
		private const string _createdByUserProperty = "CreatedByUser";
		private const string _createdDateProperty = "CreatedDate";
		private const string _lastChangedByUserProperty = "LastChangedByUser";
		private const string _lastChangedDateProperty = "LastChangedDate";

		public static void DocumentCreation([NotNull] IEntityMetadata metadata,
		                                    [NotNull] IList<object> state,
		                                    [NotNull] IList<string> propertyNames)
		{
			Assert.ArgumentNotNull(metadata, nameof(metadata));

			var newState = new NewEntityState(state, propertyNames);

			if (metadata.CreatedByUser == null)
			{
				metadata.CreatedByUser = metadata.LastChangedByUser ?? GetUserName();
				newState.Update(_createdByUserProperty, metadata.CreatedByUser);
			}

			if (metadata.CreatedDate == null)
			{
				metadata.CreatedDate = metadata.LastChangedDate ?? DateTime.Now;
				newState.Update(_createdDateProperty, metadata.CreatedDate);
			}

			if (metadata.LastChangedByUser == null)
			{
				metadata.LastChangedByUser = metadata.CreatedByUser;
				newState.Update(_lastChangedByUserProperty, metadata.LastChangedByUser);
			}

			if (metadata.LastChangedDate == null)
			{
				metadata.LastChangedDate = metadata.CreatedDate;
				newState.Update(_lastChangedDateProperty, metadata.LastChangedDate);
			}
		}

		public static void DocumentUpdate([NotNull] IEntityMetadata metadata,
		                                  [NotNull] IList<object> newValues,
		                                  [CanBeNull] IList<object> oldValues,
		                                  [NotNull] IList<string> propertyNames)
		{
			Assert.ArgumentNotNull(metadata, nameof(metadata));
			Assert.ArgumentNotNull(newValues, nameof(newValues));
			Assert.ArgumentNotNull(propertyNames, nameof(propertyNames));

			var changes = new UpdatedEntityState(newValues, oldValues, propertyNames);

			if (changes.OldValuesAreKnown)
			{
				if (changes.IsKnownUpdated(_lastChangedByUserProperty) ||
				    changes.IsKnownUpdated(_lastChangedDateProperty))
				{
					// one or both of the properties were explicitly assigned as part of the update, keep them
					return;
				}
			}

			string userName = GetUserName();
			DateTime now = DateTime.Now;

			metadata.LastChangedByUser = userName;
			metadata.LastChangedDate = now;

			changes.Update(_lastChangedDateProperty, metadata.LastChangedDate);
			changes.Update(_lastChangedByUserProperty, metadata.LastChangedByUser);
		}

		[NotNull]
		private static string GetUserName()
		{
			return EnvironmentUtils.UserDisplayName;
		}

		#region Nested types

		private abstract class EntityState
		{
			private readonly IList<object> _values;
			private readonly IList<string> _propertyNames;

			protected EntityState([NotNull] IList<object> values,
			                      [NotNull] IList<string> propertyNames)
			{
				Assert.ArgumentNotNull(values, nameof(values));
				Assert.ArgumentNotNull(propertyNames, nameof(propertyNames));
				Assert.ArgumentCondition(values.Count == propertyNames.Count,
				                         "Number of values and property names must match");

				_values = values;
				_propertyNames = propertyNames;
			}

			public void Update([NotNull] string propertyName,
			                   [CanBeNull] object newValue)
			{
				int index = _propertyNames.IndexOf(propertyName);

				if (index < 0)
				{
					return;
				}

				_values[index] = newValue;
			}

			[CanBeNull]
			protected object GetValue(int index)
			{
				return _values[index];
			}

			protected int GetIndex([NotNull] string propertyName)
			{
				return _propertyNames.IndexOf(propertyName);
			}

			protected int PropertyCount => _propertyNames.Count;
		}

		private class NewEntityState : EntityState
		{
			public NewEntityState([NotNull] IList<object> values,
			                      [NotNull] IList<string> propertyNames)
				: base(values, propertyNames) { }
		}

		private class UpdatedEntityState : EntityState
		{
			[CanBeNull] private readonly IList<object> _oldValues;

			public UpdatedEntityState([NotNull] IList<object> newValues,
			                          [CanBeNull] IList<object> oldValues,
			                          [NotNull] IList<string> propertyNames)
				: base(newValues, propertyNames)
			{
				Assert.ArgumentCondition(oldValues == null || oldValues.Count == PropertyCount,
				                         "Number of values and property names must match");

				_oldValues = oldValues;
			}

			public bool OldValuesAreKnown => _oldValues != null;

			public bool IsKnownUpdated([NotNull] string propertyName)
			{
				if (_oldValues == null)
				{
					return false;
				}

				int index = GetIndex(propertyName);

				if (index < 0)
				{
					return false;
				}

				return ! Equals(GetValue(index), _oldValues?[index]);
			}
		}

		#endregion
	}
}
