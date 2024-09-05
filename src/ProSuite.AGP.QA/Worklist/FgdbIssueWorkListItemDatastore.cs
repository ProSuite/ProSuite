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
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.QA.WorkList;

public class FgdbIssueWorkListItemDatastore : IWorkListItemDatastore
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly string _domainName = "CORRECTION_STATUS_CD";

	private string _path;

	public FgdbIssueWorkListItemDatastore(string path)
	{
		string gdbPath = null;
		if (path != null && path.EndsWith(".iwl", StringComparison.InvariantCultureIgnoreCase))
		{
			// It's the definition file
			if (! ContainsValidIssueGdbPath(path, out gdbPath, out string message))
			{
				throw new InvalidOperationException(
					$"The issue work list {path} references a geodatabase that does not exist.");
			}

			_msg.DebugFormat("Extracted issue gdb path from {0}: {1}", path, gdbPath);
		}
		else
		{
			// Assume it is already an issue.gdb path:
			gdbPath = path;
		}

		_path = gdbPath;
	}

	#region Implementation of IWorkListItemDatastore

	public bool Validate(out string message)
	{
		message = null;

		// TODO: Can the path really be null?
		if (_path != null && _path.EndsWith(".iwl", StringComparison.InvariantCultureIgnoreCase))
		{
			// It's the definition file
			string iwlFilePath = _path;
			if (! ContainsValidIssueGdbPath(_path, out string _, out message))
			{
				return false;
			}
		}

		return true;
	}

	public IEnumerable<Table> GetTables()
	{
		if (string.IsNullOrEmpty(_path))
		{
			return Enumerable.Empty<Table>();
		}

		// todo daro: ensure layers are not already in map
		// todo daro: inline
		using Geodatabase geodatabase =
			new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)));

		return DatasetUtils.OpenTables(geodatabase, IssueGdbSchema.IssueFeatureClassNames)
		                   .ToList();
	}

	public async Task<bool> TryPrepareSchema()
	{
		if (_path == null)
		{
			_msg.Debug($"{nameof(_path)} is null");
			return false;
		}

		Stopwatch watch = Stopwatch.StartNew();

		using (Geodatabase geodatabase =
		       new Geodatabase(
			       new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)))
		      )
		{
			if (geodatabase.GetDomains()
			               .Any(domain => string.Equals(_domainName, domain.GetName())))
			{
				_msg.Debug($"Domain {_domainName} already exists in {_path}");
				return true;
			}
		}

		// the GP tool is going to fail on creating a domain with the same name
		await Task.WhenAll(
			GeoprocessingUtils.CreateDomainAsync(_path, _domainName,
			                                     "Correction status for work list"),
			GeoprocessingUtils.AddCodedValueToDomainAsync(
				_path, _domainName, (int) IssueCorrectionStatus.NotCorrected, "Not Corrected"),
			GeoprocessingUtils.AddCodedValueToDomainAsync(
				_path, _domainName, (int) IssueCorrectionStatus.Corrected, "Corrected"));

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

	#endregion

	private static bool ContainsValidIssueGdbPath([NotNull] string iwlFilePath,
	                                              out string gdbPath,
	                                              out string message)
	{
		gdbPath = WorkListUtils.GetIssueGeodatabasePath(iwlFilePath);
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
		const string fieldName = "STATUS";

		Stopwatch watch = Stopwatch.StartNew();

		string path = table.GetPath().LocalPath;

		// the GP tool is not going to fail on adding a field with the same name
		// But it still takes hell of a long time...
		TableDefinition tableDefinition = table.GetDefinition();

		if (tableDefinition.FindField(fieldName) < 0)
		{
			Task<bool> addField =
				GeoprocessingUtils.AddFieldAsync(path, fieldName, "Status",
				                                 FieldType.Integer, null, null,
				                                 null, true, false, _domainName);

			Task<bool> assignDefaultValue =
				GeoprocessingUtils.AssignDefaultToFieldAsync(path, fieldName, 100);

			await Task.WhenAll(addField, assignDefaultValue);

			_msg.DebugStopTiming(watch, "Prepared schema - status field on {0}", path);
		}

		return table;
	}
}
