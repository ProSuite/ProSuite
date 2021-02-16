using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectEvaluator : IExceptionObjectEvaluator
	{
		[NotNull] private readonly IDictionary<Guid, QualityCondition> _conditionsByUuid;

		[NotNull] private readonly IExceptionEvaluationStatistics
			_exceptionEvaluationStatistics;

		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;

		[CanBeNull] private readonly InvolvedObjectsMatchCriteria
			_involvedObjectsMatchCriteria;

		[CanBeNull] private readonly IBox _aoiBox;

		[NotNull] private readonly IDictionary<Guid, QualityConditionExceptions>
			_qualityConditionExceptions = new Dictionary<Guid, QualityConditionExceptions>();

		public ExceptionObjectEvaluator(
			[NotNull] IEnumerable<ExceptionObject> exceptionObjects,
			[NotNull] IDictionary<Guid, QualityCondition> conditionsByUuid,
			[NotNull] IExceptionEvaluationStatistics exceptionEvaluationStatistics,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] InvolvedObjectsMatchCriteria involvedObjectsMatchCriteria,
			[CanBeNull] IGeometry areaOfInterest)
		{
			Assert.ArgumentNotNull(exceptionObjects, nameof(exceptionObjects));
			Assert.ArgumentNotNull(conditionsByUuid, nameof(conditionsByUuid));
			Assert.ArgumentNotNull(exceptionEvaluationStatistics,
			                       nameof(exceptionEvaluationStatistics));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_conditionsByUuid = conditionsByUuid;
			_exceptionEvaluationStatistics = exceptionEvaluationStatistics;
			_datasetResolver = datasetResolver;
			_involvedObjectsMatchCriteria = involvedObjectsMatchCriteria;

			_aoiBox = areaOfInterest == null || areaOfInterest.IsEmpty
				          ? null
				          : QaGeometryUtils.CreateBox(areaOfInterest.Envelope);

			foreach (ExceptionObject exceptionObject in exceptionObjects)
			{
				Add(exceptionObject);
			}
		}

		private void Add([NotNull] ExceptionObject exceptionObject)
		{
			Guid uuid = exceptionObject.QualityConditionUuid;

			QualityCondition qualityCondition;
			if (! _conditionsByUuid.TryGetValue(uuid, out qualityCondition))
			{
				// unknown exception object
				return;
			}

			QualityConditionExceptions qualityConditionExceptions;
			if (! _qualityConditionExceptions.TryGetValue(uuid,
			                                              out qualityConditionExceptions))
			{
				qualityConditionExceptions = new QualityConditionExceptions(
					qualityCondition, _datasetResolver, _involvedObjectsMatchCriteria,
					_aoiBox);

				_qualityConditionExceptions.Add(uuid, qualityConditionExceptions);
			}

			_exceptionEvaluationStatistics.AddExceptionObject(
				exceptionObject, qualityCondition);

			qualityConditionExceptions.Add(exceptionObject);
		}

		public bool ExistsExceptionFor(QaError qaError,
		                               QualitySpecificationElement element,
		                               out ExceptionObject exceptionObject)
		{
			var uuid = new Guid(element.QualityCondition.Uuid);

			// TODO if the error exceeds the verification extent, compare with envelope by "contained envelope" criterion instead of "equal envelope"
			// Reason: some tests cut off the error geometry at the verification extent

			QualityConditionExceptions qualityConditionExceptions;
			if (_qualityConditionExceptions.TryGetValue(uuid, out qualityConditionExceptions))
			{
				if (qualityConditionExceptions.ExistsExceptionFor(qaError, out exceptionObject))
				{
					_exceptionEvaluationStatistics.AddUsedException(
						exceptionObject, element, qaError);
					return true;
				}

				return false;
			}

			// no exceptions exist for this quality condition
			exceptionObject = null;
			return false;
		}

		public int GetUsageCount([NotNull] ExceptionObject exceptionObject)
		{
			Guid uuid = exceptionObject.QualityConditionUuid;

			QualityCondition qualityCondition;
			if (! _conditionsByUuid.TryGetValue(uuid, out qualityCondition))
			{
				return 0;
			}

			return _exceptionEvaluationStatistics.GetUsageCount(exceptionObject,
			                                                    qualityCondition);
		}
	}
}
