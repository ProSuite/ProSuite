using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class TableViewFactoryTest
	{
		[Test]
		public void CheckExpressions()
		{
			new ArcGISLicenses().Checkout();
			GdbTable t = new GdbTable(1, "t");
			t.AddField(FieldUtils.CreateField("a", esriFieldType.esriFieldTypeInteger));
			t.AddField(FieldUtils.CreateField("b", esriFieldType.esriFieldTypeInteger));
			// Validate expressions
			TableViewFactory.Create(t, "a+b=5");
			TableViewFactory.Create(t, "a-b=5");
			TableViewFactory.Create(t, "a*b=5");
			TableViewFactory.Create(t, "a/b=5");

			// Validate group expressions
			TableView tf = TableViewFactory.Create(t, "a%b=5");
			bool success;
			try
			{
				// nested expression are not allowed
				tf.AddExpressionColumn("tt", "SUM(IIF(a=0,1,2))", isGroupExpression: true);
				success = true;
			}
			catch
			{
				success = false;
			}

			Assert.False(success);

			tf.AddColumn("iifCol", typeof(int)).Expression = "IIF(a=0,1,2)";
			tf.AddExpressionColumn("sumIif", "SUM(iifCol)", isGroupExpression: true);
		}
	}
}
