using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public class ClientIssueMessageCollector : IClientIssueMessageCollector
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly List<IssueMsg> _issueMessages = new();
		private readonly List<GdbObjRefMsg> _obsoleteExceptionGdbRefs = new();

		private ShapeMsg _verifiedPerimeterMsg;

		[CanBeNull] private readonly IIssueStore _issueStore;

		private int _verifiedSpecificationId = -1;
		private QualitySpecification _verifiedSpecification;

		public ClientIssueMessageCollector([CanBeNull] IIssueStore issueStore = null)
		{
			_issueStore = issueStore;
		}

		[CanBeNull]
		private Geometry VerifiedPerimeter { get; set; }

		private IList<Row> VerifiedRows { get; set; }

		public void SetVerifiedSpecificationId(int ddxId)
		{
			_verifiedSpecificationId = ddxId;
		}

		public void SetVerifiedSpecification(QualitySpecification qualitySpecification)
		{
			_verifiedSpecification = qualitySpecification;
		}

		public void SetVerifiedObjects(IList<Row> objectsToVerify)
		{
			VerifiedRows = objectsToVerify;
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public ErrorDeletionInPerimeter ErrorDeletionInPerimeter { get; set; }

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public bool HasIssues => _issueMessages.Count > 0;

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public int SaveIssues(IEnumerable<int> verifiedConditionIds)
		{
			throw new NotImplementedException("Call async overload on this platform.");
		}

		public async Task<int> SaveIssuesAsync(IList<int> verifiedConditionIds)
		{
			if (_issueStore == null)
			{
				throw new NotSupportedException("Unsupported operation: No issue store set up");
			}

			await PrepareIssueStore(_issueStore, verifiedConditionIds);

			int savedIssueCount = 0;

			// Preparations that require a queued task:
			List<GdbObjectReference> objectsToVerify = null;
			List<Dataset> referencedIssueTables = null;

			await QueuedTask.Run(() =>
			{
				if (_verifiedPerimeterMsg != null)
				{
					VerifiedPerimeter = ProtobufConversionUtils.FromShapeMsg(_verifiedPerimeterMsg);
				}

				objectsToVerify = VerifiedRows?.Select(row => new GdbObjectReference(
					                                       row.GetTable().GetID(),
					                                       row.GetObjectID()))
				                              .ToList();

				referencedIssueTables = _issueStore
				                        .GetReferencedIssueTables(_issueMessages)
				                        .ToList();

				Assert.NotNull(referencedIssueTables, "Error getting issue FeatureClasses");

				return Task.CompletedTask;
			});

			// NOTE: Do not call transaction inside QueuedTask.Run or the EditingCompleted event
			// will fire twice!
			EditorTransaction transaction = new EditorTransaction(new EditOperation());

			bool success = await transaction.ExecuteAsync(
				               editContext =>
				               {
					               savedIssueCount =
						               UpdateIssuesTx(editContext, objectsToVerify,
						                              verifiedConditionIds);
				               },
				               "Update issues", referencedIssueTables);

			return success ? savedIssueCount : 0;
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void SetVerifiedPerimeter(ShapeMsg perimeterMsg)
		{
			if (perimeterMsg != null)
			{
				// We're potentially on an MTA thread, no COM!
				_verifiedPerimeterMsg = perimeterMsg;
			}
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void AddIssueMessage(IssueMsg issueMsg)
		{
			_issueMessages.Add(issueMsg);
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void AddObsoleteException(GdbObjRefMsg gdbObjRefMsg)
		{
			_obsoleteExceptionGdbRefs.Add(gdbObjRefMsg);
		}

		private async Task PrepareIssueStore([NotNull] IIssueStore issueStore,
		                                     [CanBeNull] IList<int> verifiedConditionIds)
		{
			if (_verifiedSpecification != null)
			{
				issueStore.SetVerifiedSpecification(_verifiedSpecification);
			}
			else if (_verifiedSpecificationId >= 0)
			{
				issueStore.SetVerifiedSpecification(_verifiedSpecificationId);
			}
			else
			{
				// No specification (id) is known on the client, e.g. because the verified
				// specification was created on the server (Release Quality). Fall back to
				// the verified condition ids from the verification message:
				Assert.True(verifiedConditionIds?.Count > 0,
				            "The verified specification/specification id was not set and " +
				            "no verified condition ids are available.");

				_msg.DebugFormat(
					"No verified specification (id) was set. Using the {0} verified " +
					"condition ids from the verification message instead.",
					verifiedConditionIds.Count);

				issueStore.SetVerifiedConditionIds(verifiedConditionIds);
			}

			bool allConditionsRequired =
				ErrorDeletionInPerimeter == ErrorDeletionInPerimeter.AllQualityConditions &&
				VerifiedRows != null;

			await issueStore.PrepareVerifiedConditions(allConditionsRequired);
		}

		private int UpdateIssuesTx(
			[NotNull] EditOperation.IEditContext editContext,
			[CanBeNull] IList<GdbObjectReference> verifiedObjects,
			IList<int> verifiedConditionIds)
		{
			Action<Row> invalidateRow = row => editContext.Invalidate(row);

			DeleteErrors(verifiedObjects, verifiedConditionIds, invalidateRow);

			_msg.Debug("Saving new issues in verification perimeter...");
			int saveCount = Assert.NotNull(_issueStore)
			                      .SaveIssues(_issueMessages, verifiedConditionIds,
			                                  invalidateRow);

			DeleteInvalidAllowedErrors(_obsoleteExceptionGdbRefs, invalidateRow);

			_msg.Debug("Deleted invalid allowed errors.");

			return saveCount;
		}

		private void DeleteInvalidAllowedErrors(
			IReadOnlyCollection<GdbObjRefMsg> obsoleteExceptions,
			[CanBeNull] Action<Row> invalidateRow)
		{
			if (obsoleteExceptions.Count == 0)
			{
				return;
			}

			Assert.NotNull(_issueStore, "No issue store set up");

			IList<GdbObjectReference> invalidAllowedErrorReferences =
				obsoleteExceptions.Select(m => new GdbObjectReference(m.ClassHandle, m.ObjectId))
				                  .ToList();

			_issueStore.DeleteInvalidAllowedErrors(invalidAllowedErrorReferences,
			                                       invalidateRow);
		}

		private void DeleteErrors([CanBeNull] IList<GdbObjectReference> objectSelection,
		                          IList<int> verifiedConditionIds,
		                          [CanBeNull] Action<Row> invalidateRow)
		{
			_msg.Debug("Deleting existing issues in verification perimeter...");

			var deleteForConditions =
				ErrorDeletionInPerimeter == ErrorDeletionInPerimeter.AllQualityConditions
					? null
					: verifiedConditionIds;

			Assert.NotNull(_issueStore).DeleteErrors(
				deleteForConditions, VerifiedPerimeter, objectSelection, invalidateRow);
		}
	}
}
