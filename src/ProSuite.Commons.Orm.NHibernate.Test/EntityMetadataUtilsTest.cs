using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate.Test
{
	[TestFixture]
	public class EntityMetadataUtilsTest
	{
		private const string _createdByUserProperty = "CreatedByUser";
		private const string _createdDateProperty = "CreatedDate";
		private const string _lastChangedByUserProperty = "LastChangedByUser";
		private const string _lastChangedDateProperty = "LastChangedDate";

		private readonly List<string> _properties = new List<string>
		                                            {
			                                            _createdDateProperty,
			                                            _createdByUserProperty,
			                                            _lastChangedDateProperty,
			                                            _lastChangedByUserProperty
		                                            };

		[Test]
		public void CanDocumentCreation()
		{
			var entity = new TestEntity();

			object[] values = CreateValues(entity);
			EntityMetadataUtils.DocumentCreation(entity, values, _properties);

			Assert.AreEqual(GetUserName(), entity.CreatedByUser);
			Assert.AreEqual(GetUserName(), entity.LastChangedByUser);

			Assert.IsNotNull(entity.CreatedDate);
			Assert.AreEqual(entity.CreatedDate, entity.LastChangedDate);

			AssertEqualState(entity, values);
		}

		[Test]
		public void CanDocumentCreationWithExplicitMetadata()
		{
			var createdDate = new DateTime(2000, 1, 1);
			const string createdByUser = "User1";
			var lastChangedDate = new DateTime(2010, 1, 1);
			const string lastChangedByUser = "User2";

			var entity = new TestEntity
			             {
				             CreatedDate = createdDate,
				             CreatedByUser = createdByUser,
				             LastChangedDate = lastChangedDate,
				             LastChangedByUser = lastChangedByUser
			             };

			object[] values = CreateValues(entity);
			EntityMetadataUtils.DocumentCreation(entity, values, _properties);

			Assert.IsNotNull(entity.CreatedDate);
			Assert.IsNotNull(entity.CreatedByUser);
			Assert.IsNotNull(entity.LastChangedDate);
			Assert.IsNotNull(entity.LastChangedByUser);
			Assert.AreEqual(createdDate, entity.CreatedDate);
			Assert.AreEqual(createdByUser, entity.CreatedByUser);
			Assert.AreEqual(lastChangedDate, entity.LastChangedDate);
			Assert.AreEqual(lastChangedByUser, entity.LastChangedByUser);

			AssertEqualState(entity, values);
		}

		[Test]
		public void CanDocumentCreationWithExplicitMetadata_CreationOnly()
		{
			var createdDate = new DateTime(2000, 1, 1);
			const string createdByUser = "User1";

			var entity = new TestEntity
			             {
				             CreatedDate = createdDate,
				             CreatedByUser = createdByUser,
			             };

			object[] values = CreateValues(entity);
			EntityMetadataUtils.DocumentCreation(entity, values, _properties);

			Assert.AreEqual(createdDate, entity.CreatedDate);
			Assert.AreEqual(createdByUser, entity.CreatedByUser);

			// the LastChanged information will be set to the Creation information if missing
			Assert.AreEqual(createdDate, entity.LastChangedDate);
			Assert.AreEqual(createdByUser, entity.LastChangedByUser);

			AssertEqualState(entity, values);
		}

		[Test]
		public void CanDocumentCreationWithExplicitMetadata_CreationDateOnly()
		{
			var createdDate = new DateTime(2000, 1, 1);
			const string createdByUser = null;

			var entity = new TestEntity
			             {
				             CreatedDate = createdDate,
				             CreatedByUser = createdByUser,
			             };

			object[] values = CreateValues(entity);
			EntityMetadataUtils.DocumentCreation(entity, values, _properties);

			Assert.AreEqual(createdDate, entity.CreatedDate);
			Assert.AreEqual(GetUserName(), entity.CreatedByUser);

			// the LastChanged information will be set to the Creation information if missing
			Assert.AreEqual(createdDate, entity.LastChangedDate);
			Assert.AreEqual(GetUserName(), entity.LastChangedByUser);

			AssertEqualState(entity, values);
		}

		[Test]
		public void CanDocumentCreationWithExplicitMetadata_LastChangedOnly()
		{
			var lastChangedDate = new DateTime(2010, 1, 1);
			const string lastChangedByUser = "User2";

			var entity = new TestEntity
			             {
				             LastChangedDate = lastChangedDate,
				             LastChangedByUser = lastChangedByUser,
			             };

			object[] values = CreateValues(entity);
			EntityMetadataUtils.DocumentCreation(entity, values, _properties);

			Assert.AreEqual(lastChangedDate, entity.LastChangedDate);
			Assert.AreEqual(lastChangedByUser, entity.LastChangedByUser);

			// the Created information will be set to the LastChanged information if missing
			Assert.AreEqual(lastChangedDate, entity.CreatedDate);
			Assert.AreEqual(lastChangedByUser, entity.CreatedByUser);

			AssertEqualState(entity, values);
		}

		[Test]
		public void CanDocumentUpdate()
		{
			var entity = new TestEntity();

			object[] oldValues = CreateValues(null, null, null, null);
			object[] newValues = CreateValues(null, null, null, null);

			EntityMetadataUtils.DocumentUpdate(entity, newValues, oldValues, _properties);

			Assert.IsNull(entity.CreatedDate);
			Assert.IsNull(entity.CreatedByUser);
			Assert.IsNotNull(entity.LastChangedDate);
			Assert.IsNotNull(entity.LastChangedByUser);
		}

		[Test]
		public void CanDocumentUpdateWithExplicitMetadata()
		{
			var createdDate = new DateTime(2000, 1, 1);
			const string createdByUser = "User1";
			var lastChangedDate = new DateTime(2010, 1, 1);
			const string lastChangedByUser = "User2";

			var entity = new TestEntity
			             {
				             CreatedDate = createdDate,
				             CreatedByUser = createdByUser,
				             LastChangedDate = lastChangedDate,
				             LastChangedByUser = lastChangedByUser
			             };

			object[] oldValues = CreateValues(null, null, null, null);
			object[] newValues = CreateValues(entity);

			EntityMetadataUtils.DocumentUpdate(entity, newValues, oldValues, _properties);

			Assert.AreEqual(createdDate, entity.CreatedDate);
			Assert.AreEqual(createdByUser, entity.CreatedByUser);
			Assert.AreEqual(lastChangedDate, entity.LastChangedDate);
			Assert.AreEqual(lastChangedByUser, entity.LastChangedByUser);
		}

		[NotNull]
		private static string GetUserName()
		{
			return EnvironmentUtils.UserDisplayName;
		}

		private void AssertEqualState([NotNull] IEntityMetadata entity,
		                              [NotNull] IList<object> state)
		{
			Assert.AreEqual(entity.CreatedByUser,
			                GetValue(_createdByUserProperty, state));

			Assert.AreEqual(entity.LastChangedByUser,
			                GetValue(_lastChangedByUserProperty, state));
			Assert.AreEqual(entity.CreatedDate,
			                GetValue(_createdDateProperty, state));

			Assert.AreEqual(entity.LastChangedByUser,
			                GetValue(_lastChangedByUserProperty, state));
		}

		[CanBeNull]
		private object GetValue([NotNull] string propertyName,
		                        [NotNull] IList<object> values)
		{
			int index = _properties.IndexOf(propertyName);
			Assert.IsTrue(index >= 0, "property name not found");
			return values[index];
		}

		[NotNull]
		private static object[] CreateValues(IEntityMetadata metadata)
		{
			return CreateValues(metadata.CreatedDate, metadata.CreatedByUser,
			                    metadata.LastChangedDate, metadata.LastChangedByUser);
		}

		[NotNull]
		private static object[] CreateValues(DateTime? createdDate,
		                                     [CanBeNull] string createdByUser,
		                                     DateTime? lastChangedDate,
		                                     [CanBeNull] string lastChangedByUser)
		{
			return new object[]
			       {
				       createdDate,
				       createdByUser,
				       lastChangedDate,
				       lastChangedByUser
			       };
		}

		private class TestEntity : IEntityMetadata
		{
			public DateTime? CreatedDate { get; set; }

			public string CreatedByUser { get; set; }

			public DateTime? LastChangedDate { get; set; }

			public string LastChangedByUser { get; set; }
		}
	}
}
