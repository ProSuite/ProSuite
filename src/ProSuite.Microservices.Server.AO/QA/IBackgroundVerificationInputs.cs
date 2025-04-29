using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA
{
	/// <summary>
	/// Provides the input parameters for a specific quality verification run in the server.
	/// Implementors get a chance to load the required inputs in a DDX transaction before any
	/// of the relevant properties are called.
	/// </summary>
	public interface IBackgroundVerificationInputs
	{
		IVerificationContext VerificationContext { get; }

		IDomainTransactionManager DomainTransactions { get; }

		IDatasetLookup DatasetLookup { get; }

		ICustomErrorFilter CustomErrorFilter { get; }

		Either<QualitySpecification, ILocationBasedQualitySpecification> QualitySpecification
		{
			get;
		}

		VerificationServiceParameters VerificationParameters { get; }

		///// <summary>
		///// The datasets that should be verified. Conditions that do not reference any
		///// dataset in this list will be disabled. Null means all conditions are verified.
		///// </summary>
		//[CanBeNull]
		//ICollection<Dataset> VerifiedDatasets { get; }

		/// <summary>
		/// The objects that should be verified. Null means all objects in the perimeter
		/// should be verified.
		/// </summary>
		ICollection<IObject> VerifiedObjects { get; }

		/// <summary>
		/// The supported instance descriptors to be used for verifications.
		/// </summary>
		ISupportedInstanceDescriptors SupportedInstanceDescriptors { set; }

		/// <summary>
		/// Allows loading all required entities inside a transaction.
		/// </summary>
		/// <param name="domainTransaction">The transaction manager that started the transaction</param>
		/// <param name="trackCancel">The cancel tracker to stop lengthy operations if desired by
		/// the user.</param>
		/// <param name="onProgressAction">The progress advancing the progress shown to the user.</param>
		void LoadInputsTx([NotNull] IDomainTransactionManager domainTransaction,
		                  [CanBeNull] ITrackCancel trackCancel,
		                  [CanBeNull] Action<VerificationProgressEventArgs> onProgressAction);

		/// <summary>
		/// Implementers should provide a non-null error repository. However, currently
		/// it will not be used to store errors in the verified model context.
		/// </summary>
		/// <param name="verificationContext"></param>
		/// <param name="qualityConditionTests"></param>
		/// <param name="datasetResolver"></param>
		/// <returns></returns>
		QualityErrorRepositoryBase CreateQualityErrorRepository(
			[NotNull] IVerificationContext verificationContext,
			[NotNull] IDictionary<QualityCondition, IList<ITest>> qualityConditionTests,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver);

		/// <summary>
		/// Allows for saving the quality verification after a successful run. The
		/// transaction must be started by implementers.
		/// </summary>
		/// <param name="qualityVerification"></param>
		/// <param name="domainTransaction"></param>
		void SaveVerification([NotNull] QualityVerification qualityVerification,
		                      [NotNull] IDomainTransactionManager domainTransaction);

		/// <summary>
		/// Allows setting the Gdb schema for the virtual model context, if desired.
		/// Only relevant if the schema is provided by the client.
		/// </summary>
		/// <param name="gdbWorkspaces"></param>
		void SetGdbSchema(IList<GdbWorkspace> gdbWorkspaces);

		/// <summary>
		/// Allows for schema initialization for the virtual model context, if desired.
		/// Only relevant if the schema is provided by the client.
		/// </summary>
		/// <param name="datasets"></param>
		void InitializeSchema(ICollection<Dataset> datasets);

		/// <summary>
		/// Initialize the requested objects to be verified and set the <see cref="VerifiedObjects"/>.
		/// </summary>
		/// <param name="datasets"></param>
		void LoadObjectsToVerify(ICollection<Dataset> datasets);

		/// <summary>
		/// Set the function to get schema and data from the client.
		/// </summary>
		/// <param name="dataRequestFunc"></param>
		void SetRemoteDataAccess(
			Func<DataVerificationResponse, DataVerificationRequest> dataRequestFunc);

		/// <summary>
		/// Creates the dataset opener that supports the opening functionality of the
		/// datasets that are supported in the current context.
		/// </summary>
		/// <param name="verificationContext"></param>
		/// <returns></returns>
		IOpenDataset CreateDatasetOpener(IVerificationContext verificationContext);

		/// <summary>
		/// Creates the GDB transaction if errors are written to the verification context.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IGdbTransaction CreateGdbTransaction();
	}
}
