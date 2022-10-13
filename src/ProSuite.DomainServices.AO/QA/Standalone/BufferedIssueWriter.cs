using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public class BufferedIssueWriter : IIssueWriter, IDisposable
	{
		[NotNull] private readonly IVerificationReportBuilder _verificationReportBuilder;
		[NotNull] private readonly IDatasetContext _datasetContext;
		[CanBeNull] private readonly IIssueRepository _issueRepository;
		[CanBeNull] private readonly Func<IObjectDataset, string> _getAlternateKeyField;
		private readonly int _maximumErrorCount;
		private readonly int _maximumVertexCount;

		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;

		[NotNull] private readonly List<PendingQaError> _pendingQaErrors =
			new List<PendingQaError>();

		private int _errorGeometryVertexCount;

		[NotNull] private readonly Dictionary<IObjectDataset, TableKeyLookup>
			_tableKeyLookups = new Dictionary<IObjectDataset, TableKeyLookup>();

		#region Constructor

		public BufferedIssueWriter(
			[NotNull] IVerificationReportBuilder verificationReportBuilder,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] IIssueRepository issueRepository,
			[CanBeNull] Func<IObjectDataset, string> getAlternateKeyField,
			int maximumErrorCount = 10000,
			int maximumVertexCount = 1000000)
		{
			Assert.ArgumentNotNull(verificationReportBuilder,
			                       nameof(verificationReportBuilder));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_verificationReportBuilder = verificationReportBuilder;
			_datasetContext = datasetContext;
			_issueRepository = issueRepository;
			_getAlternateKeyField = getAlternateKeyField;
			_maximumErrorCount = maximumErrorCount;
			_maximumVertexCount = maximumVertexCount;
			_datasetResolver = datasetResolver;
		}

		#endregion

		public void WriteIssue(QaError qaError, QualitySpecificationElement element)
		{
			var errorRequiresIdLookup = false;
			foreach (InvolvedRow involvedRow in qaError.InvolvedRows)
			{
				TableKeyLookup tableKeyLookup =
					GetTableRequiringIdLookup(involvedRow, element);

				if (tableKeyLookup != null)
				{
					tableKeyLookup.AddObjectId(involvedRow.OID);
					errorRequiresIdLookup = true;
				}
			}

			if (errorRequiresIdLookup)
			{
				AddToBuffer(qaError, element);

				if (IsBufferCapacityExceeded())
				{
					Flush();
				}
			}
			else
			{
				var issue = new Issue(element, qaError);

				WriteIssueCore(issue, qaError.Geometry);
			}
		}

		public void Dispose()
		{
			Flush();
		}

		public void Flush()
		{
			if (_pendingQaErrors.Count == 0)
			{
				return;
			}

			// create issues for all pending errors
			foreach (PendingQaError pendingQaError in _pendingQaErrors)
			{
				WriteIssueCore(CreateIssue(pendingQaError), pendingQaError.ErrorGeometry);
			}

			_pendingQaErrors.Clear();
			_errorGeometryVertexCount = 0;

			foreach (TableKeyLookup tableKeyLookup in _tableKeyLookups.Values)
			{
				tableKeyLookup?.ClearObjectIds();
			}
		}

		private bool IsBufferCapacityExceeded()
		{
			return _pendingQaErrors.Count > _maximumErrorCount ||
			       _errorGeometryVertexCount > _maximumVertexCount;
		}

		private void AddToBuffer([NotNull] QaError qaError,
		                         [NotNull] QualitySpecificationElement element)
		{
			_pendingQaErrors.Add(new PendingQaError(qaError, element, qaError.Geometry));
			_errorGeometryVertexCount = _errorGeometryVertexCount +
			                            GeometryUtils.GetPointCount(qaError.Geometry);
		}

		[NotNull]
		private Issue CreateIssue([NotNull] PendingQaError pendingQaError)
		{
			QualitySpecificationElement element = pendingQaError.QualitySpecificationElement;

			QaError error = pendingQaError.Error;

			return new Issue(element,
			                 error.Description,
			                 GetInvolvedTables(pendingQaError.Error.InvolvedRows, element),
			                 error.IssueCode,
			                 error.AffectedComponent,
			                 error.Values);
		}

		[NotNull]
		private IEnumerable<InvolvedTable> GetInvolvedTables(
			[NotNull] IEnumerable<InvolvedRow> involvedRows,
			[NotNull] QualitySpecificationElement element)
		{
			var result = new List<InvolvedTable>();

			foreach (KeyValuePair<string, List<InvolvedRow>> tableRows in
			         InvolvedRowUtils.GroupByTableName(involvedRows))
			{
				string tableName = tableRows.Key;
				List<InvolvedRow> involvedRowsForTable = tableRows.Value;

				IObjectDataset dataset = _datasetResolver.GetDatasetByInvolvedRowTableName(
					tableName, element.QualityCondition);
				Assert.NotNull(dataset, "unable to resolve dataset");

				result.Add(CreateInvolvedTable(dataset, involvedRowsForTable));
			}

			return result;
		}

		[NotNull]
		private InvolvedTable CreateInvolvedTable(
			[NotNull] IObjectDataset dataset,
			[NotNull] IEnumerable<InvolvedRow> rows)
		{
			TableKeyLookup tableKeyLookup;
			if (_tableKeyLookups.TryGetValue(dataset, out tableKeyLookup))
			{
				return new InvolvedTable(
					dataset.Name, GetAlternateKeyRowReferences(rows, tableKeyLookup),
					tableKeyLookup.KeyFieldName);
			}

			return new InvolvedTable(dataset.Name, GetOIDRowReferences(rows));
		}

		[NotNull]
		private static IEnumerable<RowReference> GetAlternateKeyRowReferences(
			[NotNull] IEnumerable<InvolvedRow> involvedRows,
			[NotNull] TableKeyLookup tableKeyLookup)
		{
			foreach (InvolvedRow row in involvedRows)
			{
				object keyValue;
				if (tableKeyLookup.TryLookupKey(row, out keyValue))
				{
					yield return new AlternateKeyRowReference(keyValue);
				}
			}
		}

		[NotNull]
		private static IEnumerable<RowReference> GetOIDRowReferences(
			[NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			foreach (InvolvedRow involvedRow in involvedRows)
			{
				if (! involvedRow.RepresentsEntireTable)
				{
					yield return new OIDRowReference(involvedRow.OID);
				}
			}
		}

		[CanBeNull]
		private TableKeyLookup GetTableRequiringIdLookup(
			[NotNull] InvolvedRow involvedRow,
			[NotNull] QualitySpecificationElement element)
		{
			if (involvedRow.RepresentsEntireTable)
			{
				return null;
			}

			IObjectDataset objectDataset = _datasetResolver.GetDatasetByInvolvedRowTableName(
				involvedRow.TableName, element.QualityCondition);

			Assert.NotNull(objectDataset,
			               "Unable to resolve object dataset for table name {0} and quality condition {1}",
			               involvedRow.TableName, element.QualityCondition.Name);

			return GetTableRequiringIdLookup(objectDataset);
		}

		[CanBeNull]
		private TableKeyLookup GetTableRequiringIdLookup(
			[NotNull] IObjectDataset objectDataset)
		{
			TableKeyLookup result;
			if (! _tableKeyLookups.TryGetValue(objectDataset, out result))
			{
				result = CreateTableRequiringIdLookup(objectDataset);
				_tableKeyLookups.Add(objectDataset, result);
			}

			return result;
		}

		[CanBeNull]
		private TableKeyLookup CreateTableRequiringIdLookup(
			[NotNull] IObjectDataset objectDataset)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));

			string involvedRowKeyField = GetAlternateKeyField(objectDataset);

			if (StringUtils.IsNullOrEmptyOrBlank(involvedRowKeyField))
			{
				return null;
			}

			ITable table = _datasetContext.OpenTable(objectDataset);

			if (table == null)
			{
				// TODO exception?
				return null;
			}

			int keyFieldIndex = table.FindField(involvedRowKeyField);
			if (keyFieldIndex < 0)
			{
				// TODO exception? incorrect configuration!
				return null;
			}

			IField field = table.Fields.Field[keyFieldIndex];

			if (field.Type == esriFieldType.esriFieldTypeOID)
			{
				// the oid field was specified as alternate ID
				return null;
			}

			return new TableKeyLookup(table, keyFieldIndex, field);
		}

		private void WriteIssueCore([NotNull] Issue issue, [CanBeNull] IGeometry geometry)
		{
			_issueRepository?.AddIssue(issue, geometry);

			_verificationReportBuilder.AddIssue(issue, geometry);
		}

		[CanBeNull]
		private string GetAlternateKeyField([NotNull] IObjectDataset objectDataset)
		{
			return _getAlternateKeyField?.Invoke(objectDataset);
		}

		private class PendingQaError
		{
			public PendingQaError(
				[NotNull] QaError error,
				[NotNull] QualitySpecificationElement qualitySpecificationElement,
				[CanBeNull] IGeometry errorGeometry)
			{
				Error = error;
				ErrorGeometry = errorGeometry;
				QualitySpecificationElement = qualitySpecificationElement;
			}

			[NotNull]
			public QaError Error { get; }

			[CanBeNull]
			public IGeometry ErrorGeometry { get; }

			[NotNull]
			public QualitySpecificationElement QualitySpecificationElement { get; }
		}

		private class TableKeyLookup
		{
			private readonly HashSet<int> _oidsRequiringLookup = new HashSet<int>();
			private readonly Dictionary<int, object> _keyMap = new Dictionary<int, object>();
			private readonly ITable _table;
			private readonly int _keyFieldIndex;

			public TableKeyLookup([NotNull] ITable table,
			                      int keyFieldIndex,
			                      [NotNull] IField keyField)
			{
				Assert.ArgumentNotNull(table, nameof(table));
				Assert.ArgumentNotNull(keyField, nameof(keyField));
				Assert.ArgumentCondition(keyFieldIndex >= 0, "invalid key field index");

				_table = table;
				_keyFieldIndex = keyFieldIndex;
				KeyFieldName = keyField.Name;
			}

			[NotNull]
			public string KeyFieldName { get; }

			public void AddObjectId(int oid)
			{
				_oidsRequiringLookup.Add(oid);
			}

			public void ClearObjectIds()
			{
				_oidsRequiringLookup.Clear();

				// TODO also clear the key map if it exceeds a certain size? or remove the least recently used entries?
			}

			[ContractAnnotation("=>false, keyValue:canbenull; =>true, keyValue:notnull")]
			public bool TryLookupKey([NotNull] InvolvedRow involvedRow,
			                         [CanBeNull] out object keyValue)
			{
				if (involvedRow.RepresentsEntireTable)
				{
					keyValue = null;
					return false;
				}

				if (_oidsRequiringLookup.Count > 0)
				{
					foreach (KeyValuePair<int, object> pair in LookupKeys(_oidsRequiringLookup))
					{
						object value = Assert.NotNull(pair.Value,
						                              "key is <null> for oid={0}, table={1}",
						                              pair.Key,
						                              DatasetUtils.GetName(_table));
						_keyMap[pair.Key] = value;
					}

					_oidsRequiringLookup.Clear();
				}

				return _keyMap.TryGetValue(involvedRow.OID, out keyValue);
			}

			[NotNull]
			private IEnumerable<KeyValuePair<int, object>> LookupKeys(
				[NotNull] IEnumerable<int> oids)
			{
				string oidFieldName = _table.OIDFieldName;

				var queryFilter = new QueryFilterClass();
				GdbQueryUtils.SetSubFields(queryFilter, oidFieldName, KeyFieldName);

				const bool recycle = true;
				foreach (IRow row in GdbQueryUtils.GetRowsInList(
					         _table, oidFieldName, oids, recycle, queryFilter))
				{
					yield return new KeyValuePair<int, object>(row.OID, row.Value[_keyFieldIndex]);
				}
			}
		}
	}
}
