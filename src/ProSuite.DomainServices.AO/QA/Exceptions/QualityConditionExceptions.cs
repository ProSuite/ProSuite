using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class QualityConditionExceptions
	{
		private readonly IBox _areaOfInterestBox;
		private readonly Guid _qualityConditionVersionGuid;
		[CanBeNull] private ExceptionObjectSpatialIndex _spatialIndex;
		[CanBeNull] private ExceptionObjectInvolvedRowsIndex _noGeometryInvolvedRowsIndex;

		[CanBeNull] private ExceptionObjectInvolvedRowsIndex
			_ignoredGeometryInvolvedRowsIndex;

		[NotNull] private readonly ExceptionObjectAffectedComponentPredicate
			_affectedComponentPredicate = new ExceptionObjectAffectedComponentPredicate();

		[NotNull] private readonly ExceptionObjectValuesPredicate _valuesPredicate =
			new ExceptionObjectValuesPredicate();

		[NotNull] private readonly ExceptionObjectIssueCodePredicate _issueCodePredicate =
			new ExceptionObjectIssueCodePredicate();

		[NotNull] private readonly ExceptionObjectInvolvedRowsPredicate
			_involvedRowsPredicate;

		[CanBeNull] private readonly InvolvedObjectsIgnoredDatasetPredicate
			_ignoredDatasetsPredicate;

		public QualityConditionExceptions(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] InvolvedObjectsMatchCriteria involvedObjectsMatchCriteria,
			[CanBeNull] IBox areaOfInterestBox)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_areaOfInterestBox = areaOfInterestBox;
			_qualityConditionVersionGuid = new Guid(qualityCondition.VersionUuid);

			_ignoredDatasetsPredicate = new InvolvedObjectsIgnoredDatasetPredicate(
				qualityCondition, datasetResolver, involvedObjectsMatchCriteria);

			_involvedRowsPredicate = new ExceptionObjectInvolvedRowsPredicate(
				_ignoredDatasetsPredicate.IgnoreDataset);
		}

		public QualityConditionExceptions(Guid qualityConditionVersionUuid,
		                                  [CanBeNull] IBox areaOfInterestBox)
		{
			_areaOfInterestBox = areaOfInterestBox;
			_qualityConditionVersionGuid = qualityConditionVersionUuid;

			_involvedRowsPredicate = new ExceptionObjectInvolvedRowsPredicate(null);
		}

		public void Add([NotNull] ExceptionObject exceptionObject)
		{
			if (exceptionObject.QualityConditionVersionUuid != _qualityConditionVersionGuid)
			{
				// TODO log? add to exception statistics?
				return;
			}

			// index exceptions			
			if (exceptionObject.ShapeEnvelope == null)
			{
				if (_noGeometryInvolvedRowsIndex == null)
				{
					_noGeometryInvolvedRowsIndex = new ExceptionObjectInvolvedRowsIndex(
						_ignoredDatasetsPredicate == null
							? (Predicate<string>) null
							: _ignoredDatasetsPredicate.IgnoreDataset);
				}

				_noGeometryInvolvedRowsIndex.Add(exceptionObject);
			}
			else if (exceptionObject.ShapeMatchCriterion == ShapeMatchCriterion.IgnoreShape)
			{
				if (_ignoredGeometryInvolvedRowsIndex == null)
				{
					_ignoredGeometryInvolvedRowsIndex = new ExceptionObjectInvolvedRowsIndex(
						_ignoredDatasetsPredicate == null
							? (Predicate<string>) null
							: _ignoredDatasetsPredicate.IgnoreDataset);
				}

				_ignoredGeometryInvolvedRowsIndex.Add(exceptionObject);
			}
			else
			{
				if (_spatialIndex == null)
				{
					_spatialIndex = new ExceptionObjectSpatialIndex(_areaOfInterestBox);
				}

				_spatialIndex.Add(exceptionObject);
			}
		}

		[ContractAnnotation(
			"=>true, exceptionObject:notnull; =>false, exceptionObject:canbenull")]
		public bool ExistsExceptionFor([NotNull] QaError qaError,
		                               [CanBeNull] out ExceptionObject exceptionObject)
		{
			foreach (ExceptionObjectCandidate candidate in GetCandidates(qaError))
			{
				if (! Matches(qaError, candidate.ExceptionObject,
				              ! candidate.InvolvedRowsPredicateApplied))
				{
					continue;
				}

				exceptionObject = candidate.ExceptionObject;
				return true;
			}

			exceptionObject = null;
			return false;
		}

		public IEnumerable<ExceptionObject> GetMatchingExceptions(
			[NotNull] ExceptionObject exceptionObject)
		{
			foreach (ExceptionObjectCandidate candidate in GetCandidates(exceptionObject))
			{
				if (Matches(exceptionObject,
				            candidate.ExceptionObject,
				            ! candidate.InvolvedRowsPredicateApplied))
				{
					yield return candidate.ExceptionObject;
				}
			}
		}

		[NotNull]
		private IEnumerable<ExceptionObjectCandidate> GetCandidates(
			[NotNull] QaError qaError)
		{
			IGeometry issueGeometry = qaError.Geometry;

			if (issueGeometry != null && ! issueGeometry.IsEmpty)
			{
				if (_spatialIndex != null)
				{
					foreach (ExceptionObject candidate in _spatialIndex.Search(issueGeometry))
					{
						yield return new ExceptionObjectCandidate(candidate, false);
					}
				}

				if (_ignoredGeometryInvolvedRowsIndex != null)
				{
					foreach (
						ExceptionObject candidate in _ignoredGeometryInvolvedRowsIndex.Search(
							qaError))
					{
						yield return new ExceptionObjectCandidate(candidate, true);
					}
				}
			}
			else
			{
				// the error has no geometry
				if (_noGeometryInvolvedRowsIndex != null)
				{
					foreach (
						ExceptionObject candidate in _noGeometryInvolvedRowsIndex.Search(qaError)
					)
					{
						yield return new ExceptionObjectCandidate(candidate, true);
					}
				}
			}
		}

		[NotNull]
		private IEnumerable<ExceptionObjectCandidate> GetCandidates(
			[NotNull] ExceptionObject exceptionObject)
		{
			if (exceptionObject.ShapeEnvelope != null)
			{
				if (_spatialIndex != null)
				{
					foreach (ExceptionObject candidate in _spatialIndex.Search(exceptionObject))
					{
						yield return new ExceptionObjectCandidate(candidate, false);
					}
				}

				if (_ignoredGeometryInvolvedRowsIndex != null)
				{
					foreach (ExceptionObject candidate in
						_ignoredGeometryInvolvedRowsIndex.Search(exceptionObject))
					{
						yield return new ExceptionObjectCandidate(candidate, true);
					}
				}
			}
			else
			{
				// the error has no geometry
				if (_noGeometryInvolvedRowsIndex != null)
				{
					foreach (ExceptionObject candidate in
						_noGeometryInvolvedRowsIndex.Search(exceptionObject)
					)
					{
						yield return new ExceptionObjectCandidate(candidate, true);
					}
				}
			}
		}

		private bool Matches([NotNull] ExceptionObject searchExceptionObject,
		                     [NotNull] ExceptionObject exceptionObject,
		                     bool applyInvolvedRowsPredicate)
		{
			return (! applyInvolvedRowsPredicate ||
			        _involvedRowsPredicate.Matches(exceptionObject, searchExceptionObject)) &&
			       _issueCodePredicate.Matches(exceptionObject, searchExceptionObject) &&
			       _affectedComponentPredicate.Matches(exceptionObject,
			                                           searchExceptionObject) &&
			       _valuesPredicate.Matches(exceptionObject, searchExceptionObject);
		}

		private bool Matches([NotNull] QaError qaError,
		                     [NotNull] ExceptionObject exceptionObject,
		                     bool applyInvolvedRowsPredicate)
		{
			return (! applyInvolvedRowsPredicate ||
			        _involvedRowsPredicate.Matches(exceptionObject, qaError)) &&
			       _issueCodePredicate.Matches(exceptionObject, qaError) &&
			       _affectedComponentPredicate.Matches(exceptionObject, qaError) &&
			       _valuesPredicate.Matches(exceptionObject, qaError);
		}

		private class ExceptionObjectCandidate
		{
			private readonly ExceptionObject _exceptionObject;
			private readonly bool _involvedRowsPredicateApplied;

			public ExceptionObjectCandidate([NotNull] ExceptionObject exceptionObject,
			                                bool involvedRowsPredicateApplied)
			{
				_exceptionObject = exceptionObject;
				_involvedRowsPredicateApplied = involvedRowsPredicateApplied;
			}

			public ExceptionObject ExceptionObject => _exceptionObject;

			public bool InvolvedRowsPredicateApplied => _involvedRowsPredicateApplied;
		}

		private class InvolvedObjectsIgnoredDatasetPredicate
		{
			[NotNull] private readonly QualityCondition _qualityCondition;
			[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
			[CanBeNull] private readonly HashSet<IObjectDataset> _ignoredObjectDatasets;

			public InvolvedObjectsIgnoredDatasetPredicate(
				[NotNull] QualityCondition qualityCondition,
				[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
				[CanBeNull] InvolvedObjectsMatchCriteria criteria)
			{
				_qualityCondition = qualityCondition;
				_datasetResolver = datasetResolver;

				_ignoredObjectDatasets = GetIgnoredObjectDatasets(qualityCondition, criteria);
			}

			[CanBeNull]
			private static HashSet<IObjectDataset> GetIgnoredObjectDatasets(
				[NotNull] QualityCondition qualityCondition,
				[CanBeNull] InvolvedObjectsMatchCriteria criteria)
			{
				if (criteria == null)
				{
					return null;
				}

				HashSet<IObjectDataset> result = null;

				foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues())
				{
					var objectDataset = dataset as IObjectDataset;
					if (objectDataset == null || ! criteria.IgnoreDataset(objectDataset))
					{
						continue;
					}

					if (result == null)
					{
						result = new HashSet<IObjectDataset>();
					}

					result.Add(objectDataset);
				}

				return result;
			}

			public bool IgnoreDataset([NotNull] string tableName)
			{
				if (_ignoredObjectDatasets == null)
				{
					return false;
				}

				IObjectDataset objectDataset =
					_datasetResolver.GetDatasetByInvolvedRowTableName(tableName,
					                                                  _qualityCondition);

				return objectDataset != null && _ignoredObjectDatasets.Contains(objectDataset);
			}
		}
	}
}
