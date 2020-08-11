using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualityVerification : Entity
	{
		[UsedImplicitly] private readonly string _specificationName;
		[UsedImplicitly] private readonly int _specificationId;
		[UsedImplicitly] private readonly string _specificationDescription;

		[UsedImplicitly] private string _operator;
		[UsedImplicitly] private DateTime _startDate;
		[UsedImplicitly] private DateTime _endDate;
		[UsedImplicitly] private int _errorsFound;
		[UsedImplicitly] private bool _fulfilled;
		[UsedImplicitly] private double _processorTimeSeconds;
		[UsedImplicitly] private bool _cancelled;

		[UsedImplicitly] private string _contextType;
		[UsedImplicitly] private string _contextName;

		private readonly IList<QualityConditionVerification> _conditionVerifications =
			new List<QualityConditionVerification>();

		private readonly IList<QualityVerificationDataset> _verificationDatasets =
			new List<QualityVerificationDataset>();

		[UsedImplicitly] private int _rowsWithStopConditions;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityVerification"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected QualityVerification() { }

		public QualityVerification(
			int qualitySpecificationId,
			string qualitySpecificationName,
			string qualitySpecificationDescription,
			string userDisplayName,
			[NotNull] IEnumerable<QualityConditionVerification> conditionVerifications)
		{
			_specificationId = qualitySpecificationId;
			_specificationName = qualitySpecificationName;
			_specificationDescription = qualitySpecificationDescription;

			_operator = userDisplayName;

			var datasets = new HashSet<Dataset>();

			foreach (QualityConditionVerification conditionVerification in conditionVerifications)
			{
				_conditionVerifications.Add(conditionVerification);

				QualityCondition condition = conditionVerification.QualityCondition;

				Assert.NotNull(condition);

				foreach (Dataset dataset in condition.GetDatasetParameterValues())
				{
					datasets.Add(dataset);
				}
			}

			foreach (Dataset dataset in datasets)
			{
				_verificationDatasets.Add(new QualityVerificationDataset(dataset));
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityVerification"/> class.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification that was verified.</param>
		public QualityVerification([NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			_operator = EnvironmentUtils.UserDisplayName;
			_specificationName = qualitySpecification.Name;
			_specificationId = ! qualitySpecification.IsUnion
				                   ? qualitySpecification.Id
				                   : -1;

			_specificationDescription = qualitySpecification.Description;

			var datasets = new HashSet<Dataset>();

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				_conditionVerifications.Add(new QualityConditionVerification(element));

				foreach (Dataset dataset in element.QualityCondition.GetDatasetParameterValues())
				{
					datasets.Add(dataset);
				}
			}

			foreach (Dataset dataset in datasets)
			{
				_verificationDatasets.Add(new QualityVerificationDataset(dataset));
			}
		}

		public string ContextType
		{
			get { return _contextType; }
			set { _contextType = value; }
		}

		public string ContextName
		{
			get { return _contextName; }
			set { _contextName = value; }
		}

		public string SpecificationName => _specificationName;

		public int SpecificationId => _specificationId;

		public string SpecificationDescription => _specificationDescription;

		public string Operator
		{
			get { return _operator; }
			set { _operator = value; }
		}

		public DateTime StartDate
		{
			get { return _startDate; }
			set { _startDate = value; }
		}

		public DateTime EndDate
		{
			get { return _endDate; }
			set { _endDate = value; }
		}

		public double ProcessorTimeSeconds
		{
			get { return _processorTimeSeconds; }
			set { _processorTimeSeconds = value; }
		}

		public int IssueCount
		{
			get { return _errorsFound; }
			set { _errorsFound = value; }
		}

		public int ErrorCount => IssueCount - WarningCount;

		public bool Fulfilled => _fulfilled;

		public bool Cancelled
		{
			get { return _cancelled; }
			set
			{
				_cancelled = value;
				if (_cancelled)
				{
					_fulfilled = false;
				}
			}
		}

		[NotNull]
		public IList<QualityConditionVerification> ConditionVerifications
			=> new ReadOnlyList<QualityConditionVerification>(_conditionVerifications);

		[CanBeNull]
		public QualityConditionVerification GetConditionVerification(
			[NotNull] QualityCondition qualityCondition)
		{
			return _conditionVerifications.FirstOrDefault(
				verification => verification.QualityCondition == qualityCondition);
		}

		[NotNull]
		public IList<QualityVerificationDataset> VerificationDatasets
			=> new ReadOnlyList<QualityVerificationDataset>(_verificationDatasets);

		/// <summary>
		/// Gets or sets the number of rows for which a stop condition reported an error.
		/// </summary>
		/// <value>
		/// The number of rows with stop conditions.
		/// </value>
		public int RowsWithStopConditions
		{
			get { return _rowsWithStopConditions; }
			set { _rowsWithStopConditions = value; }
		}

		[CanBeNull]
		public QualityVerificationDataset GetVerificationDataset([NotNull] Dataset dataset)
		{
			return _verificationDatasets.FirstOrDefault(
				verificationDataset => Equals(verificationDataset.Dataset, dataset));
		}

		public void CalculateFulfilled()
		{
			if (_cancelled)
			{
				_fulfilled = false;
				return;
			}

			foreach (QualityConditionVerification verification in _conditionVerifications)
			{
				if (verification.Fulfilled)
				{
					continue;
				}

				_fulfilled = false;
				return;
			}

			_fulfilled = true;
		}

		public int WarningCount
		{
			get
			{
				return _conditionVerifications.Where(
					verification => verification.AllowErrors).Sum(
					verification => verification.ErrorCount);
			}
		}

		public void RemoveVerificationDatasets([NotNull] IEnumerable<Dataset> datasets)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			var datasetsToRemove = new HashSet<Dataset>();
			foreach (Dataset dataset in datasets)
			{
				datasetsToRemove.Add(dataset);
			}

			var verificationDatasetsToRemove = new List<QualityVerificationDataset>();

			foreach (QualityVerificationDataset verificationDataset in _verificationDatasets)
			{
				if (datasetsToRemove.Contains(verificationDataset.Dataset))
				{
					verificationDatasetsToRemove.Add(verificationDataset);
				}
			}

			foreach (
				QualityVerificationDataset verificationDataset in verificationDatasetsToRemove)
			{
				bool removed = _verificationDatasets.Remove(verificationDataset);

				Assert.True(removed, "Unable to remove verification dataset {0}",
				            verificationDataset.Dataset);
			}
		}

		public void UnlinkQualityCondition([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			foreach (
				QualityConditionVerification conditionVerification in _conditionVerifications)
			{
				if (conditionVerification.QualityCondition != null)
				{
					if (Equals(conditionVerification.QualityCondition, qualityCondition))
					{
						conditionVerification.ClearQualityCondition();
					}
				}

				if (conditionVerification.StopCondition != null)
				{
					if (Equals(conditionVerification.StopCondition, qualityCondition))
					{
						conditionVerification.StopCondition = null;
					}
				}
			}
		}
	}
}