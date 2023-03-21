using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Test.DataModel
{
	[TestFixture]
	public abstract class AssociationTestBase
	{
		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanRegisterForeignKeyAssociation()
		{
			VectorDataset ds1 = CreateVectorDataset("ds1");
			VectorDataset ds2 = CreateVectorDataset("ds2");
			ObjectAttribute foreignKey = ds1.AddAttribute(
				new ObjectAttribute("fkey", FieldType.Text));
			ObjectAttribute primaryKey = ds2.AddAttribute(
				new ObjectAttribute("pkey", FieldType.Text));

			Association association =
				new ForeignKeyAssociation("relClassName",
				                          AssociationCardinality.OneToMany,
				                          foreignKey, primaryKey);

			AssociationEnd end1 = association.End1;
			AssociationEnd end2 = association.End2;

			Assert.IsTrue(end1.HasForeignKey);
			Assert.IsFalse(end2.HasForeignKey);
			Assert.IsFalse(end1.HasPrimaryKey);
			Assert.IsTrue(end2.HasPrimaryKey);
			Assert.AreEqual(foreignKey, end1.ForeignKey);
			Assert.AreEqual(primaryKey, end2.PrimaryKey);
			Assert.AreEqual(ds1, end1.ObjectDataset);
			Assert.AreEqual(ds2, end2.ObjectDataset);
			Assert.AreEqual(end2, end1.OppositeEnd);
			Assert.AreEqual(end1, end2.OppositeEnd);
		}

		[Test]
		public void CanRegisterAttributedAssociation()
		{
			const string foreignKey1Name = "fk1";
			const string foreignKey2Name = "fk2";

			VectorDataset ds1 = CreateVectorDataset("ds1");
			VectorDataset ds2 = CreateVectorDataset("ds2");
			ObjectAttribute primaryKey1 = ds1.AddAttribute(
				new ObjectAttribute("pkey1", FieldType.Text));
			ObjectAttribute primaryKey2 = ds2.AddAttribute(
				new ObjectAttribute("pkey2", FieldType.Text));

			var association =
				new AttributedAssociation(
					"relClassName",
					AssociationCardinality.ManyToMany,
					foreignKey1Name, FieldType.Text,
					primaryKey1,
					foreignKey2Name, FieldType.Text,
					primaryKey2);

			AssociationEnd end1 = association.End1;
			AssociationEnd end2 = association.End2;

			Assert.IsTrue(end1.HasForeignKey);
			Assert.IsTrue(end2.HasForeignKey);
			Assert.IsTrue(end1.HasPrimaryKey);
			Assert.IsTrue(end2.HasPrimaryKey);
			Assert.AreEqual(foreignKey1Name, end1.ForeignKey.Name);
			Assert.AreEqual(foreignKey2Name, end2.ForeignKey.Name);
			Assert.AreEqual(primaryKey1, end1.PrimaryKey);
			Assert.AreEqual(primaryKey2, end2.PrimaryKey);
			Assert.AreEqual(ds1, end1.ObjectDataset);
			Assert.AreEqual(ds2, end2.ObjectDataset);
			Assert.AreEqual(end2, end1.OppositeEnd);
			Assert.AreEqual(end1, end2.OppositeEnd);
		}
	}
}
