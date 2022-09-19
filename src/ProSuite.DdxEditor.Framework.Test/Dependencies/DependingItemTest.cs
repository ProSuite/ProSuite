using System;
using System.Reflection;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;

namespace ProSuite.DdxEditor.Framework.Test.Dependencies
{
	[TestFixture]
	public class DependingItemTest
	{
		[Test]
		public void CanDetectEquals()
		{
			DependingItem item1 = new RequiredPropertyDependingItem(CreateEntityA(1), "entity",
				"property");
			DependingItem item2 = new RequiredPropertyDependingItem(CreateEntityA(1), "entity",
				"property");

			Assert.AreEqual(item1, item2);
		}

		[Test]
		public void CanDetectNotEqualsDifferentEntityId()
		{
			DependingItem item1 = new RequiredPropertyDependingItem(CreateEntityA(1), "entity",
				"property");
			DependingItem item2 = new RequiredPropertyDependingItem(CreateEntityA(2), "entity",
				"property");

			Assert.AreNotEqual(item1, item2);
		}

		[Test]
		public void CanDetectNotEqualsDifferentEntityType()
		{
			DependingItem item1 = new RequiredPropertyDependingItem(CreateEntityA(1), "entity",
				"property");
			DependingItem item2 = new RequiredPropertyDependingItem(CreateEntityB(1), "entity",
				"property");

			Assert.AreNotEqual(item1, item2);
		}

		[Test]
		public void CanDetectNotEqualsDifferentItemType()
		{
			DependingItem item1 = new RequiredPropertyDependingItem(CreateEntityA(1), "entity",
				"property");
			DependingItem item2 = new TestDependingItem(CreateEntityA(1), "name");

			Assert.AreNotEqual(item1, item2);
		}

		[Test]
		public void CanDetectNotEqualsDifferentName()
		{
			DependingItem item1 = new RequiredPropertyDependingItem(CreateEntityA(1), "entity",
				"property1");
			DependingItem item2 = new RequiredPropertyDependingItem(CreateEntityA(2), "entity",
				"property2");

			Assert.AreNotEqual(item1, item2);
		}

		#region Non-public methods

		private static TestEntityA CreateEntityA(int id)
		{
			var entity = new TestEntityA();
			SetEntityId(entity, id);
			return entity;
		}

		private static TestEntityB CreateEntityB(int id)
		{
			var entity = new TestEntityB();
			SetEntityId(entity, id);
			return entity;
		}

		private static void SetEntityId(object entity, int id)
		{
			Type type = typeof(Entity);

			FieldInfo fieldInfo = type.GetField("_id",
			                                    BindingFlags.Default |
			                                    BindingFlags.NonPublic |
			                                    BindingFlags.FlattenHierarchy |
			                                    BindingFlags.Instance);

			fieldInfo.SetValue(entity, id);
		}

		#endregion

		#region Nested types

		private class TestEntityA : Entity { }

		private class TestEntityB : Entity { }

		private class TestDependingItem : DependingItem
		{
			public TestDependingItem([NotNull] Entity entity, [NotNull] string name)
				: base(entity, name) { }

			public override bool RemovedByCascadingDeletion
			{
				get { return false; }
			}

			protected override void RemoveDependencyCore()
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
