using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.GP;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace ProSuite.AGP.QA.WorkList;

/// <summary>
/// Implements the <see cref="IWorkListItemDatastore"/> for the issue file geodatabase schema.
/// </summary>
public class FileGdbIssueWorkListItemDatastore : IWorkListItemDatastore
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const string _statusFieldName = "STATUS";
	private readonly string _domainName = "CORRECTION_STATUS_CD";

	private string _issueGdbPath;
	private readonly string _initialWorkListName;

	public FileGdbIssueWorkListItemDatastore(string workListFileOrIssueGdbPath)
	{
		string gdbPath;

		if (workListFileOrIssueGdbPath != null &&
		    workListFileOrIssueGdbPath.EndsWith(
			    ".iwl", StringComparison.InvariantCultureIgnoreCase))
		{
			// It's the definition file
			if (! ContainsValidIssueGdbPath(workListFileOrIssueGdbPath, out gdbPath,
			                                out string message))
			{
				throw new InvalidOperationException(
					$"Cannot open FileGeodatabase Issue Datastore from {workListFileOrIssueGdbPath}: {message}.");
			}

			_msg.DebugFormat("Extracted issue gdb path from {0}: {1}", workListFileOrIssueGdbPath,
			                 gdbPath);

			_initialWorkListName = Path.GetFileNameWithoutExtension(workListFileOrIssueGdbPath);
		}
		else
		{
			// Assume it is already an issue.gdb path:
			gdbPath = workListFileOrIssueGdbPath;

			string directoryName = Path.GetDirectoryName(workListFileOrIssueGdbPath);
			_initialWorkListName = Path.GetFileName(directoryName);
		}

		_issueGdbPath = gdbPath;
	}

	#region Implementation of IWorkListItemDatastore

	public bool Validate(out string message)
	{
		message = null;

		// TODO: Can the path really be null?
		if (_issueGdbPath != null &&
		    _issueGdbPath.EndsWith(".iwl", StringComparison.InvariantCultureIgnoreCase))
		{
			if (! ContainsValidIssueGdbPath(_issueGdbPath, out string _, out message))
			{
				return false;
			}
		}

		return true;
	}

	public IEnumerable<Table> GetTables()
	{
		if (string.IsNullOrEmpty(_issueGdbPath))
		{
			return Enumerable.Empty<Table>();
		}

		// todo daro: ensure layers are not already in map
		// todo daro: inline
		using Geodatabase geodatabase =
			new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(_issueGdbPath, UriKind.Absolute)));

		return DatasetUtils.OpenTables(geodatabase, IssueGdbSchema.IssueFeatureClassNames)
		                   .ToList();
	}

	public async Task<bool> TryPrepareSchema()
	{
		if (_issueGdbPath == null)
		{
			_msg.Debug($"{nameof(_issueGdbPath)} is null");
			return false;
		}

		Stopwatch watch = Stopwatch.StartNew();

		using (Geodatabase geodatabase =
		       new Geodatabase(
			       new FileGeodatabaseConnectionPath(new Uri(_issueGdbPath, UriKind.Absolute))))
		{
			if (geodatabase.GetDomains()
			               .Any(domain => string.Equals(_domainName, domain.GetName())))
			{
				_msg.Debug($"Domain {_domainName} already exists in {_issueGdbPath}");
				return true;
			}
		}

		// the GP tool is going to fail on creating a domain with the same name
		await Task.WhenAll(
			GeoprocessingUtils.CreateDomainAsync(_issueGdbPath, _domainName,
			                                     "Correction status for work list"),
			GeoprocessingUtils.AddCodedValueToDomainAsync(
				_issueGdbPath, _domainName, (int) IssueCorrectionStatus.NotCorrected,
				"Not Corrected"),
			GeoprocessingUtils.AddCodedValueToDomainAsync(
				_issueGdbPath, _domainName, (int) IssueCorrectionStatus.Corrected, "Corrected"));

		_msg.DebugStopTiming(watch, "Prepared schema - domain");

		return true;
	}

	public async Task<IList<Table>> PrepareTableSchema(IList<Table> dbTables)
	{
		return await Task.WhenAll(dbTables.Select(EnsureStatusFieldCoreAsync));
	}

	public IAttributeReader CreateAttributeReader(TableDefinition definition,
	                                              params Attributes[] attributes)
	{
		return new AttributeReader(definition, attributes);
	}

	public DbSourceClassSchema CreateStatusSchema(TableDefinition tableDefinition)
	{
		int fieldIndex;

		try
		{
			fieldIndex = tableDefinition.FindField(_statusFieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentException($"No field {_statusFieldName}");
			}
		}
		catch (Exception e)
		{
			_msg.Error($"Error find field {_statusFieldName} in {tableDefinition.GetName()}", e);
			throw;
		}

		string shapeField = null;
		string objectIDField = tableDefinition.GetObjectIDField();

		if (tableDefinition is FeatureClassDefinition featureClassDefinition)
		{
			shapeField = featureClassDefinition.GetShapeField();
		}

		// The status schema is the same for production model datasets and Issue Geodatabase tables.
		return new DbSourceClassSchema(objectIDField, shapeField,
		                               _statusFieldName, fieldIndex,
		                               (int) IssueCorrectionStatus.NotCorrected,
		                               (int) IssueCorrectionStatus.Corrected);
	}

	public string SuggestWorkListName()
	{
		return _initialWorkListName;
	}

	public bool ContainsSourceClass(ISourceClass sourceClass)
	{
		// TODO: Consider also checking field names?

		return IssueGdbSchema.IssueFeatureClassNames.Any(
			n => n.Equals(sourceClass.Name, StringComparison.InvariantCultureIgnoreCase));
	}

	public IObjectDataset GetObjetDataset(TableDefinition tableDefinition)
	{
		IObjectDataset dataset;

		if (tableDefinition is FeatureClassDefinition featureClassDefinition)
		{
			GeometryType geometryType = featureClassDefinition.GetShapeType();

			switch (geometryType)
			{
				case GeometryType.Point:
				case GeometryType.Multipoint:
					dataset = new ErrorMultipointDataset("IssuePoints");
					break;
				case GeometryType.Polyline:
					dataset = new ErrorLineDataset("IssueLines");
					break;
				case GeometryType.Polygon:
					dataset = new ErrorPolygonDataset("IssuePolygons");
					break;
				case GeometryType.Multipatch:
					dataset = new ErrorMultiPatchDataset("IssueMultiPatches");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		else
		{
			dataset = new ErrorTableDataset("IssueRows");
		}

		Assert.NotNull(dataset,
		               $"Error dataset is null for table definition {tableDefinition.GetName()}");
		return dataset;
	}

	#endregion

	private static bool ContainsValidIssueGdbPath([NotNull] string iwlFilePath,
	                                              out string gdbPath,
	                                              out string message)
	{
		gdbPath = WorkListUtils.GetIssueGeodatabasePath(iwlFilePath, out message);
		message = null;

		if (gdbPath == null)
		{
			message =
				$"The issue work list {iwlFilePath} references a geodatabase that does not exist.";
			return false;
		}

		return true;
	}

	private async Task<Table> EnsureStatusFieldCoreAsync(Table table)
	{
		Stopwatch watch = Stopwatch.StartNew();

		string path = table.GetPath().LocalPath;

		// the GP tool is not going to fail on adding a field with the same name
		// But it still takes hell of a long time...
		TableDefinition tableDefinition = table.GetDefinition();

		if (tableDefinition.FindField(_statusFieldName) < 0)
		{
			Task<bool> addField =
				GeoprocessingUtils.AddFieldAsync(path, _statusFieldName, "Status",
				                                 FieldType.Integer, null, null,
				                                 null, true, false, _domainName);

			Task<bool> assignDefaultValue =
				GeoprocessingUtils.AssignDefaultToFieldAsync(path, _statusFieldName, 100);

			await Task.WhenAll(addField, assignDefaultValue);

			_msg.DebugStopTiming(watch, "Prepared schema - status field on {0}", path);
		}

		return table;
	}
}
