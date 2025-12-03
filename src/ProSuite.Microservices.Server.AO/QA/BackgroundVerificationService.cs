using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.Microservices.Server.AO.QA.Distributed;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class BackgroundVerificationService : QualityVerificationServiceBase
	{
		[NotNull] private readonly IDomainTransactionManager _domainTransactions;

		private IBackgroundVerificationInputs _backgroundVerificationInputs;

		private QualityErrorRepositoryBase _issueRepository;

		public BackgroundVerificationService(
			[NotNull] IBackgroundVerificationInputs backgroundVerificationInputs)
			: this(backgroundVerificationInputs.DomainTransactions,
			       backgroundVerificationInputs.DatasetLookup)
		{
			CustomErrorFilter = backgroundVerificationInputs.CustomErrorFilter;
		}

		public BackgroundVerificationService(
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IDatasetLookup datasetLookup) : base(datasetLookup)
		{
			_domainTransactions = domainTransactions;
		}

		protected override IList<ITest> GetTests(QualitySpecification specification,
		                                         out QualityVerification qualityVerification)
		{
			IList<ITest> result = null;
			QualityVerification verification = null;

			// TODO: Get rid of this overload and the unnecessary transaction
			// TODO: Copy from QualityVerificationService
			// Consider moving to base or provide the (properly initialized) tests together with the
			// verification in VerifyQuality() or move the condition/test dictionaries and collections
			// to a separate factory/provider class that can be called previously inside the appropriate
			// transaction
			_domainTransactions.UseTransaction(
				delegate
				{
					//ICollection<Dataset> datasets =
					//	QualitySpecificationUtils.InitializeAssociatedEntitiesTx(
					//		specification, _domainTransactions);

					//_backgroundVerificationInputs.InitializeSchema(datasets);

					result = GetTestsCore(specification, out verification);
				});

			qualityVerification = verification;
			return result;
		}

		protected override IOpenDataset CreateDatasetOpener(
			IVerificationContext verificationContext)
		{
			return _backgroundVerificationInputs.CreateDatasetOpener(verificationContext);
		}

		protected override void Compress(IEnumerable<string> compressibleDatasetPaths)
		{
			// No-op: FGDB Compression is not supported in the background.
		}

		protected override bool WriteIssueMapDocument(QualitySpecification qualitySpecification,
		                                              IIssueRepository issueRepository,
		                                              string issueStatisticsTableName,
		                                              IList<IObjectClass> verifiedObjectClasses,
		                                              XmlVerificationReport verificationReport,
		                                              string aoiFeatureClassName)
		{
			// No op: Writing mxds is not supported here.
			return false;
		}

		protected override ITestRunner CreateTestRunner(
			QualityVerification qualityVerification)
		{
			if (DistributedTestRunner == null)
			{
				return base.CreateTestRunner(qualityVerification);
			}

			return DistributedTestRunner;
		}

		protected override void ValidateTopologies(QualitySpecification qualitySpecification)
		{
			// Not supported in background verification (data is read-only)
			throw new NotImplementedException();
		}

		protected override IGdbTransaction CreateGdbTransaction()
		{
			return _backgroundVerificationInputs.CreateGdbTransaction();
		}

		protected override QualityErrorRepositoryBase CreateQualityErrorRepository(
			IVerificationContext verificationContext,
			IDictionary<QualityCondition, IList<ITest>> qualityConditionTests,
			IQualityConditionObjectDatasetResolver datasetResolver)
		{
			// TODO get rid of subclass; but allow injecting things like an AttributeWriter etc.
			// Or in the short term create a BackgroundServiceIssueRepo that is only used to read
			// (allowed) errors.
			return _issueRepository = _backgroundVerificationInputs.CreateQualityErrorRepository(
				       verificationContext, qualityConditionTests, datasetResolver);
		}

		[NotNull]
		public QualityVerification Verify(
			[NotNull] IBackgroundVerificationInputs backgroundVerificationInputs,
			[CanBeNull] ITrackCancel trackCancel)
		{
			QualitySpecification qualitySpecification = null;

			_backgroundVerificationInputs = backgroundVerificationInputs;

			_domainTransactions.UseTransaction(
				delegate
				{
					backgroundVerificationInputs.LoadInputsTx(
						_domainTransactions, trackCancel, OnProgress);

					qualitySpecification =
						PrepareQualitySpecificationTx(backgroundVerificationInputs);

					// Initialize the schema/model datasets before loading the selected objects
					ICollection<Dataset> datasets =
						QualitySpecificationUtils.InitializeAssociatedEntitiesTx(
							qualitySpecification, _domainTransactions);

					backgroundVerificationInputs.InitializeSchema(datasets);

					backgroundVerificationInputs.LoadObjectsToVerify(datasets);
				});

			IVerificationContext verificationContext =
				Assert.NotNull(backgroundVerificationInputs.VerificationContext);

			VerificationServiceParameters parameters =
				Assert.NotNull(backgroundVerificationInputs.VerificationParameters);

			QualityVerification verification = Verify(
				verificationContext, qualitySpecification, parameters,
				backgroundVerificationInputs.VerifiedObjects);

			if (parameters.SaveVerificationStatistics && ! verification.Cancelled)
			{
				backgroundVerificationInputs.SaveVerification(verification, _domainTransactions);
			}

			return verification;
		}

		private QualityVerification Verify([NotNull] IVerificationContext verificationContext,
		                                   [NotNull] QualitySpecification qualitySpecification,
		                                   [NotNull] VerificationServiceParameters parameters,
		                                   [CanBeNull] ICollection<IObject> objectsToVerify)
		{
			ISpatialReference spatialReference =
				verificationContext.SpatialReferenceDescriptor.GetSpatialReference();

			if (objectsToVerify != null)
			{
				IEnvelope selectionEnvelope = InitSelection(
					objectsToVerify, parameters.AreaOfInterest?.Extent, null);

				if (selectionEnvelope != null)
				{
					VerifiedPerimeter = selectionEnvelope;
					TestPerimeter = selectionEnvelope;
				}
			}

			if (TestPerimeter == null)
			{
				SetTestPerimeter(parameters.AreaOfInterest, spatialReference);
			}

			SetParameters(parameters);

			VerificationContext = verificationContext;

			AllowEditing = false;

			QualityVerification verification = VerifyEditableDatasets(qualitySpecification);

			verification.ContextType = parameters.VerificationContextType;
			verification.ContextName = parameters.VerificationContextName;

			return verification;
		}

		/// <summary>
		/// The perimeter that was actually verified if it differs from the perimeter
		/// provided (e.g. due to an object list which allowed shrinking the perimeter).
		/// </summary>
		private IEnvelope VerifiedPerimeter { get; set; }

		public IGeometry GetVerifiedPerimeter()
		{
			// Return the verified perimeter, if it is different from the desired test perimeter.
			return VerifiedPerimeter ?? TestPerimeter;
		}

		public DistributedTestRunner DistributedTestRunner { get; set; }

		private QualitySpecification PrepareQualitySpecificationTx(
			IBackgroundVerificationInputs backgroundVerificationInputs)
		{
			QualitySpecification qualitySpecification =
				backgroundVerificationInputs.QualitySpecification.Match(
					qs => qualitySpecification = qs,
					lbqs =>
					{
						qualitySpecification =
							lbqs.QualitySpecification;
						SetLocationBasedQualitySpecification(lbqs);
						return qualitySpecification;
					});

			if (DistributedTestRunner != null)
			{
				DistributedTestRunner.QualitySpecification = qualitySpecification;
			}

			// Not needed as long as VerifyEditableDatasets() is used:
			//if (backgroundVerificationInputs.VerifiedDatasets != null)
			//{
			//	DisableUninvolvedConditions(
			//		qualitySpecification, backgroundVerificationInputs.VerifiedDatasets);
			//}

			InitializeTestParameterValuesTx(qualitySpecification, _domainTransactions);

			return qualitySpecification;
		}

		public IEnumerable<AllowedError> GetInvalidatedAllowedErrors()
		{
			if (_issueRepository == null)
			{
				yield break;
			}

			if (OverrideAllowedErrors)
			{
				// Allowed errors must not be deleted if they have not been collected because of
				// this OverrideAllowedErrors.
				yield break;
			}

			foreach (AllowedError allowedError in _issueRepository.GetAllowedErrors(
				         ae => ae.Invalidated))
			{
				yield return allowedError;
			}
		}

		public IEnumerable<AllowedError> GetUnusedAllowedErrors()
		{
			if (_issueRepository == null)
			{
				yield break;
			}

			if (OverrideAllowedErrors)
			{
				// Allowed errors must not be deleted if they have not been collected because of
				// this OverrideAllowedErrors.
				yield break;
			}

			foreach (AllowedError allowedError in _issueRepository.GetAllowedErrors(
				         ae => ! ae.IsUsed))
			{
				yield return allowedError;
			}
		}
	}
}
