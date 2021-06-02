using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Workflow.WorkspaceFilters;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public static class ProjectUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull]
		public static IEnumerable<WorkspaceAssociation> GetWorkspaceAssociations<P, M>(
			[NotNull] IWorkspace workspace,
			[NotNull] P project)
			where P : Project<M>
			where M : ProductionModel
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(project, nameof(project));

			Stopwatch watch = _msg.DebugStartTiming();

			int associationCount = project.ProductionModel.GetAssociations().Count;
			if (associationCount == 0)
			{
				// there are no associations in the model, no need to enumerate relationship classes
				yield break;
			}

			bool? isModelMasterDatabase = null;

			var count = 0;
			foreach (IDatasetName datasetName in DatasetUtils.GetDatasetNames(
				workspace, esriDatasetType.esriDTRelationshipClass))
			{
				if (isModelMasterDatabase == null)
				{
					isModelMasterDatabase = IsModelMasterDatabase(
						workspace, project.ProductionModel);
				}

				Association association = GetAssociation<P, M>(
					project, datasetName, isModelMasterDatabase.Value);

				if (association == null)
				{
					continue;
				}

				count++;
				yield return CreateWorkspaceAssociation(datasetName, association);
			}

			_msg.DebugStopTiming(
				watch,
				"Read relationship classes for project {0} in workspace {1} ({2} of {3} model associations found)",
				project.Name,
				WorkspaceUtils.GetConnectionString(workspace, true),
				count, associationCount);
		}

		[NotNull]
		public static IWorkspaceFilter CreateChildDatabaseWorkspaceFilter(
			[CanBeNull] string childDatabaseRestrictions)
		{
			var parser = new NamedValuesParser('=',
			                                   new[] {";", Environment.NewLine},
			                                   new[] {","},
			                                   " AND ");

			NotificationCollection notifications;
			IList<NamedValuesExpression> restrictionExpressions;
			if (! parser.TryParse(childDatabaseRestrictions,
			                      out restrictionExpressions,
			                      out notifications))
			{
				throw new RuleViolationException(notifications,
				                                 "Error reading child database restrictions");
			}

			IWorkspaceFilter filter =
				WorkspaceFilterFactory.TryCreate(restrictionExpressions,
				                                 out notifications);
			if (filter == null)
			{
				throw new RuleViolationException(notifications,
				                                 "Error creating child database workspace filter");
			}

			return filter;
		}

		[NotNull]
		public static DatasetNameTransformer CreateDatasetNameTransformer(
			[CanBeNull] string transformationPatterns)
		{
			return new DatasetNameTransformer(transformationPatterns);
		}

		/// <summary>
		/// Indicates if a qualified gdb element is from another database schema than the one harvested
		/// for a model with unqualified element names (i.e. a mismatch would happen if simply
		/// un-qualifying the gdb element name)
		/// </summary>
		/// <param name="model"></param>
		/// <param name="gdbElementName"></param>
		/// <returns></returns>
		private static bool IsFromOtherMasterDatabaseSchema(
			[NotNull] Model model,
			[NotNull] string gdbElementName)
		{
			if (model.ElementNamesAreQualified ||
			    ! ModelElementNameUtils.IsQualifiedName(gdbElementName))
			{
				return false;
			}

			string owner = ModelElementNameUtils.GetOwnerName(gdbElementName).Trim();

			// the workspace is from the model master database
			string masterDatabaseSchemaOwner =
				(model.DefaultDatabaseSchemaOwner ?? string.Empty).Trim();

			// true if dataset is from a different schema:
			if (! string.IsNullOrEmpty(masterDatabaseSchemaOwner) &&
			    ! string.Equals(masterDatabaseSchemaOwner, owner,
			                    StringComparison.OrdinalIgnoreCase))
			{
				_msg.VerboseDebugFormat(
					"Dataset {0} is from master database of model {1}, but from a different schema: {2} (<> {3})",
					gdbElementName, model.Name, owner, masterDatabaseSchemaOwner);

				return true;
			}

			return false;
		}

		[CanBeNull]
		private static Association GetAssociation<P, M>(
			[NotNull] P project,
			[NotNull] IDatasetName datasetName,
			bool isModelMasterDatabase) where P : Project<M>
			                            where M : ProductionModel
		{
			// called when activating a work context

			Model model = project.ProductionModel;
			if (model == null)
			{
				return null;
			}

			string modelName = GetModelElementName(
				datasetName.Name,
				project.ChildDatabaseDatasetNameTransformer,
				model, isModelMasterDatabase);

			_msg.VerboseDebugFormat(
				"Association name for {0} in model {1} (from master db: {2}): {3}",
				datasetName.Name, model.Name, isModelMasterDatabase,
				modelName ?? "<null>");

			if (modelName == null)
			{
				return null;
			}

			Association association = model.GetAssociationByModelName(modelName);

			// TODO check cardinality, related object datasets etc.

			return association;
		}

		[CanBeNull]
		private static string GetModelElementNameForChildDatabaseElement(
			[NotNull] string gdbElementName,
			[NotNull] Model model,
			[NotNull] IDatasetNameTransformer datasetNameTransformer)
		{
			if (! model.ElementNamesAreQualified)
			{
				return datasetNameTransformer.TransformName(
					ModelElementNameUtils.GetUnqualifiedName(gdbElementName));
			}

			// the model uses qualified element names

			if (DoNotMatchChildDatabaseForQualifiedModelElements())
			{
				_msg.VerboseDebugFormat(
					"Matching child database datasets for qualified model elements is disabled");

				// restore previous behavior: don't try to match child database elements for
				// model that was harvested with *qualified* element names
				return null;
			}

			if (ModelElementNameUtils.IsQualifiedName(gdbElementName))
			{
				_msg.VerboseDebugFormat(
					"Gdb element name is qualified, and model uses qualified names");

				// gdb element name is also qualified, but from child database
				// rely on transformer for changing schema owner/database name, if required.
				return datasetNameTransformer.TransformName(gdbElementName);
			}

			// gdb element name is unqualified, but model element names are qualified

			if (string.IsNullOrEmpty(model.DefaultDatabaseSchemaOwner))
			{
				_msg.VerboseDebugFormat(
					"Gdb element name is unqualified, but qualified model has no unique schema owner " +
					"to allow name qualification");

				// default database schema owner is not known, cannot qualify name
				return null; // give up
			}

			_msg.VerboseDebugFormat(
				"Using unique master database schema information to qualify dataset name");

			string transformedName = datasetNameTransformer.TransformName(gdbElementName);

			string owner = model.DefaultDatabaseSchemaOwner.Trim();
			return string.IsNullOrEmpty(model.DefaultDatabaseName)
				       ? $"{owner}.{transformedName}"
				       : $"{model.DefaultDatabaseName.Trim()}.{owner}.{transformedName}";
		}

		private static bool DoNotMatchChildDatabaseForQualifiedModelElements()
		{
			return EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				"PROSUITE_DONT_MATCH_CHILD_DATABASE_FOR_QUALIFIED_MODEL_ELEMENT_NAMES");
		}

		[CanBeNull]
		private static string GetModelElementName(
			[NotNull] string gdbElementName,
			[NotNull] IDatasetNameTransformer datasetNameTransformer,
			[NotNull] Model model,
			bool isModelMasterDatabase)
		{
			if (! isModelMasterDatabase)
			{
				return GetModelElementNameForChildDatabaseElement(
					gdbElementName, model, datasetNameTransformer);
			}

			// element is from master database

			if (IsFromOtherMasterDatabaseSchema(model, gdbElementName))
			{
				// Exclude datasetNames that are from ANOTHER schema/(database) than the model schema
				// error scenario:
				// - harvest from one schema --> unique names
				// - get workspace datasets from workspace with access to multiple schemas
				// - unqualified datasets in these schemas are not unique
				//   --> multiple matches by unqualified name to datasets
				//   --> multiple datasets returned
				// two cases
				// - the workspace corresponds to the model master database
				//   -> filter datasetNames by DefaultDatabaseSchemaOwner
				// - a "checkout" database with qualified names
				//   -> ???
				return null;
			}

			return model.ElementNamesAreQualified
				       ? gdbElementName // no need to check, element name must also be qualified
				       : ModelElementNameUtils
					       .GetUnqualifiedName(gdbElementName); // don't apply transformer
		}

		private static bool IsModelMasterDatabase([NotNull] IWorkspace workspace,
		                                          [NotNull] ProductionModel model)
		{
			IWorkspaceContext masterDatabaseWorkspaceContext =
				model.MasterDatabaseWorkspaceContext;

			return masterDatabaseWorkspaceContext != null &&
			       WorkspaceUtils.IsSameDatabase(
				       workspace,
				       masterDatabaseWorkspaceContext.Workspace);
		}

		[NotNull]
		private static WorkspaceAssociation CreateWorkspaceAssociation(
			[NotNull] IDatasetName datasetName,
			[NotNull] Association association)
		{
			IDatasetName featureDatasetName =
				DatasetUtils.GetFeatureDatasetName(datasetName);

			return new WorkspaceAssociation(datasetName.Name,
			                                featureDatasetName?.Name,
			                                association);
		}
	}
}
