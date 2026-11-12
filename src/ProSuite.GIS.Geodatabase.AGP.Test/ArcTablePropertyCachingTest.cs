using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.GIS.Geodatabase.AGP.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ArcTablePropertyCachingTest
{
	private ArcGIS.Core.Data.Geodatabase _gdb;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		_gdb = SchemaBuilder.CreateGeodatabase(
			new MemoryConnectionProperties("ArcTablePropertyCachingTest"));
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		_gdb?.Dispose();
	}

	[Test]
	public void Properties_of_cached_table_can_be_read_from_other_thread()
	{
		using Table proTable = CreateTable("PROPERTIES_TABLE", "Properties Alias");
		using TableDefinition definition = proTable.GetDefinition();

		var tableNotInWorkspaceCache = new ArcTable(proTable);

		string name = null;
		string aliasName = null;
		string oidFieldName = null;
		int nameFieldIndex = -1;

		Action readProperties = () =>
		{
			name = tableNotInWorkspaceCache.Name;
			aliasName = tableNotInWorkspaceCache.AliasName;
			oidFieldName = tableNotInWorkspaceCache.OIDFieldName;
			nameFieldIndex = tableNotInWorkspaceCache.FindField("NAME");
		};

		Assert.IsInstanceOf<CalledOnWrongThreadException>(
			TestThreadUtils.RunOnOtherThread(readProperties));

		tableNotInWorkspaceCache.CacheProperties();

		Assert.IsNull(TestThreadUtils.RunOnOtherThread(readProperties));
		Assert.AreEqual(definition.GetName(), name);
		Assert.AreEqual(definition.GetAliasName(), aliasName);
		Assert.AreEqual(definition.GetObjectIDField(), oidFieldName);
		Assert.AreEqual(definition.FindField("NAME"), nameFieldIndex);
	}

	[Test]
	public void Caching_on_other_thread_fails_and_can_be_retried_on_the_correct_thread()
	{
		using Table proTable = CreateTable("WRONG_THREAD_TABLE");

		var table = new ArcTable(proTable);

		for (int i = 0; i < 2; i++)
		{
			Assert.IsInstanceOf<CalledOnWrongThreadException>(
				TestThreadUtils.RunOnOtherThread(table.CacheProperties));
		}

		table.CacheProperties();

		string aliasName = null;
		Assert.IsNull(TestThreadUtils.RunOnOtherThread(() => aliasName = table.AliasName));
		Assert.AreEqual("WRONG_THREAD_TABLE", aliasName);
	}

	[Test]
	public void Failed_table_property_caching_is_retried_until_it_succeeds()
	{
		using Table proTable = CreateTable("RETRY_TABLE");

		var table = new TableFailingToCacheOnce(proTable);

		Assert.Throws<InvalidOperationException>(table.CacheProperties);

		table.CacheProperties();
		Assert.AreEqual(2, table.CacheAttemptCount);

		table.CacheProperties();
		Assert.AreEqual(2, table.CacheAttemptCount);
	}

	[Test]
	public void Failed_feature_class_property_caching_can_be_retried()
	{
		using FeatureClass proFeatureClass = DatasetUtils.CreateFeatureClass(
			_gdb, "RETRY_FC", CreateFieldDescriptions(), GeometryType.Point,
			SpatialReferences.WGS84, hasZ: false);
		using FeatureClassDefinition definition = proFeatureClass.GetDefinition();

		var featureClass = new FeatureClassFailingToCacheOnce(proFeatureClass);

		string shapeFieldName = null;
		Action readShapeField = () => shapeFieldName = featureClass.ShapeFieldName;

		Assert.Throws<InvalidOperationException>(featureClass.CacheProperties);

		Assert.IsInstanceOf<CalledOnWrongThreadException>(
			TestThreadUtils.RunOnOtherThread(readShapeField));

		featureClass.CacheProperties();

		Assert.IsNull(TestThreadUtils.RunOnOtherThread(readShapeField));
		Assert.AreEqual(definition.GetShapeField(), shapeFieldName);
	}

	private Table CreateTable(string name, string aliasName = null)
	{
		var schemaBuilder = new SchemaBuilder(_gdb);
		schemaBuilder.Create(new TableDescription(name, CreateFieldDescriptions())
		                     {
			                     AliasName = aliasName
		                     });
		Assert.True(schemaBuilder.Build(), string.Join("; ", schemaBuilder.ErrorMessages));

		return _gdb.OpenDataset<Table>(name);
	}

	private static List<FieldDescription> CreateFieldDescriptions()
	{
		return new List<FieldDescription>
		       {
			       new FieldDescription("NAME", FieldType.String)
		       };
	}

	private class TableFailingToCacheOnce : ArcTable
	{
		public TableFailingToCacheOnce(Table proTable) : base(proTable) { }

		public int CacheAttemptCount { get; private set; }

		protected override void CachePropertiesCore()
		{
			CacheAttemptCount++;

			if (CacheAttemptCount == 1)
			{
				throw new InvalidOperationException("Simulated caching failure");
			}
		}
	}

	private class FeatureClassFailingToCacheOnce : ArcFeatureClass
	{
		private bool _hasFailed;

		public FeatureClassFailingToCacheOnce(FeatureClass proFeatureClass)
			: base(proFeatureClass) { }

		protected override void CachePropertiesCore()
		{
			if (! _hasFailed)
			{
				_hasFailed = true;
				throw new InvalidOperationException("Simulated caching failure");
			}

			base.CachePropertiesCore();
		}
	}
}
