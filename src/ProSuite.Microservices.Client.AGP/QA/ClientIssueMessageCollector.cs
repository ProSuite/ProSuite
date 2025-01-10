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

		[CanBeNull] private readonly IIssueStore _issueStore;

		private int _verifiedSpecificationId = -1;
		private QualitySpecification _verifiedSpecification;

		public ClientIssueMessageCollector([CanBeNull] IIssueStore issueStore = null)
		{
			_issueStore = issueStore;
		}

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

			await PrepareIssueStore(_issueStore);

			int savedIssueCount = 0;

			// Preparations that require a queued task:
			List<GdbObjectReference> objectsToVerify = null;
			List<Dataset> referencedIssueTables = null;

			bool success = false;

			await QueuedTask.Run(async () =>
			{
				objectsToVerify = VerifiedRows?.Select(row => new GdbObjectReference(
					                                       row.GetTable().GetID(),
					                                       row.GetObjectID()))
				                              .ToList();

				referencedIssueTables = _issueStore
				                        .GetReferencedIssueTables(_issueMessages)
				                        .ToList();

				Assert.NotNull(referencedIssueTables, "Error getting issue FeatureClasses");

				EditorTransaction transaction = new EditorTransaction(new EditOperation());

				success = await transaction.ExecuteAsync(
					          editContext =>
					          {
						          savedIssueCount =
							          UpdateIssuesTx(editContext, objectsToVerify,
							                         verifiedConditionIds);

								  // Deleting issues can be pretty undiscriminating, we don't even
								  // know if there were deletes or not:
						          foreach (Dataset issueTable in referencedIssueTables)
						          {
							          editContext.Invalidate(issueTable);
						          }
					          },
					          "Update issues", referencedIssueTables);
			});

			return success ? savedIssueCount : 0;
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void SetVerifiedPerimeter(ShapeMsg perimeterMsg)
		{
			if (perimeterMsg != null)
			{
				VerifiedPerimeter = ProtobufConversionUtils.FromShapeMsg(perimeterMsg);
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

		private async Task PrepareIssueStore([NotNull] IIssueStore issueStore)
		{
			if (_verifiedSpecification != null)
			{
				issueStore.SetVerifiedSpecification(_verifiedSpecification);
			}
			else
			{
				Assert.False(_verifiedSpecificationId < 0,
				             "The verified specification/specification id was not set.");

				issueStore.SetVerifiedSpecification(_verifiedSpecificationId);
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
			// TODO: Invalidate deleted / inserted features / issue tables
			//editContext.Invalidate();

			DeleteErrors(verifiedObjects, verifiedConditionIds);

			_msg.Debug("Saving new issues in verification perimeter...");
			int saveCount = Assert.NotNull(_issueStore)
			                      .SaveIssues(_issueMessages, verifiedConditionIds);

			DeleteInvalidAllowedErrors(_obsoleteExceptionGdbRefs);

			_msg.Debug("Deleted invalid allowed errors.");

			return saveCount;
		}

		private void DeleteInvalidAllowedErrors(
			IReadOnlyCollection<GdbObjRefMsg> obsoleteExceptions)
		{
			if (obsoleteExceptions.Count == 0)
			{
				return;
			}

			Assert.NotNull(_issueStore, "No issue store set up");

			IList<GdbObjectReference> invalidAllowedErrorReferences =
				obsoleteExceptions.Select(
					m => new GdbObjectReference(m.ClassHandle, m.ObjectId)).ToList();

			_issueStore.DeleteInvalidAllowedErrors(invalidAllowedErrorReferences);
		}

		private void DeleteErrors([CanBeNull] IList<GdbObjectReference> objectSelection,
		                          IEnumerable<int> verifiedConditionIds)
		{
			_msg.Debug("Deleting existing issues in verification perimeter...");

			var deleteForConditions =
				ErrorDeletionInPerimeter == ErrorDeletionInPerimeter.AllQualityConditions
					? null
					: verifiedConditionIds;

			Assert.NotNull(_issueStore).DeleteErrors(
				deleteForConditions, VerifiedPerimeter, objectSelection);
		}
	}
}
