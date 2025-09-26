using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Test.DataModel
{
	[TestFixture]
	public abstract class ObjectDatasetTestBase
	{
		#region Setup/Teardown

		[SetUp]
		public void Setup()
		{
			string gdbPath = TestDataPreparer
			                 .ExtractZip("gdb2.gdb.zip", @"..\ProSuite.Commons.AO.Test\TestData")
			                 .GetPath();

			Model = CreateModel(gdbPath);
		}

		#endregion

		protected DdxModel Model { get; private set; }

		protected abstract DdxModel CreateModel([NotNull] string fgdbPath);

		protected abstract ObjectDataset CreateObjectDataset(string name);

		protected abstract void HarvestObjectTypes(ObjectDataset dataset);

		[Test]
		public void CanAddAttributes()
		{
			ObjectDataset dataset = CreateObjectDataset("test");
			dataset.AddAttribute(new ObjectAttribute("ATT1",
			                                         FieldType.Integer));
			Assert.AreEqual(1, dataset.Attributes.Count);
			Assert.AreEqual(1, new List<ObjectAttribute>(dataset.GetAttributes()).Count);
		}

		[Test]
		public void CanAddObjectSubtypes()
		{
			ObjectDataset dataset = CreateObjectDataset("vectordataset1");
			Model.AddDataset(dataset);

			dataset.AddAttribute(new ObjectAttribute("KUNSTBAUTE",
			                                         FieldType.Integer));

			HarvestObjectTypes(dataset);

			const string objectSubtypeName = "10m_Strasse_Bruecke";
			const string attributeName = "KUNSTBAUTE";
			const int attributeValue = 1;
			ObjectSubtype objectSubtype =
				dataset.ObjectTypes[0].AddObjectSubType(objectSubtypeName,
				                                        attributeName, attributeValue);
			Assert.AreEqual(objectSubtypeName, objectSubtype.Name);
			Assert.AreEqual(1, dataset.ObjectTypes[0].ObjectSubtypes.Count);
			Assert.AreEqual(1, objectSubtype.Criteria.Count);
			ObjectSubtypeCriterion criterion = objectSubtype.Criteria[0];
			Assert.AreEqual(attributeName, criterion.Attribute.Name);
			Assert.AreEqual(attributeValue, criterion.AttributeValue);
		}

		[Test]
		public void CanGetAttributeByName()
		{
			ObjectDataset dataset = CreateObjectDataset("test");

			dataset.AddAttribute(new ObjectAttribute("ATT1",
			                                         FieldType.Integer));
			dataset.AddAttribute(new ObjectAttribute("ATT2",
			                                         FieldType.Integer));
			dataset.AddAttribute(new ObjectAttribute("ATT3",
			                                         FieldType.Integer));
			dataset.AddAttribute(new ObjectAttribute("ATT4",
			                                         FieldType.Integer));

			ObjectAttribute att = dataset.GetAttribute("ATT3");
			Assert.IsNotNull(att);
			Assert.AreEqual("ATT3", att.Name);
		}

		[Test]
		public void CanGetAttributeByRole()
		{
			ObjectDataset dataset = CreateObjectDataset("test");

			dataset.AddAttribute(
				new ObjectAttribute("ATT1", FieldType.Integer));
			dataset.AddAttribute(
				new ObjectAttribute("ATT2", FieldType.Integer));
			dataset.AddAttribute(
				new ObjectAttribute("ATT3", FieldType.Guid,
				                    new ObjectAttributeType(AttributeRole.UUID)));
			dataset.AddAttribute(
				new ObjectAttribute("ATT4", FieldType.Integer));

			ObjectAttribute att = dataset.GetAttribute(AttributeRole.UUID);
			Assert.IsNotNull(att);
			Assert.AreEqual("ATT3", att.Name);
		}

		[Test]
		public void CanGetNullAttributeIfNameUnknown()
		{
			ObjectDataset dataset = CreateObjectDataset("test");

			dataset.AddAttribute(
				new ObjectAttribute("ATT1", FieldType.Integer));
			dataset.AddAttribute(
				new ObjectAttribute("ATT2", FieldType.Integer));

			ObjectAttribute att = dataset.GetAttribute("ATT3");
			Assert.IsNull(att);
		}

		[Test]
		public void CanGetNullAttributeIfRoleUndefined()
		{
			ObjectDataset dataset = CreateObjectDataset("test");

			dataset.AddAttribute(
				new ObjectAttribute("ATT1", FieldType.Integer));
			dataset.AddAttribute(
				new ObjectAttribute("ATT2", FieldType.Integer));

			ObjectAttribute att = dataset.GetAttribute(AttributeRole.UUID);
			Assert.IsNull(att);
		}

		[Test]
		[Ignore("TODO reimplement using test gdb")]
		public void CanGetObjectSubtype()
		{
			//ObjectDataset dataset = CreateObjectDataset("ds1");
			//dataset.AddAttribute(new ObjectAttribute("type", 0));
			//dataset.AddAttribute(new ObjectAttribute("att1", 1));
			//dataset.AddAttribute(new ObjectAttribute("att2", 2));

			//ObjectType type1 = dataset.AddObjectType(1, "type1");
			//ObjectType type2 = dataset.AddObjectType(2, "type2");

			//type1.AddObjectSubType("subtype11", "att1", "a", VariantValueType.String);
			//type1.AddObjectSubType("subtype12", "att1", "b", VariantValueType.String);
			//ObjectSubtype subType13 =
			//    type1.AddObjectSubType("subtype13", "att1", "c", VariantValueType.String);

			//type2.AddObjectSubType("subtype21", "att2", "x", VariantValueType.String);
			//type2.AddObjectSubType("subtype22", "att2", "y", VariantValueType.String);
			//type2.AddObjectSubType("subtype23", "att2", "z", VariantValueType.String);

			//subType13.AddCriterion("att2", "z", VariantValueType.String);

			//// prepare IObject mock
			//ObjectClassMock classMock = new ObjectClassMock(1, "ds1");
			//classMock.AddField("type", esriFieldType.esriFieldTypeInteger);
			//classMock.AddField("att1", esriFieldType.esriFieldTypeString);
			//classMock.AddField("att2", esriFieldType.esriFieldTypeString);

			//IObject objMock = classMock.CreateObject(1);
			//objMock.set_Value(0, 1);
			//objMock.set_Value(1, "c");
			//objMock.set_Value(2, "z");

			//// prepare mock for gdb facade
			//MockRepository mocks = new MockRepository();
			//IGdbUtils gdbUtilsMock = (IGdbUtils) mocks.StrictMock(typeof (IGdbUtils));
			//Expect.Call(gdbUtilsMock.GetSubtypeFieldIndex(objMock)).Return(0);
			//Expect.Call(gdbUtilsMock.GetName((IObjectClass) classMock)).Return("ds1");
			//Expect.Call(gdbUtilsMock.GetSubtypeFieldIndex(objMock)).Return(0);
			//mocks.ReplayAll();

			//using (GdbUtils.SetMock(gdbUtilsMock))
			//{
			//    ObjectCategory category = dataset.GetObjectCategory(objMock);

			//    Assert.IsNotNull(category);
			//    Assert.AreEqual(2, ((ObjectSubtype) category).Criteria.Count);
			//    Assert.AreEqual("subtype13", category.Name);
			//}
		}

		[Test]
		[Ignore("TODO reimplement using test gdb")]
		public void CanGetObjectType()
		{
			//ObjectDataset dataset = CreateObjectDataset("ds1");
			//dataset.AddAttribute(new ObjectAttribute("type", 0));
			//dataset.AddAttribute(new ObjectAttribute("att1", 1));
			//dataset.AddAttribute(new ObjectAttribute("att2", 2));

			//ObjectType type1 = dataset.AddObjectType(1, "type1");
			//ObjectType type2 = dataset.AddObjectType(2, "type2");

			//type1.AddObjectSubType("subtype11", "att1", "a", VariantValueType.String);
			//type1.AddObjectSubType("subtype12", "att1", "b", VariantValueType.String);
			//ObjectSubtype subType13 =
			//    type1.AddObjectSubType("subtype13", "att1", "c", VariantValueType.String);

			//type2.AddObjectSubType("subtype21", "att2", "x", VariantValueType.String);
			//type2.AddObjectSubType("subtype22", "att2", "y", VariantValueType.String);
			//type2.AddObjectSubType("subtype23", "att2", "z", VariantValueType.String);

			//subType13.AddCriterion("att2", "z", VariantValueType.String);

			//// prepare IObject mock
			//ObjectClassMock classMock = new ObjectClassMock(1, "ds1");
			//classMock.AddField("type", esriFieldType.esriFieldTypeInteger);
			//classMock.AddField("att1", esriFieldType.esriFieldTypeString);
			//classMock.AddField("att2", esriFieldType.esriFieldTypeString);

			//IObject objMock = classMock.CreateObject(1);
			//objMock.set_Value(0, 2);
			//objMock.set_Value(2, "y");

			//// prepare mock for gdb facade
			//MockRepository mocks = new MockRepository();
			//IGdbUtils gdbUtilsMock = (IGdbUtils) mocks.StrictMock(typeof (IGdbUtils));
			//Expect.Call(gdbUtilsMock.GetSubtypeFieldIndex(objMock)).Return(0);
			//Expect.Call(gdbUtilsMock.GetName((IObjectClass) classMock)).Return("ds1");
			//Expect.Call(gdbUtilsMock.GetSubtypeFieldIndex(objMock)).Return(0);
			//mocks.ReplayAll();

			//using (GdbUtils.SetMock(gdbUtilsMock))
			//{
			//    ObjectCategory category = dataset.GetObjectCategory(objMock);

			//    Assert.IsNotNull(category);
			//    Assert.AreEqual(1, ((ObjectSubtype) category).Criteria.Count);
			//    Assert.AreEqual("subtype22", category.Name);
			//}
		}

		[Test]
		public void CannotAddAttributeDirectly()
		{
			Assert.Throws<NotSupportedException>(
				delegate
				{
					ObjectDataset dataset = CreateObjectDataset("test");
					dataset.Attributes.Add(
						new ObjectAttribute("ATT1", FieldType.Integer));
				});
		}

		[Test]
		public void CannotAddAttributesWithSameName()
		{
			Assert.Throws<ArgumentException>(
				delegate
				{
					ObjectDataset dataset = CreateObjectDataset("test");
					var att1 = new ObjectAttribute("ATT1", FieldType.Integer);
					var att2 = new ObjectAttribute("ATT1", FieldType.Integer);

					dataset.AddAttribute(att1);
					dataset.AddAttribute(att2);
				});
		}

		[Test]
		public void CannotAddAttributesWithSameNonStandardRole()
		{
			Assert.Throws<ArgumentException>(
				delegate
				{
					ObjectDataset dataset = CreateObjectDataset("test");
					var att1 = new ObjectAttribute("ATT1", FieldType.Guid,
					                               new ObjectAttributeType(
						                               "uuid", AttributeRole.UUID));
					var att2 = new ObjectAttribute("ATT2", FieldType.Guid,
					                               new ObjectAttributeType(
						                               "uuid", AttributeRole.UUID));

					dataset.AddAttribute(att1);
					dataset.AddAttribute(att2);
				});
		}

		[Test]
		public void CannotAddAttributeTwice()
		{
			ObjectDataset dataset = CreateObjectDataset("test");
			var att1 = new ObjectAttribute("ATT1", FieldType.Integer);
			dataset.AddAttribute(att1);
			try
			{
				dataset.AddAttribute(att1); // offending call: same instance again
			}
			catch (ArgumentException)
			{
				// expected
			}

			Assert.AreEqual(1, dataset.Attributes.Count);
			Assert.AreEqual(1, new List<ObjectAttribute>(dataset.GetAttributes()).Count);

			Assert.AreEqual(dataset, att1.Dataset);
		}

		[Test]
		public void CannotAddDuplicateCriteria()
		{
			Assert.Throws<ArgumentException>(
				delegate
				{
					ObjectDataset dataset = CreateObjectDataset("vectordataset1");
					Model.AddDataset(dataset);

					const string attributeName = "KUNSTBAUTE";

					dataset.AddAttribute(
						new ObjectAttribute(attributeName, FieldType.Integer));

					HarvestObjectTypes(dataset);

					const int attributeValue = 1;
					ObjectSubtype objectSubtype =
						dataset.ObjectTypes[0].AddObjectSubType("10m_Strasse_Bruecke",
						                                        attributeName, attributeValue);
					objectSubtype.AddCriterion(attributeName, attributeValue);
				});
		}

		[Test]
		public void CannotRemoveAttributeDirectly()
		{
			Assert.Throws<NotSupportedException>(
				delegate
				{
					ObjectDataset dataset = CreateObjectDataset("test");
					ObjectAttribute att = dataset.AddAttribute(
						new ObjectAttribute("ATT1", FieldType.Integer));
					dataset.Attributes.Remove(att);
				});
		}

		[Test]
		[Category("gdb")]
		public void CanReadObjectTypesFromGdb()
		{
			ObjectDataset dataset = CreateObjectDataset("vectordataset1");
			Model.AddDataset(dataset);

			HarvestObjectTypes(dataset);

			Assert.AreEqual(3, dataset.ObjectTypes.Count);
			Assert.AreEqual(dataset.ObjectTypes.Count,
			                new List<ObjectType>(dataset.GetObjectTypes()).Count);
		}

		[Test]
		[Category("gdb")]
		public void CanReadObjectTypesFromGdbIfNotEmpty()
		{
			ObjectDataset dataset = CreateObjectDataset("vectordataset1");
			Model.AddDataset(dataset);

			dataset.AddObjectType(98, "existingType1");
			dataset.AddObjectType(99, "existingType2");

			HarvestObjectTypes(dataset);
			int activeObjectTypes = 0;
			foreach (ObjectType type in dataset.ObjectTypes)
			{
				if (! type.Deleted)
				{
					activeObjectTypes++;
				}
			}

			Assert.AreEqual(3, activeObjectTypes);
		}

		[Test]
		public void CanRemoveAttributes()
		{
			ObjectDataset dataset = CreateObjectDataset("test");

			var att1 = new ObjectAttribute("ATT1", FieldType.Integer);
			var att2 = new ObjectAttribute("ATT2", FieldType.Integer);

			dataset.AddAttribute(att1);
			dataset.AddAttribute(att2);

			Assert.AreEqual(2, dataset.Attributes.Count);
			Assert.AreEqual(2, new List<ObjectAttribute>(dataset.GetAttributes()).Count);
			dataset.RemoveAttribute(att2);

			Assert.AreEqual(1, dataset.Attributes.Count);
			Assert.AreEqual(1, new List<ObjectAttribute>(dataset.GetAttributes()).Count);

			Assert.AreEqual(att1, dataset.Attributes[0]);
			Assert.IsNull(att2.Dataset);
		}
	}
}
