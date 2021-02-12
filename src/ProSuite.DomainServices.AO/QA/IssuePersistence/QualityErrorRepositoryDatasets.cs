using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	/// <summary>
	/// Encapsulates the access to the error datasets and their schema information.
	/// </summary>
	public class QualityErrorRepositoryDatasets
	{
		private readonly IVerificationContext _verificationContext;

		[CanBeNull]
		private IDictionary<esriGeometryType, IssueDatasetWriter> _issueWritersByGeometryType;

		private ISpatialReference _spatialReference;

		private string _fieldNameErrorType;
		private string _fieldNameDescription;
		private string _fieldNameQualityConditionId;

		private IFieldIndexCache _fieldIndexCache;

		public QualityErrorRepositoryDatasets(IVerificationContext verificationContext)
		{
			_verificationContext = verificationContext;
		}

		[NotNull]
		private IFieldIndexCache FieldIndexCache
		{
			get
			{
				if (_fieldIndexCache != null)
				{
					return _fieldIndexCache;
				}

				return _fieldIndexCache = new FieldIndexCache();
			}
		}

		/// <summary>
		/// Flushes any issueWriter's open insert cursors.
		/// </summary>
		public void SavePendingIssues()
		{
			if (_issueWritersByGeometryType == null)
			{
				return;
			}

			foreach (IssueDatasetWriter issueWriter in _issueWritersByGeometryType.Values)
			{
				const bool releaseInsertCursor = true;
				issueWriter.Flush(releaseInsertCursor);
			}
		}

		#region Get IssueDatasetWriters

		[NotNull]
		public IEnumerable<IssueDatasetWriter> GetIssueWriters()
		{
			return GetIssueWritersByGeometryType().Values;
		}

		public IEnumerable<ITable> GetIssueTables()
		{
			return GetIssueWriters().Select(w => w.Table);
		}

		[NotNull]
		public IssueDatasetWriter GetIssueWriter([CanBeNull] IGeometry forGeometryToBeStored)
		{
			if (forGeometryToBeStored == null)
			{
				return GetIssueWriterNoGeometry();
			}

			IssueDatasetWriter result = GetIssueWriter(forGeometryToBeStored.GeometryType);
			if (result != null)
			{
				return result;
			}

			throw new ArgumentOutOfRangeException(
				nameof(forGeometryToBeStored),
				string.Format(@"No issue dataset found for geometry type: {0}.",
				              forGeometryToBeStored.GeometryType));
		}

		[NotNull]
		public IssueDatasetWriter GetIssueWriterNoGeometry()
		{
			IssueDatasetWriter issueTable = GetIssueWriter(esriGeometryType.esriGeometryNull);

			Assert.NotNull(issueTable, "No table for issues without geometry defined");

			return issueTable;
		}

		[CanBeNull]
		public IssueDatasetWriter GetIssueWriter(esriGeometryType storedGeometryType)
		{
			IDictionary<esriGeometryType, IssueDatasetWriter> tablesByGeometryType =
				GetIssueWritersByGeometryType();

			IssueDatasetWriter result;
			return tablesByGeometryType.TryGetValue(storedGeometryType, out result)
				       ? result
				       : null;
		}

		[NotNull]
		public IDictionary<esriGeometryType, IssueDatasetWriter> GetIssueWritersByGeometryType()
		{
			return _issueWritersByGeometryType ??
			       (_issueWritersByGeometryType =
				        CreateIssueWritersByGeometryType(_verificationContext,
				                                         FieldIndexCache));
		}

		[NotNull]
		private static IDictionary<esriGeometryType, IssueDatasetWriter>
			CreateIssueWritersByGeometryType(
				[NotNull] IVerificationContext verificationContext,
				[NotNull] IFieldIndexCache fieldIndexCache)
		{
			var result = new Dictionary<esriGeometryType, IssueDatasetWriter>();

			foreach (KeyValuePair<esriGeometryType, IErrorDataset> pair in
				VerificationContextUtils.GetIssueDatasetsByGeometryType(verificationContext))
			{
				IssueDatasetWriter issueWriter = CreateIssueWriter(pair.Value,
				                                                   verificationContext,
				                                                   fieldIndexCache);

				if (issueWriter != null)
				{
					result.Add(pair.Key, issueWriter);
				}
			}

			return result;
		}

		[CanBeNull]
		private static IssueDatasetWriter CreateIssueWriter(
			[NotNull] IErrorDataset objectDataset,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IFieldIndexCache fieldIndexCache)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(fieldIndexCache, nameof(fieldIndexCache));

			ITable table = datasetContext.OpenTable(objectDataset);

			return table == null
				       ? null
				       : new IssueDatasetWriter(table, objectDataset, fieldIndexCache);
		}

		#endregion

		#region Model information

		[NotNull]
		public string FieldNameErrorType => _fieldNameErrorType ??
		                                    (_fieldNameErrorType =
			                                     GetFieldName(AttributeRole.ErrorErrorType));

		[NotNull]
		public string FieldNameDescription => _fieldNameDescription ??
		                                      (_fieldNameDescription =
			                                       GetFieldName(
				                                       AttributeRole.ErrorDescription));

		[NotNull]
		public string FieldNameQualityConditionId => _fieldNameQualityConditionId ??
		                                             (_fieldNameQualityConditionId =
			                                              GetFieldName(
				                                              AttributeRole.ErrorConditionId));

		[NotNull]
		public ISpatialReference SpatialReference => _spatialReference ??
		                                             (_spatialReference =
			                                              GetUniqueErrorFeatureClassSpatialReference
				                                              ());

		[NotNull]
		internal string GetFieldName([NotNull] AttributeRole attributeRole)
		{
			// TODO: Consider implementing a configuration consistency check at the beginning

			string fieldName = null;

			foreach (IssueDatasetWriter issueWriter in GetIssueWriters())
			{
				if (fieldName == null)
				{
					fieldName = issueWriter.GetAttribute(attributeRole).Name;
				}
				else if (! fieldName.Equals(
					         issueWriter.GetAttribute(attributeRole).Name,
					         StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidConfigurationException(
						string.Format(
							"The field with role '{0}' must have the same name in all issue tables.",
							attributeRole));
				}
			}

			Assert.NotNull(fieldName, "Field for role '{0}' not found in issue datasets",
			               attributeRole);

			return fieldName;
		}

		[NotNull]
		private ISpatialReference GetUniqueErrorFeatureClassSpatialReference()
		{
			ISpatialReference result = null;

			foreach (IssueDatasetWriter issueWriter in GetIssueWriters())
			{
				if (issueWriter.SpatialReference == null)
				{
					continue;
				}

				if (result == null)
				{
					result = issueWriter.SpatialReference;
				}
				else
				{
					const bool compareTolerances = false;
					if (! SpatialReferenceUtils.AreEqualXYZ(result,
					                                        issueWriter.SpatialReference,
					                                        compareTolerances))
					{
						throw new InvalidOperationException(
							"The spatial references of all issue feature classes " +
							"(including xy and z precision and tolerances) must be equal");
					}
				}
			}

			return Assert.NotNull(result, "spatialReference");
		}

		#endregion
	}
}
