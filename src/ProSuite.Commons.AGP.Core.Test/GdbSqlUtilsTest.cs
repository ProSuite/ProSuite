using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.GeoDb;
using FieldType = ArcGIS.Core.Data.FieldType;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GdbSqlUtilsTest
{
	private const string TableName = "dated_rows";
	private const string DateFieldName = "EVENT_DATE";

	private static readonly DateTime[] _storedDates =
	{
		new DateTime(2024, 6, 4, 6, 57, 2),
		new DateTime(2024, 6, 4, 11, 24, 48),
		new DateTime(2024, 6, 5, 7, 7, 45)
	};

	private string _tempDirectory;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		_tempDirectory = Path.Combine(Path.GetTempPath(),
		                              $"{nameof(GdbSqlUtilsTest)}_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDirectory);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		try
		{
			Directory.Delete(_tempDirectory, true);
		}
		catch (IOException)
		{
			// The geodatabase files can still be locked by Pro.
		}
		catch (UnauthorizedAccessException)
		{
			// The geodatabase files can still be locked by Pro.
		}
	}

	/// <summary>
	/// Both are local databases for Pro. A mobile geodatabase was reported as file geodatabase,
	/// so the file geodatabase date syntax was used on it and the query failed.
	/// </summary>
	[TestCase(".gdb", WorkspaceDbType.FileGeodatabase)]
	[TestCase(".geodatabase", WorkspaceDbType.MobileGeodatabase)]
	public void GetWorkspaceDbType_DistinguishesFileAndMobileGeodatabase(
		string extension, WorkspaceDbType expected)
	{
		using ArcGIS.Core.Data.Geodatabase gdb = CreateGeodatabaseWithDatedRows(extension);

		Assert.AreEqual(GeodatabaseType.LocalDatabase, gdb.GetGeodatabaseType());
		Assert.AreEqual(expected, WorkspaceUtils.GetWorkspaceDbType(gdb));
	}

	/// <summary>
	/// The middle stored date is the lower limit, so the query must also find the row whose
	/// date is equal to the limit.
	/// </summary>
	[TestCase(".gdb")]
	[TestCase(".geodatabase")]
	public void GetDateLiteral_FindsRowsFromTheLimitOnwards(string extension)
	{
		using ArcGIS.Core.Data.Geodatabase gdb = CreateGeodatabaseWithDatedRows(extension);
		using Table table = gdb.OpenDataset<Table>(TableName);

		string literal = GdbSqlUtils.GetDateLiteral(_storedDates[1], gdb);

		Assert.AreEqual(2, CountRows(table, $"{DateFieldName} >= {literal}"), literal);
		Assert.AreEqual(1, CountRows(table, $"{DateFieldName} = {literal}"), literal);
	}

	/// <summary>
	/// Documents why <see cref="GdbSqlUtils.GetMobileGeodatabaseDateLiteral"/> does not use
	/// the SQLite text literal: a mobile geodatabase stores dates as numbers, Pro accepts
	/// the text literal, and the query finds nothing without reporting an error.
	/// </summary>
	[Test]
	public void MobileGeodatabase_SqliteTextLiteralSilentlyFindsNoRows()
	{
		using ArcGIS.Core.Data.Geodatabase gdb = CreateGeodatabaseWithDatedRows(".geodatabase");
		using Table table = gdb.OpenDataset<Table>(TableName);

		string literal = GdbSqlUtils.GetSqliteDateLiteral(_storedDates[0]);

		Assert.AreEqual(0, CountRows(table, $"{DateFieldName} >= {literal}"), literal);
	}

	private static long CountRows(Table table, string whereClause)
	{
		return table.GetCount(new QueryFilter { WhereClause = whereClause });
	}

	/// <summary>
	/// Creates a file geodatabase (extension .gdb) or a mobile geodatabase (extension
	/// .geodatabase) with one table that contains a row for each of the stored dates.
	/// </summary>
	private ArcGIS.Core.Data.Geodatabase CreateGeodatabaseWithDatedRows(string extension)
	{
		string path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}{extension}");
		var uri = new Uri(path, UriKind.Absolute);

		ArcGIS.Core.Data.Geodatabase gdb =
			extension == ".gdb"
				? SchemaBuilder.CreateGeodatabase(new FileGeodatabaseConnectionPath(uri))
				: SchemaBuilder.CreateGeodatabase(new MobileGeodatabaseConnectionPath(uri));

		var schemaBuilder = new SchemaBuilder(gdb);
		schemaBuilder.Create(new TableDescription(
			                     TableName, new List<FieldDescription>
			                                {
				                                new FieldDescription(DateFieldName,
					                                FieldType.Date)
			                                }));
		Assert.True(schemaBuilder.Build(), string.Join("; ", schemaBuilder.ErrorMessages));

		using Table table = gdb.OpenDataset<Table>(TableName);

		gdb.ApplyEdits(() =>
		{
			foreach (DateTime date in _storedDates)
			{
				using RowBuffer buffer = table.CreateRowBuffer();
				buffer[DateFieldName] = date;
				using Row row = table.CreateRow(buffer);
			}
		});

		return gdb;
	}
}
