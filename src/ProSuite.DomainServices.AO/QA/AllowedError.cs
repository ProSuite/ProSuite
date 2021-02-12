using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public class AllowedError : IComparable, IComparer<AllowedError>
	{
		[NotNull] private readonly QualityCondition _qualityCondition;
		private readonly DateTime _dateOfCreation;
		private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
		private readonly bool _usesGdbDatasetNames;
		private readonly int _oid;
		private readonly string _errorDescription;
		private readonly IList<InvolvedRow> _involvedRows;
		[NotNull] private readonly QaErrorGeometry _errorGeometry;
		private readonly int? _qualityConditionVersion;
		private readonly ITable _table;
		private List<InvolvedDatasetRow> _involvedDatasetRows;

		private readonly IDictionary<string, ICollection<int>>
			_involvedRowsByUnresolvedTableName =
				new Dictionary<string, ICollection<int>>();

		// mutable properties:
		private bool _invalidated;

		private bool _isUsed;

		public AllowedError([NotNull] QualityCondition qualityCondition,
		                    int? conditionVersion,
		                    [CanBeNull] IGeometry geometry,
		                    [NotNull] string errorDescription,
		                    [NotNull] IList<InvolvedRow> involvedRows,
		                    [NotNull] ITable table,
		                    int objectId,
		                    DateTime dateOfCreation,
		                    [NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(errorDescription, nameof(errorDescription));
			Assert.ArgumentNotNull(involvedRows, nameof(involvedRows));
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_qualityCondition = qualityCondition;
			_qualityConditionVersion = conditionVersion;
			_errorDescription = errorDescription;
			_involvedRows = involvedRows;
			_errorGeometry = new QaErrorGeometry(geometry);

			_table = table;
			_oid = objectId;
			_dateOfCreation = dateOfCreation;
			_datasetResolver = datasetResolver;

			_isUsed = false;
			_invalidated = false;
			_usesGdbDatasetNames = false; // uses involved row table name
		}

		/// <summary>
		/// Should only be used to search for the proper instance of the AllowedError
		/// in a list.
		/// </summary>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="qaError">The qa error.</param>
		/// <param name="datasetResolver">The dataset resolver.</param>
		/// <param name="usesGdbDatasetNames">if set to <c>true</c> [uses GDB dataset names].</param>
		internal AllowedError(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] QaError qaError,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			bool usesGdbDatasetNames)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_qualityCondition = qualityCondition;
			_errorDescription = qaError.Description;
			_errorGeometry = new QaErrorGeometry(qaError.Geometry);
			_involvedRows = qaError.InvolvedRows;
			_datasetResolver = datasetResolver;
			_usesGdbDatasetNames = usesGdbDatasetNames;
		}

		public ITable Table => _table;

		public int ObjectId => _oid;

		[NotNull]
		public QualityCondition QualityCondition => _qualityCondition;

		public int QualityConditionId => _qualityCondition.Id;

		public int? QualityConditionVersion => _qualityConditionVersion;

		public DateTime DateOfCreation => _dateOfCreation;

		public bool Invalidated
		{
			get { return _invalidated; }
			set { _invalidated = value; }
		}

		public bool IsUsed
		{
			get { return _isUsed; }
			set { _isUsed = value; }
		}

		[NotNull]
		public IList<InvolvedDatasetRow> InvolvedDatasetRows
		{
			get
			{
				EnsureInvolvedDatasetRows();

				return _involvedDatasetRows;
			}
		}

		[NotNull]
		public IDictionary<string, ICollection<int>> InvolvedRowsByUnresolvedTableName
		{
			get
			{
				EnsureInvolvedDatasetRows();

				return _involvedRowsByUnresolvedTableName;
			}
		}

		public GdbObjectReference GetObjectReference()
		{
			IObjectClass objectClass = (IObjectClass) Table;

			return new GdbObjectReference(
				objectClass.ObjectClassID, ObjectId);
		}

		[NotNull]
		private List<InvolvedDatasetRow> GetInvolvedDatasetRows()
		{
			var result = new List<InvolvedDatasetRow>(_involvedRows.Count);

			foreach (InvolvedRow involvedRow in _involvedRows)
			{
				string tableName = involvedRow.TableName;
				int oid = involvedRow.OID;

				ICollection<int> unresolvedRows;
				if (_involvedRowsByUnresolvedTableName.TryGetValue(tableName, out unresolvedRows))
				{
					if (! unresolvedRows.Contains(oid))
					{
						unresolvedRows.Add(oid);
					}

					continue;
				}

				IObjectDataset dataset = ResolveDataset(tableName);

				if (dataset == null)
				{
					// TODO logging
					_involvedRowsByUnresolvedTableName.Add(tableName, new List<int> {oid});
					continue;
				}

				result.Add(new InvolvedDatasetRow(dataset, oid));
			}

			result.Sort();

			return result;
		}

		private void EnsureInvolvedDatasetRows()
		{
			if (_involvedDatasetRows == null)
			{
				_involvedDatasetRows = GetInvolvedDatasetRows();
			}
		}

		[CanBeNull]
		private IObjectDataset ResolveDataset([NotNull] string tableName)
		{
			return _usesGdbDatasetNames
				       ? _datasetResolver.GetDatasetByGdbTableName(tableName, _qualityCondition)
				       : _datasetResolver.GetDatasetByInvolvedRowTableName(tableName,
				                                                           _qualityCondition);
		}

		private static int CompareInvolvedDatasetRows(
			[NotNull] IList<InvolvedDatasetRow> list0,
			[NotNull] IList<InvolvedDatasetRow> list1)
		{
			int involvedRowCount0 = list0.Count;

			if (involvedRowCount0 != list1.Count)
			{
				return involvedRowCount0 - list1.Count;
			}

			for (var i = 0; i < involvedRowCount0; i++)
			{
				int difference = list0[i].CompareTo(list1[i]);
				if (difference != 0)
				{
					return difference;
				}
			}

			return 0;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			var allowedError = obj as AllowedError;

			return allowedError == null
				       ? -1
				       : Compare(this, allowedError);
		}

		#endregion

		#region IComparer<AllowedError> Members

		public int Compare(AllowedError allowedError0, AllowedError allowedError1)
		{
			Assert.ArgumentNotNull(allowedError0, nameof(allowedError0));
			Assert.ArgumentNotNull(allowedError1, nameof(allowedError1));

			int qualityConditionIdDifference =
				allowedError0._qualityCondition.Id.CompareTo(
					allowedError1._qualityCondition.Id);

			if (qualityConditionIdDifference != 0)
			{
				return qualityConditionIdDifference;
			}

			int errorDifference = CompareAllowedErrors(allowedError0,
			                                           allowedError1);

			if (errorDifference != 0)
			{
				return errorDifference;
			}

			return CompareInvolvedDatasetRows(allowedError0.InvolvedDatasetRows,
			                                  allowedError1.InvolvedDatasetRows);
		}

		#endregion

		private static int CompareAllowedErrors([NotNull] AllowedError error0,
		                                        [NotNull] AllowedError error1)
		{
			Assert.ArgumentNotNull(error0, nameof(error0));
			Assert.ArgumentNotNull(error1, nameof(error1));

			// TODO: if the error has a derived geometry, then don't use the geometry for comparison. 
			//       (as it may be different depending on the current verification context (verified datasets) or 
			//        after irrelevant geometry changes to related features)
			//       https://issuetracker02.eggits.net/browse/PSM-162

			int involvedRowsCount = error0._involvedRows.Count;

			if (involvedRowsCount != error1._involvedRows.Count)
			{
				// different number of involved rows
				return involvedRowsCount - error1._involvedRows.Count;
			}

			// check if geometry box is the same
			int envelopeDifference = error0._errorGeometry.CompareEnvelope(error1._errorGeometry);
			if (envelopeDifference != 0)
			{
				return envelopeDifference;
			}

			// TODO compare error code / error attributes instead
			int descriptionDifference = Comparer<string>.Default.Compare(
				error0._errorDescription,
				error1._errorDescription);

			if (descriptionDifference != 0)
			{
				return descriptionDifference;
			}

			// no difference detected
			return 0;
		}
	}
}
