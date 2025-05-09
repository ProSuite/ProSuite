using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	public class IssueDatasetWriter
	{
		#region Field declarations

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly IFeatureClass _featureClass;

		[NotNull] private readonly IErrorDataset _issueObjectDataset;
		[NotNull] private readonly IFieldIndexCache _fieldIndexCache;
		[CanBeNull] private ISpatialReference _spatialReference;
		[CanBeNull] private ICursor _insertCursor;
		[CanBeNull] private IRowBuffer _rowBuffer;
		private int _bufferedPointCount;

		[NotNull] private readonly Dictionary<AttributeRole, int> _fieldIndexesByRole =
			new Dictionary<AttributeRole, int>();

		[NotNull] private readonly Dictionary<AttributeRole, int> _fieldLengthByRole =
			new Dictionary<AttributeRole, int>();

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueDatasetWriter"/> class.
		/// </summary>
		/// <param name="issueFeatureClass">The error feature class.</param>
		/// <param name="issueObjectDataset">The error object dataset.</param>
		/// <param name="fieldIndexCache">The field index cache.</param>
		public IssueDatasetWriter([NotNull] IFeatureClass issueFeatureClass,
		                          [NotNull] IErrorDataset issueObjectDataset,
		                          [NotNull] IFieldIndexCache fieldIndexCache)
			: this((ITable) issueFeatureClass, issueObjectDataset, fieldIndexCache) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueDatasetWriter"/> class.
		/// </summary>
		/// <param name="issueTable">The error table.</param>
		/// <param name="issueObjectDataset">The error object dataset.</param>
		/// <param name="fieldIndexCache">The field index cache.</param>
		public IssueDatasetWriter([NotNull] ITable issueTable,
		                          [NotNull] IErrorDataset issueObjectDataset,
		                          [NotNull] IFieldIndexCache fieldIndexCache)
		{
			Assert.ArgumentNotNull(issueTable, nameof(issueTable));
			Assert.ArgumentNotNull(issueObjectDataset, nameof(issueObjectDataset));
			Assert.ArgumentNotNull(fieldIndexCache, nameof(fieldIndexCache));

			Table = issueTable;
			_issueObjectDataset = issueObjectDataset;
			_fieldIndexCache = fieldIndexCache;

			_featureClass = Table as IFeatureClass;

			HasM = _featureClass != null && DatasetUtils.HasM(_featureClass);
			HasZ = _featureClass != null && DatasetUtils.HasZ(_featureClass);
		}

		#region Public properties

		public bool HasZ { get; }

		public bool HasM { get; }

		[PublicAPI]
		public int AutoCommitInterval { get; set; } = 500;

		[PublicAPI]
		public int MaxBufferedPointCount { get; set; } = 10000;

		[NotNull]
		public ITable Table { get; }

		public ISpatialReference SpatialReference
		{
			get
			{
				if (_spatialReference == null && _featureClass != null)
				{
					_spatialReference = ((IGeoDataset) _featureClass).SpatialReference;
				}

				return _spatialReference;
			}
		}

		public int InsertedRowCount { get; private set; }

		public string DatasetName => _issueObjectDataset.Name;

		#endregion

		#region Public methods

		public void Flush(bool releaseInsertCursor)
		{
			if (_insertCursor == null)
			{
				return;
			}

			_insertCursor.Flush();
			InsertedRowCount = 0;
			_bufferedPointCount = 0;

			if (releaseInsertCursor)
			{
				if (_rowBuffer != null)
				{
					Marshal.ReleaseComObject(_rowBuffer);
				}

				Marshal.ReleaseComObject(_insertCursor);
				_insertCursor = null;
				_rowBuffer = null;
			}
		}

		[NotNull]
		public IRowBuffer GetRowBuffer()
		{
			if (_insertCursor == null)
			{
				_insertCursor = Table.Insert(true);
				_rowBuffer = Table.CreateRowBuffer();

				InsertedRowCount = 0;
				_bufferedPointCount = 0;
			}

			return Assert.NotNull(_rowBuffer);
		}

		public void InsertRow()
		{
			Assert.NotNull(_rowBuffer, "no row buffer");
			Assert.NotNull(_insertCursor, "insert cursor is null");

			try
			{
				_insertCursor.InsertRow(_rowBuffer);
			}
			catch (Exception ex)
			{
				if (_rowBuffer is IFeatureBuffer && ex is COMException)
				{
					// this exception can be caught to retry with simplified issue geometry
					throw new IssueFeatureInsertionFailedException(
						string.Format("Error inserting issue feature into table {0}: {1}",
						              _issueObjectDataset.Name,
						              GetBufferDiagnostics(_rowBuffer)),
						ex);
				}

				// unknown error
				throw new InvalidOperationException(
					string.Format("Unknown error inserting issue object into table {0}: {1}",
					              _issueObjectDataset.Name,
					              GetBufferDiagnostics(_rowBuffer)),
					ex);
			}

			// the insert succeeded

			InsertedRowCount++;
			_bufferedPointCount += GetPointCount(_rowBuffer);

			if (InsertedRowCount >= AutoCommitInterval ||
			    _bufferedPointCount > MaxBufferedPointCount)
			{
				const bool releaseInsertCursor = false;
				Flush(releaseInsertCursor);
			}
		}

		private static int GetPointCount([NotNull] IRowBuffer rowBuffer)
		{
			var featureBuffer = rowBuffer as IFeatureBuffer;

			return featureBuffer == null
				       ? 0
				       : GeometryUtils.GetPointCount(featureBuffer.Shape);
		}

		[NotNull]
		public IList<InvolvedRow> GetInvolvedRows([NotNull] IRow errorRow)
		{
			return RowParser.Parse(GetString(errorRow, AttributeRole.ErrorObjects));
		}

		public T? Get<T>([NotNull] IRow errorRow,
		                 [NotNull] AttributeRole role) where T : struct
		{
			return GdbObjectUtils.ReadRowValue<T>(errorRow, GetFieldIndex(role));
		}

		[NotNull]
		public string GetString([NotNull] IRow errorRow,
		                        [NotNull] AttributeRole role)
		{
			const bool roleIsOptional = false;
			return Assert.NotNull(GetString(errorRow, role, roleIsOptional));
		}

		[CanBeNull]
		public string GetString([NotNull] IRow errorRow,
		                        [NotNull] AttributeRole role,
		                        bool roleIsOptional)
		{
			int fieldIndex = GetFieldIndex(role, roleIsOptional);
			if (fieldIndex < 0)
			{
				return null;
			}

			object obj = errorRow.Value[fieldIndex];

			return obj == null || obj is DBNull
				       ? string.Empty
				       : Convert.ToString(obj);
		}

		public void WriteValue([NotNull] IRowBuffer rowBuffer,
		                       [NotNull] AttributeRole role,
		                       [CanBeNull] object value)
		{
			int fieldIndex = GetFieldIndex(role);

			rowBuffer.set_Value(fieldIndex, value ?? DBNull.Value);
		}

		public void WriteValue([NotNull] IRowBuffer rowBuffer,
		                       [NotNull] AttributeRole role,
		                       [CanBeNull] string text)
		{
			int fieldIndex = GetFieldIndex(role);

			WriteTextValue(text, rowBuffer, fieldIndex);
		}

		// additional WriteValue(string fieldName) could be considered with cached field index dictionary.

		public void WriteValue([NotNull] IRowBuffer rowBuffer,
		                       [NotNull] string fieldName,
		                       [CanBeNull] string text)
		{
			int fieldIndex = Table.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(fieldName), fieldName,
				                                      string.Format(
					                                      "Field name does not exist in table {0}.",
					                                      DatasetUtils.GetName(Table)));
			}

			WriteTextValue(text, rowBuffer, fieldIndex);
		}

		public bool HasAttribute([NotNull] AttributeRole attributeRole)
		{
			return _issueObjectDataset.GetAttribute(attributeRole) != null;
		}

		[NotNull]
		public ObjectAttribute GetAttribute([NotNull] AttributeRole attributeRole)
		{
			const bool roleIsOptional = false;
			return Assert.NotNull(GetAttribute(attributeRole, roleIsOptional));
		}

		[CanBeNull]
		private ObjectAttribute GetAttribute([NotNull] AttributeRole attributeRole,
		                                     bool roleIsOptional)
		{
			// TODO: Test if there are uow problems here
			ObjectAttribute attribute = _issueObjectDataset.GetAttribute(attributeRole);

			if (attribute != null || roleIsOptional)
			{
				return attribute;
			}

			throw new InvalidConfigurationException(
				string.Format("Attribute role {0} does not exist in {1}",
				              attributeRole, _issueObjectDataset.Name));
		}

		public int GetFieldLength([NotNull] AttributeRole role)
		{
			int length;
			if (! _fieldLengthByRole.TryGetValue(role, out length))
			{
				IField field = Table.Fields.Field[GetFieldIndex(role)];

				length = field.Length;

				_fieldLengthByRole.Add(role, length);
			}

			return length;
		}

		public void DeleteErrorObjects([NotNull] IQueryFilter filter)
		{
			Assert.ArgumentNotNull(filter, nameof(filter));

			IQueryFilter tableSpecificFilter = AdaptFilterToErrorTable(filter);

			DatasetUtils.DeleteRowsByFilter(Table, tableSpecificFilter);
		}

		public void DeleteErrorObjects(
			[NotNull] IQueryFilter filter,
			[NotNull] IDeletableErrorRowFilter deletableErrorRowFilter,
			[NotNull] IDictionary<int, QualityCondition> qualityConditionsById)
		{
			Assert.ArgumentNotNull(filter, nameof(filter));
			Assert.ArgumentNotNull(deletableErrorRowFilter, nameof(deletableErrorRowFilter));
			Assert.ArgumentNotNull(qualityConditionsById, nameof(qualityConditionsById));

			Stopwatch watch = _msg.DebugStartTiming();

			string subFieldsBefore = filter.SubFields;

			IQueryFilter tableSpecificFilter = AdaptFilterToErrorTable(filter);

			var count = 0;
			const bool recycle = true;
			ICursor cursor = null;
			try
			{
				cursor = Table.Update(tableSpecificFilter, recycle);

				for (IRow row = cursor.NextRow();
				     row != null;
				     row = cursor.NextRow())
				{
					int? qualityConditionId = Get<int>(row, AttributeRole.ErrorConditionId);

					if (qualityConditionId != null)
					{
						QualityCondition qualityCondition;
						if (! qualityConditionsById.TryGetValue(qualityConditionId.Value,
						                                        out qualityCondition))
						{
							// the quality condition is not in the specified set, or the id was invalid
							// -> don't delete
							// NOTE: if the set of quality conditions is guaranteed to be complete then deletion would be possible
							continue;
						}

						if (! deletableErrorRowFilter.IsDeletable(row, qualityCondition))
						{
							// the error is not deletable
							continue;
						}
					}

					// The error is deletable for it's quality condition, or it does not have a quality condition id
					// -> delete it
					cursor.DeleteRow();
					count++;
				}
			}
			finally
			{
				if (cursor != null)
				{
					Marshal.ReleaseComObject(cursor);
				}

				// Side effect: For PostGIS tables the Search() method can change the SubFields
				if (filter.SubFields != subFieldsBefore)
				{
					filter.SubFields = subFieldsBefore;
				}
			}

			_msg.DebugStopTiming(watch,
			                     "Deleted {0} error(s) in {1} based on deletable row filter",
			                     count, DatasetName);
		}

		public int DeleteOrphanedErrorObjects([NotNull] IQueryFilter filter,
		                                      [NotNull] ICollection<int> qualityConditionIds)
		{
			Assert.ArgumentNotNull(filter, nameof(filter));
			Assert.ArgumentNotNull(qualityConditionIds, nameof(qualityConditionIds));

			Stopwatch watch = _msg.DebugStartTiming("Deleting orphaned errors in {0}",
			                                        DatasetName);

			string subFieldsBefore = filter.SubFields;

			IQueryFilter tableSpecificFilter = AdaptFilterToErrorTable(filter);

			int fieldIndex = GetFieldIndex(AttributeRole.ErrorConditionId);

			var count = 0;
			const bool recycle = true;
			ICursor cursor = null;
			try
			{
				cursor = Table.Update(tableSpecificFilter, recycle);

				for (IRow row = cursor.NextRow();
				     row != null;
				     row = cursor.NextRow())
				{
					object qualityConditionId = row.Value[fieldIndex];

					bool isOrphaned = qualityConditionId == null ||
					                  qualityConditionId is DBNull ||
					                  ! qualityConditionIds.Contains(
						                  Convert.ToInt32(qualityConditionId));

					if (isOrphaned)
					{
						count++;
						cursor.DeleteRow();
					}
				}
			}
			finally
			{
				if (cursor != null)
				{
					ComUtils.ReleaseComObject(cursor);
				}

				// Side effect: For PostGIS tables the Search() method can change the SubFields
				if (filter.SubFields != subFieldsBefore)
				{
					filter.SubFields = subFieldsBefore;
				}
			}

			_msg.DebugStopTiming(watch, "Deleted {0} error(s)", count);

			return count;
		}

		#endregion

		private int GetFieldIndex([NotNull] AttributeRole role)
		{
			const bool roleIsOptional = false;
			return GetFieldIndex(role, roleIsOptional);
		}

		private int GetFieldIndex([NotNull] AttributeRole role, bool roleIsOptional)
		{
			int fieldIndex;
			if (! _fieldIndexesByRole.TryGetValue(role, out fieldIndex))
			{
				fieldIndex = GetFieldIndexCore(role, roleIsOptional);
				_fieldIndexesByRole.Add(role, fieldIndex);
			}

			return fieldIndex;
		}

		private int GetFieldIndexCore([NotNull] AttributeRole role, bool roleIsOptional)
		{
			ObjectAttribute attribute = GetAttribute(role, roleIsOptional);

			if (attribute == null)
			{
				return -1;
			}

			int fieldIndex = AttributeUtils.GetFieldIndex(Table, attribute, _fieldIndexCache);

			if (fieldIndex >= 0 || roleIsOptional)
			{
				return fieldIndex;
			}

			throw new ArgumentException(string.Format("Field {0} not found", attribute.Name));
		}

		[NotNull]
		private static string GetBufferDiagnostics([NotNull] IRowBuffer rowBuffer)
		{
			// TODO: consider changing GdbObjectUtils.ObjectToString() to accept rowBuffer / row
			var sb = new StringBuilder();

			IFields fields = rowBuffer.Fields;
			int fieldCount = fields.FieldCount;

			for (var i = 0; i < fieldCount; i++)
			{
				sb.AppendFormat("{0}: ", fields.Field[i].Name);

				if (rowBuffer.Value[i] is IGeometry)
				{
					sb.AppendFormat("{0}",
					                GeometryUtils.ToString(
						                rowBuffer.Value[i] as IGeometry));
				}
				else if (rowBuffer.Value[i] != DBNull.Value)
				{
					object value = rowBuffer.Value[i];
					sb.AppendFormat("{0}", value);
				}
				else
				{
					sb.Append("<null>");
				}

				sb.Append("; ");
			}

			return sb.ToString();
		}

		[NotNull]
		private IQueryFilter AdaptFilterToErrorTable([NotNull] IQueryFilter filter)
		{
			Assert.ArgumentNotNull(filter, nameof(filter));

			var spatialFilter = filter as ISpatialFilter;

			if (spatialFilter == null)
			{
				return filter;
			}

			if (_featureClass == null)
			{
				// translate into non-Spatial Filter
				return new QueryFilterClass
				       {
					       SubFields = filter.SubFields,
					       WhereClause = filter.WhereClause
				       };
			}

			spatialFilter.GeometryField = _featureClass.ShapeFieldName;

			return spatialFilter;
		}

		private void WriteTextValue([CanBeNull] string text,
		                            [NotNull] IRowBuffer toRow,
		                            int fieldIndex)
		{
			IField field = toRow.Fields.Field[fieldIndex];

			if (text != null && text.Length > field.Length)
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat(
						"The text '{0}' is too long for the text field in the table {1}.",
						text,
						_issueObjectDataset.Name);
				}

				text = text.Substring(0, field.Length - 3) + "...";
			}

			toRow.Value[fieldIndex] = text ?? (object) DBNull.Value;
		}
	}
}
