using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.AO.DataModel
{
	// TODO test on sql server and postgres (dbname!)
	// - sql server: OK
	// TODO apply dataset prefix from model (otherwise the datasets will be filtered out)
	public class IssueDatasetSchemaManager : IIssueDatasetSchemaManager
	{
		private readonly ErrorDatasetSchema _errorDatasetSchema;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueDatasetSchemaManager"/> class.
		/// </summary>
		/// <param name="errorDatasetSchema">The error dataset schema.</param>
		public IssueDatasetSchemaManager([NotNull] ErrorDatasetSchema errorDatasetSchema)
		{
			Assert.ArgumentNotNull(errorDatasetSchema, nameof(errorDatasetSchema));

			_errorDatasetSchema = errorDatasetSchema;
		}

		#endregion

		public ICollection<string> GetMissingErrorDatasets(
			IWorkspace schemaOwnerWorkspace,
			out IList<ITable> existingTables)
		{
			Assert.ArgumentNotNull(schemaOwnerWorkspace, nameof(schemaOwnerWorkspace));

			var result = new List<string>();
			existingTables = new List<ITable>();

			foreach (IssueDatasetName datasetName in _errorDatasetSchema.IssueDatasetNames)
			{
				string qualifiedName;
				ITable existingTable;
				if (! ExistsDataset(schemaOwnerWorkspace, datasetName.Name,
				                    out qualifiedName, out existingTable))
				{
					result.Add(qualifiedName);
				}
				else
				{
					existingTables.Add(existingTable);
				}
			}

			return result;
		}

		public ICollection<ITable> CreateMissingErrorDatasets(
			IWorkspace schemaOwnerWorkspace,
			ISpatialReference spatialReference,
			string featureDatasetName,
			string configKeyword,
			double gridSize1,
			double gridSize2,
			double gridSize3,
			IEnumerable<string> readers,
			IEnumerable<string> writers)
		{
			Assert.ArgumentNotNull(schemaOwnerWorkspace, nameof(schemaOwnerWorkspace));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));
			Assert.ArgumentNotNull(readers, nameof(readers));
			Assert.ArgumentNotNull(writers, nameof(writers));

			var result = new List<ITable>();
			var featureWorkspace = (IFeatureWorkspace) schemaOwnerWorkspace;

			IFeatureDataset featureDataset = GetFeatureDataset(featureWorkspace,
			                                                   featureDatasetName,
			                                                   spatialReference);

			ICollection<string> readerCollection = CollectionUtils.GetCollection(readers);
			ICollection<string> writerCollection = CollectionUtils.GetCollection(writers);

			foreach (IssueDatasetName datasetName in _errorDatasetSchema.IssueDatasetNames)
			{
				ITable table;
				string qualifiedName;
				if (ExistsDataset(schemaOwnerWorkspace, datasetName.Name, out qualifiedName))
				{
					if (readerCollection.Count > 0 || writerCollection.Count > 0)
					{
						_msg.InfoFormat("Dataset {0} already exists, granting privileges...",
						                datasetName.Name);

						table = GetTable(schemaOwnerWorkspace, qualifiedName);
						Assert.NotNull(table, $"Could not open existing table {datasetName.Name}");
						GrantPrivileges(table, readerCollection, writerCollection);

						// TODO: Also register as versioned? -> Checkbox on UI?
					}

					continue;
				}

				if (datasetName.GeometryType == null)
				{
					table = CreateTable(qualifiedName, featureWorkspace, configKeyword);
				}
				else
				{
					table = CreateFeatureClass(featureWorkspace, qualifiedName,
					                           datasetName.GeometryType.Value,
					                           spatialReference,
					                           featureDataset, configKeyword,
					                           gridSize1, gridSize2, gridSize3);
				}

				RegisterAsVersioned(table);
				GrantPrivileges(table, readerCollection, writerCollection);
				result.Add(table);
			}

			return result;
		}

		[CanBeNull]
		private static IFeatureDataset GetFeatureDataset(
			[NotNull] IFeatureWorkspace workspace,
			[CanBeNull] string featureDatasetName,
			[NotNull] ISpatialReference spatialReference)
		{
			if (string.IsNullOrEmpty(featureDatasetName))
			{
				return null;
			}

			if (featureDatasetName.Trim().Length == 0)
			{
				return null;
			}

			string qualifiedName = GetQualifiedName((IWorkspace) workspace, featureDatasetName);

			IFeatureDataset featureDataset = GetFeatureDataset(workspace, qualifiedName);

			if (featureDataset == null)
			{
				_msg.InfoFormat("Creating feature dataset '{0}'...", qualifiedName);

				return DatasetUtils.CreateFeatureDataset(workspace, qualifiedName,
				                                         spatialReference);
			}

			// the feature dataset already exists - check if it is compatible
			const bool comparePrecisionAndTolerance = true;
			const bool compareVerticalCS = false;
			if (! SpatialReferenceUtils.AreEqual(spatialReference,
			                                     ((IGeoDataset) featureDataset).SpatialReference,
			                                     comparePrecisionAndTolerance,
			                                     compareVerticalCS))
			{
				throw new InvalidOperationException(
					string.Format(
						"The existing feature dataset '{0}' has a different spatial reference than the one specified for the model",
						featureDataset.Name));
			}

			return featureDataset;
		}

		[NotNull]
		private ITable CreateTable([NotNull] string qualifiedName,
		                           [NotNull] IFeatureWorkspace featureWorkspace,
		                           [CanBeNull] string configKeyword)
		{
			_msg.InfoFormat("Creating table '{0}'...", qualifiedName);

			ITable table = DatasetUtils.CreateTable(featureWorkspace, qualifiedName,
			                                        configKeyword, CreateTableFields());

			SetAliasName((IObjectClass) table);

			return table;
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass(
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] string qualifiedName,
			esriGeometryType geometryType,
			[NotNull] ISpatialReference spatialReference,
			[CanBeNull] string configKeyword,
			double gridSize1,
			double gridSize2,
			double gridSize3)
		{
			_msg.InfoFormat("Creating feature class '{0}'...", qualifiedName);

			IFields fields = CreateFeatureClassFields(geometryType, spatialReference,
			                                          gridSize1, gridSize2, gridSize3);

			return DatasetUtils.CreateSimpleFeatureClass(
				featureWorkspace, qualifiedName, fields, configKeyword);
		}

		private static void SetAliasName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			string aliasName = GetDefaultAliasName(objectClass);

			if (! Equals(objectClass.AliasName, aliasName))
			{
				((IClassSchemaEdit) objectClass).AlterAliasName(aliasName);
			}
		}

		[NotNull]
		private static string GetDefaultAliasName([NotNull] IObjectClass objectClass)
		{
			return ModelElementNameUtils.GetUnqualifiedName(DatasetUtils.GetName(objectClass));
		}

		[NotNull]
		private ITable CreateFeatureClass([NotNull] IFeatureWorkspace featureWorkspace,
		                                  [NotNull] string qualifiedName,
		                                  esriGeometryType geometryType,
		                                  [NotNull] ISpatialReference spatialReference,
		                                  [CanBeNull] IFeatureDataset featureDataset,
		                                  [CanBeNull] string configKeyword,
		                                  double gridSize1,
		                                  double gridSize2,
		                                  double gridSize3)
		{
			IFeatureClass featureClass;
			if (featureDataset == null)
			{
				featureClass = CreateFeatureClass(featureWorkspace, qualifiedName, geometryType,
				                                  spatialReference, configKeyword,
				                                  gridSize1, gridSize2, gridSize3);
			}
			else
			{
				featureClass = CreateFeatureClass(featureDataset, qualifiedName, geometryType,
				                                  spatialReference, configKeyword,
				                                  gridSize1, gridSize2, gridSize3);
			}

			SetAliasName(featureClass);

			return (ITable) featureClass;
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] IFeatureDataset featureDataset,
		                                         [NotNull] string qualifiedName,
		                                         esriGeometryType geometryType,
		                                         [NotNull] ISpatialReference
			                                         spatialReference,
		                                         [CanBeNull] string configKeyword,
		                                         double gridSize1,
		                                         double gridSize2,
		                                         double gridSize3)
		{
			_msg.InfoFormat("Creating feature class '{0}' in feature dataset '{1}'...",
			                qualifiedName, featureDataset.Name);

			IFields fields = CreateFeatureClassFields(geometryType, spatialReference,
			                                          gridSize1, gridSize2, gridSize3);

			return DatasetUtils.CreateSimpleFeatureClass(
				featureDataset, qualifiedName, fields, configKeyword);
		}

		private static void GrantPrivileges([NotNull] ITable table,
		                                    [NotNull] IEnumerable<string> readers,
		                                    [NotNull] IEnumerable<string> writers)
		{
			Grant(table, readers, esriSQLPrivilege.esriSelectPrivilege);
			Grant(table, writers, esriSQLPrivilege.esriSelectPrivilege |
			                      esriSQLPrivilege.esriInsertPrivilege |
			                      esriSQLPrivilege.esriUpdatePrivilege |
			                      esriSQLPrivilege.esriDeletePrivilege);
		}

		private static void Grant([NotNull] ITable table,
		                          [NotNull] IEnumerable<string> userNames,
		                          esriSQLPrivilege privileges)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(userNames, nameof(userNames));

			ISQLPrivilege sqlPrivilege = null;

			foreach (string userName in userNames)
			{
				if (sqlPrivilege == null)
				{
					IName datasetName = ((IDataset) table).FullName;
					sqlPrivilege = datasetName as ISQLPrivilege;

					if (sqlPrivilege == null)
					{
						_msg.WarnFormat("Unable to grant privileges on {0}",
						                DatasetUtils.GetName(table));
						return;
					}
				}

				_msg.DebugFormat("Granting privileges on {0} to {1}: {2}",
				                 DatasetUtils.GetName(table), userName, privileges);

				try
				{
					sqlPrivilege.Grant(userName, (int) privileges, withGrant: false);
				}
				catch (Exception e)
				{
					throw new Exception(
						$"Error granting privilege {sqlPrivilege} to {userName}: {e.Message}", e);
				}
			}
		}

		private static void RegisterAsVersioned([NotNull] ITable table)
		{
			var versionedObject = table as IVersionedObject;

			if (versionedObject == null)
			{
				return;
			}

			try
			{
				const bool isVersioned = true;
				versionedObject.RegisterAsVersioned(isVersioned);
			}
			catch (Exception e)
			{
				_msg.Warn($"Error registering {DatasetUtils.GetName(table)} as versioned: " +
				          $"{e.Message}. If necessary, manually register as versioned and " +
				          $"grant privileges again.", e);
			}
		}

		private IFields CreateFeatureClassFields(
			esriGeometryType geometryType,
			[NotNull] ISpatialReference spatialReference,
			double gridSize1, double gridSize2, double gridSize3)
		{
			IFieldsEdit result = new FieldsClass();

			const bool hasZ = true;
			const bool hasM = false;

			result.AddField(FieldUtils.CreateShapeField(geometryType, spatialReference,
			                                            gridSize1, gridSize2, gridSize3,
			                                            hasZ, hasM));

			AddFields(result, _errorDatasetSchema);

			return result;
		}

		private IFields CreateTableFields()
		{
			IFieldsEdit result = new FieldsClass();

			AddFields(result, _errorDatasetSchema);

			return result;
		}

		private static void AddFields([NotNull] IFieldsEdit fields,
		                              [NotNull] ErrorDatasetSchema schema)
		{
			foreach (IField field in GetFields(schema))
			{
				fields.AddField(field);
			}
		}

		[NotNull]
		private static IEnumerable<IField> GetFields([NotNull] ErrorDatasetSchema schema)
		{
			const int maxTextLength = 2000;
			const int operatorFieldLength = 50;
			const int qualityConditionNameLength = 255;
			const int affectedComponentLength = 255;

			yield return FieldUtils.CreateOIDField();

			yield return FieldUtils.CreateTextField(schema.OperatorFieldName,
			                                        operatorFieldLength,
			                                        schema.OperatorFieldAlias);

			yield return FieldUtils.CreateDateField(schema.DateOfCreationFieldName,
			                                        schema.DateOfCreationFieldAlias);

			yield return FieldUtils.CreateDateField(schema.DateOfChangeFieldName,
			                                        schema.DateOfChangeFieldAlias);

			yield return FieldUtils.CreateIntegerField(schema.QualityConditionIDFieldName,
			                                           schema.QualityConditionIDFieldAlias);

			yield return FieldUtils.CreateTextField(
				schema.QualityConditionParametersFieldName,
				maxTextLength,
				schema.QualityConditionParametersFieldAlias);

			yield return FieldUtils.CreateTextField(schema.QualityConditionNameFieldName,
			                                        qualityConditionNameLength,
			                                        schema.QualityConditionNameFieldAlias);

			yield return FieldUtils.CreateIntegerField(schema.StatusFieldName,
			                                           schema.StatusFieldAlias);

			yield return FieldUtils.CreateTextField(schema.ErrorDescriptionFieldName,
			                                        maxTextLength,
			                                        schema.ErrorDescriptionFieldAlias);

			yield return FieldUtils.CreateTextField(schema.ErrorObjectsFieldName,
			                                        maxTextLength,
			                                        schema.ErrorObjectsFieldAlias);

			yield return FieldUtils.CreateIntegerField(schema.ErrorTypeFieldName,
			                                           schema.ErrorTypeFieldAlias);

			yield return FieldUtils.CreateIntegerField(
				schema.QualityConditionVersionFieldName,
				schema.QualityConditionVersionFieldAlias);

			string affectedComponentFieldName = schema.ErrorAffectedComponentFieldName;
			if (! string.IsNullOrEmpty(affectedComponentFieldName))
			{
				yield return
					FieldUtils.CreateTextField(affectedComponentFieldName,
					                           affectedComponentLength,
					                           schema.ErrorAffectedComponentFieldAlias);
			}
		}

		private static bool ExistsDataset([NotNull] IWorkspace schemaOwnerWorkspace,
		                                  [NotNull] string unqualifiedDatasetName,
		                                  [NotNull] out string qualifiedName)
		{
			return ExistsDataset(schemaOwnerWorkspace, unqualifiedDatasetName,
			                     out qualifiedName, out ITable _);
		}

		private static bool ExistsDataset([NotNull] IWorkspace schemaOwnerWorkspace,
		                                  [NotNull] string unqualifiedDatasetName,
		                                  [NotNull] out string qualifiedName,
		                                  [CanBeNull] out ITable table)
		{
			qualifiedName = GetQualifiedName(schemaOwnerWorkspace, unqualifiedDatasetName);

			table = GetTable(schemaOwnerWorkspace, qualifiedName);

			return table != null;
		}

		[CanBeNull]
		private static IFeatureDataset GetFeatureDataset(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string name)
		{
			try
			{
				return workspace.OpenFeatureDataset(name);
			}
			catch (COMException)
			{
				// TODO catch specific case "feature dataset does not exist"
				return null;
			}
		}

		[CanBeNull]
		private static ITable GetTable([NotNull] IWorkspace workspace,
		                               [NotNull] string qualifiedName)
		{
			var featureWorkspace = (IFeatureWorkspace) workspace;
			try
			{
				return featureWorkspace.OpenTable(qualifiedName);
			}
			catch (COMException)
			{
				// TODO catch specific case "table does not exist"
				return null;
			}
		}

		[NotNull]
		private static string GetQualifiedName([NotNull] IWorkspace schemaOwnerWorkspace,
		                                       [NotNull] string unqualifiedDatasetName)
		{
			if (schemaOwnerWorkspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				return unqualifiedDatasetName;
			}

			string schemaOwner = WorkspaceUtils.GetConnectedUser(schemaOwnerWorkspace);

			// TODO won't work for sql server/postgresql
			return string.Format("{0}.{1}", schemaOwner, unqualifiedDatasetName);
		}
	}
}
