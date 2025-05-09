using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using Path = System.IO.Path;
#if !Server
using ESRI.ArcGIS.GeoDatabaseExtensions;
#endif

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class DatasetUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _queryPrefix = "%";

		/// <summary>
		/// Returns a value indicating if a dataset name references a given workspace, doing 
		/// either a version-specific comparison or disregarding version differences.
		/// </summary>
		/// <param name="datasetName">The dataset name.</param>
		/// <param name="workspace">The workspace.</param>
		/// <param name="workspaceComparison">Controls how workspaces are compared (exact comparison, 
		/// which includes version name, or disregarding version differences)
		/// </param>
		/// <remarks>If the dataset name is not valid (i.e. the workspace cannot be opened), 
		/// <c>false</c> is returned.</remarks>
		public static bool ReferencesWorkspace(
			[NotNull] IDatasetName datasetName,
			[NotNull] IWorkspace workspace,
			WorkspaceComparison workspaceComparison = WorkspaceComparison.Exact)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			const bool openDefaultVersion = true;
			const bool openVersionAsIs = false;

			switch (workspaceComparison)
			{
				case WorkspaceComparison.Exact:

					// compare the specific versions
					return TryOpenWorkspace(datasetName, openVersionAsIs) == workspace;

				case WorkspaceComparison.AnyUserAnyVersion:

					// check if both point to same gdb repository
					return WorkspaceUtils.IsSameDatabase(
						TryOpenWorkspace(datasetName, openDefaultVersion),
						workspace);

				case WorkspaceComparison.AnyUserSameVersion:
					return WorkspaceUtils.IsSameVersion(
						TryOpenWorkspace(datasetName, openVersionAsIs),
						workspace);

				default:
					throw new ArgumentOutOfRangeException(nameof(workspaceComparison));
			}
		}

		public static esriGeometryType GetShapeType(
			[NotNull] IFeatureClassName featureClassName)
		{
			esriGeometryType shapeType = featureClassName.ShapeType;
			if (shapeType != esriGeometryType.esriGeometryAny)
			{
				return shapeType;
			}

			try
			{
				var featureClass = (IFeatureClass) ((IName) featureClassName).Open();

				return featureClass.ShapeType;
			}
			catch (Exception)
			{
				// unable to open the feature class
				return shapeType;
			}
		}

		public static esriGeometryType GetShapeType([NotNull] IObjectClass objectClass)
		{
			IFeatureClass featureClass = objectClass as IFeatureClass;

			if (featureClass == null)
			{
				return esriGeometryType.esriGeometryNull;
			}

			return featureClass.ShapeType;
		}

		[CanBeNull]
		public static IField GetLengthField([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			try
			{
				return featureClass.LengthField;
			}
			catch (NotImplementedException)
			{
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		[CanBeNull]
		public static IField GetLengthField([NotNull] IReadOnlyFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			try
			{
				return featureClass.LengthField;
			}
			catch (NotImplementedException)
			{
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		[CanBeNull]
		public static IField GetAreaField([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			try
			{
				return featureClass.AreaField;
			}
			catch (NotImplementedException)
			{
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		[CanBeNull]
		public static IField GetAreaField([NotNull] IReadOnlyFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			try
			{
				return featureClass.AreaField;
			}
			catch (NotImplementedException)
			{
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		/// <summary>
		/// Determine if two given object classes are the same
		/// under the given definition of equality.
		/// </summary>
		/// <param name="objectClass1">An object class.</param>
		/// <param name="objectClass2">An object class.</param>
		/// <param name="equality">Definition of equality.</param>
		/// <returns>True if the object classes are the same; otherwise, false.</returns>
		public static bool IsSameObjectClass([NotNull] IObjectClass objectClass1,
		                                     [NotNull] IObjectClass objectClass2,
		                                     ObjectClassEquality equality)
		{
			Assert.ArgumentNotNull(objectClass1, nameof(objectClass1));
			Assert.ArgumentNotNull(objectClass2, nameof(objectClass2));

			switch (equality)
			{
				case ObjectClassEquality.DontEquate:
					return false;

				case ObjectClassEquality.SameTableAnyVersion:
					return IsSameObjectClass(objectClass1, objectClass2);

				case ObjectClassEquality.SameTableSameVersion:
					return objectClass1 == objectClass2 ||
					       IsSameObjectClass(objectClass1, objectClass2) &&
					       WorkspaceUtils.IsSameVersion(GetWorkspace(objectClass1),
					                                    GetWorkspace(objectClass2));

				case ObjectClassEquality.SameDatasetName:
					return objectClass1 == objectClass2 ||
					       string.Equals(((IDataset) objectClass1).Name,
					                     ((IDataset) objectClass2).Name);

				case ObjectClassEquality.SameInstance:
					return objectClass1 == objectClass2;

				default:
					throw new ArgumentOutOfRangeException(nameof(equality));
			}
		}

		[NotNull]
		public static IDataset OpenDataset([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			return (IDataset) ((IName) datasetName).Open();
		}

		[NotNull]
		public static IDataset OpenDataset([NotNull] IDatasetName datasetName,
		                                   [NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			IWorkspaceName origWorkspaceName = datasetName.WorkspaceName;

			IWorkspaceName2 workspaceName = WorkspaceUtils.GetWorkspaceName(workspace);
			try
			{
				datasetName.WorkspaceName = workspaceName;
				return OpenDataset(datasetName);
			}
			finally
			{
				// restore the previous workspace name
				datasetName.WorkspaceName = origWorkspaceName;
			}
		}

		[NotNull]
		public static IObjectClass OpenObjectClass(
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] string name)
		{
			return (IObjectClass) OpenTable(featureWorkspace, name);
		}

		[CanBeNull]
		public static IObjectClass TryOpenObjectClass(
			[NotNull] IWorkspace workspace,
			[NotNull] string featureClassName,
			out string message)
		{
			message = null;

			string errorPrefix =
				$"Cannot open feature class {featureClassName} in workspace {workspace.PathName}";

			IObjectClass objectClass = null;

			try
			{
				objectClass = OpenObjectClass((IFeatureWorkspace) workspace, featureClassName);
			}
			catch (COMException comException)
			{
				string error = Enum.GetName(typeof(fdoError), comException.ErrorCode);

				message = $"{errorPrefix}: {error}";
				_msg.Debug(message, comException);
			}
			catch (Exception e)
			{
				message = errorPrefix;
				_msg.Debug(message, e);
			}

			return objectClass;
		}

		[CanBeNull]
		public static IObjectClass TryOpenObjectClass([NotNull] string catalogPath,
		                                              out string message)
		{
			string workspaceCatalogPath =
				GetGdbWorkspaceCatalogPath(catalogPath, out _, out string featureClassName);

			if (workspaceCatalogPath == null)
			{
				message =
					$"{catalogPath} does not contain a supported workspace or is not a valid catalog path.";

				return null;
			}

			IWorkspace workspace =
				WorkspaceUtils.TryOpenWorkspace(workspaceCatalogPath, out message);

			if (workspace == null)
			{
				message =
					$"{catalogPath} does not contain a supported workspace or is not a valid catalog path.";

				return null;
			}

			return TryOpenObjectClass(workspace, Assert.NotNull(featureClassName), out message);
		}

		[CanBeNull]
		public static IObjectClass TryOpenObjectClass(
			[NotNull] IFeatureWorkspace featureWorkspace,
			int classId)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentCondition(classId >= 0, "invalid class Id: {0}", classId);

			string className =
				((IFeatureWorkspaceManage2) featureWorkspace).GetObjectClassNameByID(
					classId);

			if (string.IsNullOrEmpty(className))
			{
				return null;
			}

			return (IObjectClass) featureWorkspace.OpenTable(className);
		}

		[NotNull]
		public static IObjectClass OpenObjectClass(
			[NotNull] IFeatureWorkspace featureWorkspace,
			int classId)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentCondition(classId >= 0, "invalid class Id: {0}", classId);

			return Assert.NotNull(TryOpenObjectClass(featureWorkspace, classId),
			                      "object class for id {0} not found", classId);
		}

		[NotNull]
		public static IRelationshipClass OpenRelationshipClass(
			[NotNull] IFeatureWorkspace featureWorkspace,
			int relationshipClassId)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentCondition(relationshipClassId >= 0,
			                         "invalid relationship class Id: {0}",
			                         relationshipClassId);

			string className =
				((IFeatureWorkspaceManage2) featureWorkspace).GetRelationshipClassNameByID
					(relationshipClassId);
			Assert.NotNullOrEmpty(className, "relationship class for id {0} not found",
			                      relationshipClassId);

			return featureWorkspace.OpenRelationshipClass(className);
		}

		[NotNull]
		public static IFeatureClass OpenFeatureClass([NotNull] IWorkspace workspace,
		                                             [NotNull] string name)
		{
			var featureWorkspace = workspace as IFeatureWorkspace;
			Assert.NotNull(featureWorkspace, "not a feature workspace");

			return OpenFeatureClass(featureWorkspace, name);
		}

		[NotNull]
		public static IFeatureClass OpenFeatureClass([NotNull] IFeatureWorkspace workspace,
		                                             [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			return workspace.OpenFeatureClass(name);
		}

		[NotNull]
		public static ITable OpenTable([NotNull] IWorkspace workspace,
		                               [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			return OpenTable((IFeatureWorkspace) workspace, name);
		}

		[NotNull]
		public static ITable OpenTable([NotNull] IFeatureWorkspace workspace,
		                               [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			try
			{
				return workspace.OpenTable(name);
			}
			catch (Exception e)
			{
				var ws = WorkspaceUtils.WorkspaceToString(workspace);
				throw new InvalidOperationException(
					$"Error opening table '{name}' from workspace {ws}: {e.Message}", e);
			}
		}

		public static bool TryOpenTable([NotNull] IFeatureWorkspace workspace,
		                                [NotNull] string name,
		                                out ITable table)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			try
			{
				table = OpenTable(workspace, name);
				return true;
			}
			catch (Exception e)
			{
				table = null;
				return false;
			}
		}

		/// <summary>
		/// Opens a relationship class given its name and a featureWorkspace.
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		/// <param name="name">The relationship class name.</param>
		/// <returns></returns>
		[NotNull]
		public static IRelationshipClass OpenRelationshipClass(
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] string name)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			try
			{
				return featureWorkspace.OpenRelationshipClass(name);
			}
			catch (Exception e)
			{
				var ws = WorkspaceUtils.WorkspaceToString(featureWorkspace);
				throw new InvalidOperationException(
					$"Error opening relationship class '{name}' from workspace {ws}: {e.Message}",
					e);
			}
		}

		[NotNull]
		public static IFeatureClass OpenShapefile([NotNull] string directory,
		                                          [NotNull] string file)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(file, nameof(file));

			IFeatureWorkspace shapeWorkspace =
				WorkspaceUtils.OpenShapefileWorkspace(directory);
			return shapeWorkspace.OpenFeatureClass(file);
		}

		[NotNull]
		public static IRasterDataset OpenRasterDataset([NotNull] string fullPath)
		{
			Assert.ArgumentNotNullOrEmpty(fullPath, nameof(fullPath));

			var dirInfo = new DirectoryInfo(fullPath);
			DirectoryInfo parent = dirInfo.Parent;

			Assert.NotNull(parent, "no parent directory");

			string directory = parent.FullName;
			string rasterName = dirInfo.Name;

			if (directory.EndsWith("gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				IWorkspace fgdbWorkspace = WorkspaceUtils.OpenFileGdbWorkspace(directory);
				return OpenRasterDataset(fgdbWorkspace, rasterName);
			}

			// TODO: Access workspace, SDE workspace

			return OpenRasterDataset(directory, rasterName);
		}

		/// <summary>
		/// Opens a file raster dataset residing in the provided directory.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="rasterName"></param>
		/// <returns></returns>
		[NotNull]
		public static IRasterDataset OpenRasterDataset([NotNull] string directory,
		                                               [NotNull] string rasterName)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(rasterName, nameof(rasterName));

			IRasterWorkspace2 workspace = WorkspaceUtils.OpenRasterWorkspace(directory);

			string rasterFullName = Path.Combine(directory, rasterName);

			// rasterName might be a directory (e.g. grid) or a file (e.g. tif)
			if (! Directory.Exists(rasterFullName) && ! File.Exists(rasterFullName))
			{
				Ex.Throw<FileNotFoundException>("File or directory not found: {0}",
				                                rasterFullName);
			}

			IRasterDataset rasterDataset;

			try
			{
				rasterDataset = workspace.OpenRasterDataset(rasterName);
			}
			catch (Exception ex)
			{
				_msg.Debug(
					string.Format("Error opening raster dataset {0} in {1}.", rasterName,
					              directory), ex);
				throw;
			}

			return rasterDataset;
		}

		[NotNull]
		public static IRasterDataset OpenRasterDataset([NotNull] IWorkspace workspace,
		                                               [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			var rasName = (IName) FindRootDatasetName(workspace,
			                                          esriDatasetType.esriDTRasterDataset,
			                                          name);
			if (rasName == null)
			{
				rasName = (IName) FindRootDatasetName(workspace,
				                                      esriDatasetType.esriDTMosaicDataset, name);
			}

			Assert.NotNull(rasName, "raster dataset not found: {0}", name);

			return (IRasterDataset) rasName.Open();
		}

		[NotNull]
		public static ITin OpenTin([NotNull] string directory, [NotNull] string name)
		{
			const bool fail = true;
			return Assert.NotNull(OpenTin(directory, name, fail));
		}

		[NotNull]
		public static ITin OpenTin([NotNull] string fullPath)
		{
			var dirInfo = new DirectoryInfo(fullPath);
			DirectoryInfo parent = dirInfo.Parent;

			Assert.NotNull(parent, "no parent directory");

			string directory = parent.FullName;
			string tinName = dirInfo.Name;

			return OpenTin(directory, tinName);
		}

		[CanBeNull]
		public static IDatasetName FindRootDatasetName([NotNull] IWorkspace workspace,
		                                               esriDatasetType datasetType,
		                                               [NotNull] string name)
		{
			IEnumDatasetName names = GetRootDatasetNames(workspace, datasetType);
			if (names == null)
			{
				return null;
			}

			names.Reset();

			string ownerName = GetOwnerName(workspace, name);

			IDatasetName datasetName;
			while ((datasetName = names.Next()) != null)
			{
				if (string.IsNullOrEmpty(ownerName))
				{
					// no owner part in search name, compare only the table name
					string tableName = GetTableName(workspace, datasetName.Name);

					if (string.Compare(tableName, name,
					                   StringComparison.OrdinalIgnoreCase) == 0)
					{
						return datasetName;
					}
				}
				else
				{
					// owner part in search name, compare the full table names
					if (string.Compare(datasetName.Name, name,
					                   StringComparison.OrdinalIgnoreCase) == 0)
					{
						return datasetName;
					}
				}
			}

			return null;
		}

		[CanBeNull]
		public static IDatasetName FindRootDatasetName(
			[NotNull] IFeatureWorkspace workspace,
			esriDatasetType datasetType,
			[NotNull] string name)
		{
			return FindRootDatasetName((IWorkspace) workspace, datasetType, name);
		}

		[NotNull]
		public static IFeatureClass CreateSimpleFeatureClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string fclassName,
			[CanBeNull] string configKeyWord,
			params IField[] fields)
		{
			return CreateSimpleFeatureClass(
				workspace, fclassName, FieldUtils.CreateFields(fields), configKeyWord);
		}

		[NotNull]
		public static IFeatureClass CreateSimpleFeatureClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string fclassName,
			[NotNull] IFields fields,
			[CanBeNull] string configKeyWord = null,
			[CanBeNull] string shapeFieldName = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(fclassName, nameof(fclassName));
			Assert.ArgumentNotNull(fields, nameof(fields));

			try
			{
				return workspace.CreateFeatureClass(
					fclassName, fields, GetFeatureUID(), null,
					esriFeatureType.esriFTSimple,
					shapeFieldName ?? FieldUtils.GetShapeFieldName(), configKeyWord);
			}
			catch (Exception e)
			{
				LogCreateFeatureClassParameters(workspace, fclassName, fields, configKeyWord);

				if (e is COMException comEx &&
				    comEx.ErrorCode == (int) fdoError.FDO_E_TABLE_ALREADY_EXISTS)
				{
					throw new DataException(
						$"Error creating feature class '{fclassName}' because it already exists.",
						comEx);
				}

				throw new Exception($"Error creating feature class '{fclassName}'", e);
			}
		}

		[NotNull]
		public static IFeatureClass CreateSimpleFeatureClass(
			[NotNull] IFeatureDataset featureDataset,
			[NotNull] string fclassName,
			[NotNull] IFields fields,
			[CanBeNull] string configKeyWord)
		{
			Assert.ArgumentNotNull(featureDataset, nameof(featureDataset));
			Assert.ArgumentNotNullOrEmpty(fclassName, nameof(fclassName));
			Assert.ArgumentNotNull(fields, nameof(fields));

			return featureDataset.CreateFeatureClass(
				fclassName, fields, GetFeatureUID(), null,
				esriFeatureType.esriFTSimple,
				FieldUtils.GetShapeFieldName(),
				configKeyWord);
		}

		[NotNull]
		public static IFeatureClass CreateAnnotationFeatureClass(
			[NotNull] IFeatureDataset featureDataset,
			[NotNull] string fclassName,
			[NotNull] IFields fields,
			[CanBeNull] string configKeyWord = null)
		{
			Assert.ArgumentNotNull(featureDataset, nameof(featureDataset));
			Assert.ArgumentNotNullOrEmpty(fclassName, nameof(fclassName));
			Assert.ArgumentNotNull(fields, nameof(fields));

			return featureDataset.CreateFeatureClass(
				fclassName, fields, GetAnnotationFeatureUID(),
				GetAnnotationFeatureClassExtensionUID(),
				esriFeatureType.esriFTAnnotation,
				FieldUtils.GetShapeFieldName(),
				configKeyWord);
		}

		[NotNull]
		public static ITable CreateTable([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] string name,
		                                 [NotNull] params IField[] fields)
		{
			return CreateTable(workspace, name, null, fields);
		}

		[NotNull]
		public static ITable CreateTable([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] string name,
		                                 [CanBeNull] string configKeyword,
		                                 params IField[] fields)
		{
			return CreateTable(workspace, name, configKeyword, FieldUtils.CreateFields(fields));
		}

		[NotNull]
		public static ITable CreateTable([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] string name,
		                                 [CanBeNull] string configKeyword,
		                                 [NotNull] IFields fields)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(fields, nameof(fields));

			try
			{
				return workspace.CreateTable(name, fields, GetObjectUID(), null, configKeyword);
			}
			catch (Exception)
			{
				try
				{
					_msg.DebugFormat("Error creating table '{0}'", name);
					_msg.DebugFormat("Workspace: {0}", WorkspaceUtils.GetConnectionString(
						                 (IWorkspace) workspace, true));
					_msg.DebugFormat("Config keyword: {0}", configKeyword ?? "<null>");
					LogFields(fields);
				}
				catch (Exception e)
				{
					_msg.Debug("Error writing to log", e);
				}

				throw;
			}
		}

		public static bool TrySetAliasName([NotNull] ITable table,
		                                   [NotNull] string aliasName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(aliasName, nameof(aliasName));

			var objectClass = table as IObjectClass;
			return objectClass != null && TrySetAliasName(objectClass, aliasName);
		}

		public static bool TrySetAliasName([NotNull] IObjectClass objectClass,
		                                   [NotNull] string aliasName)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNullOrEmpty(aliasName, nameof(aliasName));

			var schemaEdit = objectClass as IClassSchemaEdit;
			if (schemaEdit == null)
			{
				return false;
			}

			schemaEdit.AlterAliasName(aliasName);
			return true;
		}

		[NotNull]
		public static IFeatureDataset CreateFeatureDataset(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string datasetName,
			[NotNull] ISpatialReference spatialReference)
		{
			ISpatialReference highPrecisionSpatialReference;
			SpatialReferenceUtils.EnsureHighPrecision(spatialReference,
			                                          out highPrecisionSpatialReference);

			return workspace.CreateFeatureDataset(datasetName, highPrecisionSpatialReference);
		}

		public static bool DeleteFeatureDataset([NotNull] IFeatureWorkspace workspace,
		                                        [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			IDatasetName datasetName = FindRootDatasetName(
				workspace,
				esriDatasetType.esriDTFeatureDataset,
				name);

			return datasetName != null && DeleteDataset(workspace, datasetName);
		}

		public static bool DeleteDataset([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var wsManage = (IFeatureWorkspaceManage2) workspace;

			if (! wsManage.CanDelete((IName) datasetName))
			{
				return false;
			}

			// NOTE: this can fail silently, observed for established locks
			wsManage.DeleteByName(datasetName);

			return true;
		}

		public static bool DeleteFeatureClass([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			var dataset = (IDataset) featureClass;
			var datasetName = dataset.FullName as IDatasetName;
			var featureWorkspace = (IFeatureWorkspace) dataset.Workspace;

			return datasetName != null && DeleteDataset(featureWorkspace, datasetName);
		}

		public static bool DeleteFeatureClass([NotNull] IFeatureWorkspace workspace,
		                                      [NotNull] string fclassName)
		{
			return DeleteDataset((IWorkspace) workspace,
			                     esriDatasetType.esriDTFeatureClass, fclassName);
		}

		public static bool DeleteFeatureClass([NotNull] IWorkspace workspace,
		                                      [NotNull] string fclassName)
		{
			return DeleteFeatureClass((IFeatureWorkspace) workspace, fclassName);
		}

		public static bool DeleteDataset([NotNull] IWorkspace workspace,
		                                 esriDatasetType datasetType,
		                                 [NotNull] string name)
		{
			IDatasetName datasetName = FindRootDatasetName(workspace, datasetType, name);

			return datasetName != null &&
			       DeleteDataset((IFeatureWorkspace) workspace, datasetName);
		}

		public static void RegisterAsVersioned([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			var versionedObject = (IVersionedObject) featureClass;
			RegisterAsVersioned(versionedObject);
		}

		public static void RegisterAsVersioned([NotNull] IFeatureDataset featureDataset)
		{
			Assert.ArgumentNotNull(featureDataset, nameof(featureDataset));

			var versionedObject = (IVersionedObject) featureDataset;

			RegisterAsVersioned(versionedObject);
		}

		public static void RegisterAsVersioned([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var versionedObject = (IVersionedObject) table;

			RegisterAsVersioned(versionedObject);
		}

		public static bool IsVersioned([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			IDataset dataset;
			try
			{
				dataset = ((IName) datasetName).Open() as IDataset;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					string.Format("Unable to open dataset {0}",
					              datasetName.Name), ex);
			}

			return dataset != null && IsVersioned(dataset);
		}

		public static bool IsVersioned([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return IsVersioned((IDataset) objectClass);
		}

		public static bool IsVersioned([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			var versionedObj = dataset as IVersionedObject;

			return versionedObj != null && versionedObj.IsRegisteredAsVersioned;
		}

		public static bool IsRegisteredAsObjectClass([NotNull] IReadOnlyTable readOnlyTable)
		{
			Assert.ArgumentNotNull(readOnlyTable, nameof(readOnlyTable));
			if (! (readOnlyTable is ReadOnlyTable ro))
			{
				return false;
			}

			ITable table = ro.BaseTable;
			if (table == null)
			{
				return false;
			}

			if (table is VirtualTable)
			{
				return false;
			}

			return table is IObjectClass objectClass
				       ? IsRegisteredAsObjectClass(objectClass)
				       : IsRegisteredAsObjectClass(GetWorkspace(table), GetName(table));
		}

		public static bool IsRegisteredAsObjectClass([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return table is IObjectClass objectClass
				       ? IsRegisteredAsObjectClass(objectClass)
				       : IsRegisteredAsObjectClass(GetWorkspace(table), GetName(table));
		}

		public static bool IsRegisteredAsObjectClass([NotNull] IWorkspace workspace,
		                                             [NotNull] string name)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			return workspace is IFeatureWorkspaceManage workspaceManage &&
			       workspaceManage.IsRegisteredAsObjectClass(name);
		}

		public static bool IsRegisteredAsObjectClass([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return objectClass.ObjectClassID >= 0;
		}

		[NotNull]
		public static string GetUnqualifiedName([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return GetTableName(dataset);
		}

		[NotNull]
		public static string GetUnqualifiedName([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			return GetTableName(datasetName);
		}

		[NotNull]
		public static string GetUnqualifiedName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return GetTableName((IDataset) objectClass);
		}

		[NotNull]
		public static string GetUnqualifiedName([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetTableName((IDataset) table);
		}

		[NotNull]
		public static string GetUnqualifiedName(
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			return GetTableName((IDataset) relationshipClass);
		}

		public static string GetTableName([NotNull] IWorkspace workspace,
		                                  [NotNull] string fullTableName)
		{
			string tableName;

			if (workspace is ISQLSyntax sqlSyntax)
			{
				sqlSyntax.ParseTableName(fullTableName, out _, out _, out tableName);
			}
			else if (workspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				// Virtual workspace, such as GdbWorkspace
				tableName = ModelElementNameUtils.GetUnqualifiedName(fullTableName);
			}
			else
			{
				// TOP-5790: Shapefiles, rasters or something else. Do not un-qualify names potentially containing a dot.
				tableName = fullTableName;
			}

			return tableName;
		}

		public static string GetTableName([NotNull] IFeatureClass featureClass)
		{
			return GetTableName((IDataset) featureClass);
		}

		/// <summary>
		/// Gets the unqualified table name of a dataset (without the owner name).
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetTableName([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IWorkspace workspace = dataset.Workspace;

			try
			{
				return Assert.NotNull(GetTableName(workspace, dataset.Name));
			}
			finally
			{
				if (workspace != null && Marshal.IsComObject(workspace))
				{
					// Avoid locking the workspace
					Marshal.ReleaseComObject(workspace);
				}
			}
		}

		/// <summary>
		/// Gets the unqualified table name for a dataset.
		/// </summary>
		/// <param name="datasetName">Name of the dataset.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetTableName([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var workspace = (IWorkspace) ((IName) datasetName.WorkspaceName).Open();

			try
			{
				return GetTableName(workspace, datasetName.Name);
			}
			finally
			{
				// Avoid locking the workspace
				Marshal.ReleaseComObject(workspace);
			}
		}

		/// <summary>
		/// Gets the unqualified table name for an object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns></returns>
		public static string GetTableName([NotNull] IObjectClass objectClass)
		{
			return GetTableName((IDataset) objectClass);
		}

		/// <summary>
		/// Gets the unqualified table name for a table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns></returns>
		public static string GetTableName([NotNull] ITable table)
		{
			return GetTableName((IDataset) table);
		}

		public static string QualifyTableName([NotNull] IWorkspace workspace,
		                                      [CanBeNull] string databaseName,
		                                      [CanBeNull] string ownerName,
		                                      [NotNull] string tableName)
		{
			return QualifyTableName((IFeatureWorkspace) workspace,
			                        databaseName, ownerName, tableName);
		}

		public static string QualifyTableName([NotNull] IFeatureWorkspace workspace,
		                                      [CanBeNull] string databaseName,
		                                      [CanBeNull] string ownerName,
		                                      [NotNull] string tableName)
		{
			Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));
			Assert.True(workspace is ISQLSyntax, "workspace is not ISQLSyntax");

			var sqlSyntax = (ISQLSyntax) workspace;

			return sqlSyntax.QualifyTableName(
				databaseName ?? string.Empty, ownerName ?? string.Empty, tableName);
		}

		/// <summary>
		/// Create a 'Query Layer' type of feature class. Unlike the QueryFeatureClasses created
		/// by TableJoinUtils, this creates a class with unqualified field names.
		/// </summary>
		/// <param name="sqlWorkspace">The workspace</param>
		/// <param name="sql">The query that contains a single shape field column which can
		/// be found in the sde column registry.</param>
		/// <param name="name">The name of the table / feature class which will be adapted
		/// in case it does not start with '%'</param>
		/// <param name="oidFieldName">The OBJECTID field</param>
		/// <param name="xyTolerance">The xyTolerance </param>
		/// <returns></returns>
		public static ITable CreateQueryLayerClass(
			[NotNull] ISqlWorkspace sqlWorkspace,
			[NotNull] string sql,
			[NotNull] string name,
			[CanBeNull] string oidFieldName = null,
			double xyTolerance = double.NaN)
		{
			IQueryDescription queryDescription =
				CreateQueryDescription(sqlWorkspace, sql, oidFieldName, xyTolerance);

			return CreateQueryLayerClass(sqlWorkspace, queryDescription, name);
		}

		public static IQueryDescription CreateQueryDescription(
			[NotNull] ISqlWorkspace sqlWorkspace,
			[NotNull] string sql,
			[CanBeNull] string oidFieldName = null,
			double xyTolerance = double.NaN)
		{
			_msg.DebugFormat("Getting query layer description for {0}", sql);

			IQueryDescription queryDescription = sqlWorkspace.GetQueryDescription(sql);

			if (! string.IsNullOrEmpty(oidFieldName))
			{
				queryDescription.OIDFields = oidFieldName;
			}

			var srTolerance = (ISpatialReferenceTolerance) queryDescription.SpatialReference;

			if (srTolerance != null)
			{
				if (! double.IsNaN(xyTolerance))
				{
					// TOP-5355: In some environments (versions) the SR created by GetQueryDescription()
					// has 0 tolerances which subsequently results in HRESULT E_FAIL in topo-operators
					// because it is not simple. Typically the tolerance is just the default of the SR
					// which, in some situations can be equal or even smaller than the resolution!

					// NOTE: The spatial reference must not be set to anything even slightly different 
					// from the one derived by the Srid (internally) or no rows are found.

					// However, just chaning the tolerance seems to work:

					srTolerance.XYTolerance = xyTolerance;
				}

				if (srTolerance.XYToleranceValid != esriSRToleranceEnum.esriSRToleranceOK)
				{
					// Safety net: Do not allow a tolerance equal or smaller than the resolution:
					srTolerance.SetMinimumXYTolerance();
				}
			}

			return queryDescription;
		}

		public static ITable CreateQueryLayerClass([NotNull] ISqlWorkspace sqlWorkspace,
		                                           [NotNull] IQueryDescription queryDescription,
		                                           [NotNull] string name)
		{
			// NOTE: the unqualified name of a query class must start with a '%'
			if (! IsQueryLayerClassName(name))
			{
				name = GetQueryLayerClassName((IFeatureWorkspace) sqlWorkspace, name);
			}

			_msg.DebugFormat(
				"Opening query layer with name {0} using the following query description: {1}",
				name, QueryDescriptionToString(queryDescription));

			ITable queryClass = sqlWorkspace.OpenQueryClass(name, queryDescription);
			return queryClass;
		}

		[NotNull]
		public static string GetQueryLayerClassName([NotNull] IFeatureWorkspace workspace,
		                                            [NotNull] string gdbDatasetName)
		{
			string databaseName;
			string ownerName;
			string tableName;
			ParseTableName(workspace, gdbDatasetName,
			               out databaseName,
			               out ownerName,
			               out tableName);

			tableName = _queryPrefix + tableName;
			return QualifyTableName(workspace,
			                        databaseName,
			                        ownerName,
			                        tableName);
		}

		public static string QueryDescriptionToString(
			[CanBeNull] IQueryDescription queryDescription)
		{
			if (queryDescription == null)
			{
				return "<null>";
			}

			StringBuilder sb = new StringBuilder();

			sb.AppendLine(queryDescription.Query);
			sb.AppendLine($"OID Column: {queryDescription.OIDColumnName}");
			sb.AppendLine($"SHAPE: {queryDescription.ShapeColumnName}");
			sb.AppendLine($"Geometry Type: {queryDescription.GeometryType}");
			sb.AppendLine($"SRID: {queryDescription.Srid}");
			sb.AppendLine(
				$"SRID: {SpatialReferenceUtils.ToString(queryDescription.SpatialReference)}");

			return sb.ToString();
		}

		public static bool IsQueryLayerClassName(string className)
		{
			return className.IndexOf(_queryPrefix, StringComparison.Ordinal) >= 0;
		}

		[NotNull]
		public static string QualifyFieldName([NotNull] IObjectClass objectClass,
		                                      [NotNull] string unqualifiedFieldName)
		{
			return QualifyFieldName((ITable) objectClass, unqualifiedFieldName);
		}

		public static string QualifyFieldName([NotNull] IReadOnlyTable table,
		                                      [NotNull] string unqualifiedFieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(unqualifiedFieldName, nameof(unqualifiedFieldName));

			IWorkspace workspace = table.Workspace;

			if (workspace is ISQLSyntax sqlWorkspace)
			{
				string result = sqlWorkspace.QualifyColumnName(table.Name, unqualifiedFieldName);

				return result;
			}

			return $"{table.Name}.{unqualifiedFieldName}";
		}

		[NotNull]
		public static string QualifyFieldName([NotNull] ITable table,
		                                      [NotNull] string unqualifiedFieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(unqualifiedFieldName, nameof(unqualifiedFieldName));

			IWorkspace workspace = GetWorkspace(table);
			string tableName = GetName(table);

			return QualifyFieldName(tableName, unqualifiedFieldName, workspace);
		}

		[NotNull]
		public static string QualifyFieldName([NotNull] string tableName,
		                                      [NotNull] string unqualifiedFieldName,
		                                      [NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(tableName, nameof(tableName));
			Assert.ArgumentNotNullOrEmpty(unqualifiedFieldName, nameof(unqualifiedFieldName));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			return ((ISQLSyntax) workspace).QualifyColumnName(tableName, unqualifiedFieldName);
		}

		[CanBeNull]
		public static string GetGdbWorkspaceCatalogPath(
			[NotNull] string objectClassCatalogPath,
			[CanBeNull] out string featureDataset,
			[CanBeNull] out string featureClass)
		{
			Assert.ArgumentNotNullOrEmpty(objectClassCatalogPath,
			                              nameof(objectClassCatalogPath));

			// Shave off the last part until it is a valid connection file / file workspace
			string candidateWorkspace = objectClassCatalogPath;

			featureClass = null;
			featureDataset = null;
			do
			{
				if (IsGdbWorkspacePath(candidateWorkspace))
				{
					return candidateWorkspace;
				}

				if (featureClass == null)
				{
					featureClass = Path.GetFileName(candidateWorkspace);
				}
				else
				{
					featureDataset = Path.GetFileName(candidateWorkspace);
				}

				candidateWorkspace = Path.GetDirectoryName(candidateWorkspace);
			} while (! string.IsNullOrEmpty(candidateWorkspace));

			return null;
		}

		public static void ParseTableName([NotNull] IFeatureWorkspace featureWorkspace,
		                                  [NotNull] string fullTableName,
		                                  [CanBeNull] out string databaseName,
		                                  [CanBeNull] out string ownerName,
		                                  [NotNull] out string tableName)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNullOrEmpty(fullTableName, nameof(fullTableName));

			var workspace = (IWorkspace) featureWorkspace;
			ParseTableName(workspace, fullTableName,
			               out databaseName,
			               out ownerName,
			               out tableName);
		}

		public static void ParseTableName([NotNull] IDatasetName datasetName,
		                                  [CanBeNull] out string databaseName,
		                                  [CanBeNull] out string ownerName,
		                                  [NotNull] out string tableName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var workspace = (IWorkspace) ((IName) datasetName.WorkspaceName).Open();

			ParseTableName(workspace, datasetName.Name,
			               out databaseName,
			               out ownerName,
			               out tableName);
		}

		public static void ParseTableName([NotNull] IWorkspace workspace,
		                                  [NotNull] string fullTableName,
		                                  [CanBeNull] out string databaseName,
		                                  [CanBeNull] out string ownerName,
		                                  [NotNull] out string tableName)
		{
			if (workspace is ISQLSyntax sqlSyntax)
			{
				sqlSyntax.ParseTableName(fullTableName,
				                         out databaseName,
				                         out ownerName,
				                         out tableName);
			}
			else
			{
				databaseName = string.Empty;
				ownerName = string.Empty;

				tableName = fullTableName;
			}
		}

		/// <summary>
		/// Gets the schema part of a dataset name
		/// </summary>
		public static string GetOwnerName([NotNull] IFeatureWorkspace featureWorkspace,
		                                  [NotNull] string fullTableName)
		{
			var workspace = (IWorkspace) featureWorkspace;

			return GetOwnerName(workspace, fullTableName);
		}

		public static string GetOwnerName([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var workspace = (IWorkspace) ((IName) datasetName.WorkspaceName).Open();

			return GetOwnerName(workspace, datasetName.Name);
		}

		public static string GetOwnerName([NotNull] IWorkspace workspace,
		                                  [NotNull] string fullTableName)
		{
			if (workspace is ISQLSyntax sqlSyntax)
			{
				sqlSyntax.ParseTableName(fullTableName, out _, out string ownerName, out _);

				return ownerName;
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the feature dataset names for a given workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="owner">The owner of the feature datasets to return (optional).</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IDatasetName> GetFeatureDatasetNames(
			[NotNull] IWorkspace workspace,
			[CanBeNull] string owner = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			IEnumDatasetName enumDatasetName =
				GetRootDatasetNames(workspace, esriDatasetType.esriDTFeatureDataset);

			if (enumDatasetName == null)
			{
				yield break;
			}

			enumDatasetName.Reset();

			IDatasetName datasetName = enumDatasetName.Next();
			var sqlSyntax = workspace as ISQLSyntax;

			string trimmedOwner = owner?.Trim();

			while (datasetName != null)
			{
				if (string.IsNullOrEmpty(trimmedOwner) ||
				    sqlSyntax == null ||
				    DatasetOwnerMatches(datasetName.Name, trimmedOwner, sqlSyntax))
				{
					yield return datasetName;
				}

				datasetName = enumDatasetName.Next();
			}

			enumDatasetName.Reset();
		}

		[NotNull]
		public static IEnumerable<IDatasetName> GetDatasetNames(
			[NotNull] IWorkspace workspace,
			IEnumerable<esriDatasetType> datasetTypes,
			[CanBeNull] string owner = null)
		{
			return GetDatasetNames((IFeatureWorkspace) workspace,
			                       datasetTypes, owner);
		}

		[NotNull]
		public static IEnumerable<IDatasetName> GetDatasetNames(
			[NotNull] IWorkspace workspace,
			params esriDatasetType[] datasetTypes)
		{
			return GetDatasetNames((IFeatureWorkspace) workspace, datasetTypes);
		}

		[NotNull]
		public static IEnumerable<IDatasetName> GetDatasetNames(
			[NotNull] IFeatureWorkspace featureWorkspace,
			params esriDatasetType[] datasetTypes)
		{
			return GetDatasetNames(featureWorkspace,
			                       (IEnumerable<esriDatasetType>) datasetTypes);
		}

		[NotNull]
		public static IEnumerable<IDatasetName> GetDatasetNames(
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] IEnumerable<esriDatasetType> datasetTypes,
			[CanBeNull] string owner = null)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNull(datasetTypes, nameof(datasetTypes));

			// This is very slow for workspaces with many datasets. Either it should be cached
			// or we can replace it by only searching for the required datasets
			IList<IDatasetName> featureDatasetNames =
				GetFeatureDatasetNames((IWorkspace) featureWorkspace, owner).ToList();

			foreach (esriDatasetType datasetType in datasetTypes)
			{
				foreach (IDatasetName datasetName in GetDatasetNames(featureWorkspace,
					         datasetType,
					         featureDatasetNames,
					         owner))
				{
					yield return datasetName;
				}
			}
		}

		/// <summary>
		/// Gets the dataset names.
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		/// <param name="datasetType">Type of the dataset.</param>
		/// <param name="owner">The dataset owner (optional)</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IDatasetName> GetDatasetNames(
			[NotNull] IFeatureWorkspace featureWorkspace,
			esriDatasetType datasetType,
			[CanBeNull] string owner = null)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			List<IDatasetName> featureDatasetNames =
				GetFeatureDatasetNames((IWorkspace) featureWorkspace, owner).ToList();

			return GetDatasetNames(featureWorkspace, datasetType, featureDatasetNames, owner);
		}

		[NotNull]
		private static IList<IDatasetName> GetDatasetNames(
			[NotNull] IFeatureWorkspace featureWorkspace,
			esriDatasetType datasetType,
			[NotNull] IEnumerable<IDatasetName> featureDatasetNames,
			[CanBeNull] string owner)
		{
			var result = new List<IDatasetName>();

			var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var ws = (IWorkspace) featureWorkspace;
			var sqlSyntax = ws as ISQLSyntax;

			string trimmedOwner = owner?.Trim();

			if (! MustBeInFeatureDataset(datasetType))
			{
				// get root level dataset names
				IEnumDatasetName names = GetRootDatasetNames(ws, datasetType);

				if (names != null)
				{
					AddDatasetNames(result, names, uniqueNames, trimmedOwner, sqlSyntax);
				}
			}

			if (CanBeInFeatureDataset(datasetType))
			{
				foreach (IDatasetName featureDatasetName in featureDatasetNames)
				{
					if (StringUtils.IsNullOrEmptyOrBlank(featureDatasetName.Name))
					{
						// see PSM-459
						_msg.Warn("Ignoring unnamed feature dataset");
						continue;
					}

					if (! string.IsNullOrEmpty(trimmedOwner) &&
					    sqlSyntax != null &&
					    ! DatasetOwnerMatches(featureDatasetName.Name, trimmedOwner, sqlSyntax))
					{
						// ignore feature dataset since it has a non-matching owner
						continue;
					}

					// get names of datasets contained in the feature dataset
					IEnumDatasetName names = null;
					try
					{
						// NOTE: esriDatasetType.esriDTAny cannot be used here (argument exception)
						names = ((IDatasetContainerName) featureDatasetName).DatasetNames[
							datasetType];
					}
					catch (COMException ex)
					{
						if (ex.ErrorCode == -2147467259)
						{
							// BUG in ArcGIS 9.3 (Build 1745):
							// Feature Datasets without visible Feature Classes (no privileges) 
							// AND with one or more topologies throws COMException.
							_msg.DebugFormat(
								"Datasets in feature dataset '{0}' could not be determined.",
								featureDatasetName.Name);
						}
						else
						{
							throw;
						}
					}

					if (names != null)
					{
						AddDatasetNames(result, names, uniqueNames, trimmedOwner, sqlSyntax);
					}
				}
			}

			return result;
		}

		[CanBeNull]
		public static IEnumDatasetName GetRootDatasetNames(
			[NotNull] IWorkspace workspace, esriDatasetType datasetType)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			try
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.VerboseDebug(
						() => $"Getting dataset names of type {datasetType} in " +
						      $"{WorkspaceUtils.GetWorkspaceDisplayText(workspace)}");
				}

				return workspace.DatasetNames[datasetType];
			}
			catch (Exception exception)
			{
				if (WorkspaceUtils.IsOleDbWorkspace(workspace) &&
				    (datasetType == esriDatasetType.esriDTMosaicDataset ||
				     datasetType == esriDatasetType.esriDTRasterDataset))
				{
					// PSM-462: ignore known exception when getting mosaic or raster dataset names via OLE-DB connection
					return null;
				}

				_msg.DebugFormat("Error getting dataset names for {0} in {1}: {2}",
				                 datasetType,
				                 WorkspaceUtils.GetConnectionString(
					                 workspace, replacePassword: true),
				                 exception.Message);
				throw;
			}
		}

		[NotNull]
		public static IEnumerable<IObjectClass> GetObjectClasses(
			[NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			foreach (IDataset dataset in
			         GetDatasets(workspace,
			                     esriDatasetType.esriDTFeatureClass,
			                     esriDatasetType.esriDTTable))
			{
				if (dataset is IObjectClass objectClass)
				{
					yield return objectClass;
				}
			}

			foreach (IDataset dataset in
			         GetDatasets(workspace, esriDatasetType.esriDTFeatureDataset))
			{
				foreach (
					IFeatureClass featureClass in GetFeatureClasses((IFeatureDataset) dataset))
				{
					yield return featureClass;
				}
			}
		}

		/// <summary>
		/// Gets the feature classes in a feature dataset.
		/// </summary>
		/// <param name="featureDataset">The feature dataset.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IFeatureClass> GetFeatureClasses(
			[NotNull] IFeatureDataset featureDataset)
		{
			Assert.ArgumentNotNull(featureDataset, nameof(featureDataset));

			var featureClassContainer = (IFeatureClassContainer) featureDataset;

			return GetFeatureClasses(featureClassContainer);
		}

		/// <summary>
		/// Gets the feature classes in a feature class container (e.g. feature dataset or topology)
		/// </summary>
		/// <param name="featureClassContainer">The feature class container.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IFeatureClass> GetFeatureClasses(
			[NotNull] IFeatureClassContainer featureClassContainer)
		{
			Assert.ArgumentNotNull(featureClassContainer, nameof(featureClassContainer));

			IEnumFeatureClass enumFeatureClass = featureClassContainer.Classes;

			enumFeatureClass.Reset();

			IFeatureClass featureClass;
			while ((featureClass = enumFeatureClass.Next()) != null)
			{
				yield return featureClass;
			}
		}

		[NotNull]
		public static IIndex AddIndex([NotNull] ITable table,
		                              [NotNull] string fieldName,
		                              bool unique,
		                              [CanBeNull] string prefix)
		{
			return AddIndex(table, new[] { fieldName }, unique, prefix);
		}

		[NotNull]
		public static IIndex AddIndex([NotNull] ITable table,
		                              [NotNull] string fieldName1,
		                              [NotNull] string fieldName2, bool unique,
		                              [CanBeNull] string prefix)
		{
			return AddIndex(table, new[] { fieldName1, fieldName2 }, unique, prefix);
		}

		[NotNull]
		public static IIndex AddIndex([NotNull] ITable table,
		                              [NotNull] string[] fieldNames,
		                              bool unique,
		                              [CanBeNull] string prefix)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));
			Assert.ArgumentCondition(fieldNames.Length > 0, "empty field names array");

			IFieldsEdit fields = new FieldsClass();
			foreach (string fieldName in fieldNames)
			{
				int fieldIndex = table.FindField(fieldName);
				if (fieldIndex < 0)
				{
					throw new ArgumentException("Field not found: " + fieldName);
				}

				fields.AddField(table.Fields.Field[fieldIndex]);
			}

			return AddIndex(table, fields, unique, prefix);
		}

		[NotNull]
		public static IIndex AddIndex([NotNull] ITable table,
		                              [NotNull] IFields fields,
		                              bool unique,
		                              [CanBeNull] string prefix)
		{
			IIndex index = CreateIndex(table, fields, unique, prefix);

			table.AddIndex(index); // NOTE: fails if index exists on *another* table

			return index;
		}

		[NotNull]
		public static IIndex CreateIndex([NotNull] ITable table,
		                                 [NotNull] IFields fields,
		                                 bool unique,
		                                 [CanBeNull] string prefix)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fields, nameof(fields));

			IIndexEdit index = new IndexClass();

			index.Fields_2 = fields;
			index.Name_2 = GetNewIndexName(table, prefix);
			index.IsUnique_2 = unique;

			return index;
		}

		public static void CreateSpatialIndex(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IFeatureClass template)
		{
			int shapeIdx = string.IsNullOrEmpty(featureClass.ShapeFieldName)
				               ? -1 // no SHAPE field
				               : template.FindField(featureClass.ShapeFieldName);

			IField shapeField = featureClass.Fields.Field[shapeIdx];

			IGeometryDef geometryDef = shapeField.GeometryDef;

			int gridCount = geometryDef.GridCount;

			double gridSize1 = gridCount > 0 ? geometryDef.GridSize[0] : 0;
			double gridSize2 = gridCount > 1 ? geometryDef.GridSize[1] : 0;
			double gridSize3 = gridCount > 2 ? geometryDef.GridSize[2] : 0;

			CreateSpatialIndex(featureClass, gridSize1, gridSize2, gridSize3);
		}

		public static void CreateSpatialIndex([NotNull] IFeatureClass featureClass,
		                                      double gridSize1 = 0,
		                                      double gridSize2 = 0,
		                                      double gridSize3 = 0)
		{
			Stopwatch watch = _msg.DebugStartTiming(
				"Creating spatial index with grid sizes {0}, {1}, {2}...",
				gridSize1, gridSize2, gridSize3);

			((IFeatureClassSpatialIndex) featureClass).RecreateSpatialIndex(
				"SpatialIndex", gridSize1, gridSize2, gridSize3);

			_msg.DebugStopTiming(watch, "Created spatial index");
		}

		[NotNull]
		public static IEnumerable<IIndex> GetIndexes([NotNull] IObjectClass objectClass)
		{
			return GetIndexes((ITable) objectClass);
		}

		public static IEnumerable<IIndex> GetIndexes([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			IIndexes indexes = table.Indexes;
			int indexCount = indexes.IndexCount;

			for (var i = 0; i < indexCount; i++)
			{
				yield return indexes.Index[i];
			}
		}

		public static bool HasSubtypes([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return subtypes != null && subtypes.HasSubtype;
		}

		/// <summary>
		/// Gets the subtypes defined for the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>list of subtypes.</returns>
		[NotNull]
		public static IList<Subtype> GetSubtypes([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			return GetSubtypes(objectClass as ISubtypes);
		}

		[NotNull]
		public static IList<Subtype> GetSubtypes([CanBeNull] ISubtypes subtypes)
		{
			var result = new List<Subtype>();

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return result;
			}

			IEnumSubtype enumSubtype = subtypes.Subtypes;
			int subtypeCode;
			string subtypeName = enumSubtype.Next(out subtypeCode);
			while (subtypeName != null)
			{
				result.Add(new Subtype(subtypeCode, subtypeName));

				subtypeName = enumSubtype.Next(out subtypeCode);
			}

			// The enumerator returns subtypes in the order that they are defined.
			// Sort on the subtype code.
			result.Sort(CompareSubtypes);

			return result;
		}

		/// <summary>
		/// Gets a dictionary of subtype names by code for the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>Dictionary of subtype names by code.</returns>
		[NotNull]
		public static IDictionary<int, string> GetSubtypeNamesByCode(
			[NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return GetSubtypeNamesByCode(subtypes);
		}

		public static IDictionary<int, string> GetSubtypeNamesByCode(
			[CanBeNull] ISubtypes subtypes)
		{
			var result = new Dictionary<int, string>();

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return result;
			}

			IEnumSubtype enumSubtype = subtypes.Subtypes;
			int subtypeCode;
			string subtypeName = enumSubtype.Next(out subtypeCode);

			while (subtypeName != null)
			{
				result.Add(subtypeCode, subtypeName);

				subtypeName = enumSubtype.Next(out subtypeCode);
			}

			return result;
		}

		/// <summary>
		/// Gets a dictionary of subtypes by code for the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>Dictionary of subtypes by code.</returns>
		[NotNull]
		public static IDictionary<int, Subtype> GetSubtypesByCode(
			[NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var result = new Dictionary<int, Subtype>();

			var subtypes = objectClass as ISubtypes;

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return result;
			}

			IEnumSubtype enumSubtype = subtypes.Subtypes;
			int subtypeCode;
			string subtypeName = enumSubtype.Next(out subtypeCode);

			while (subtypeName != null)
			{
				result.Add(subtypeCode, new Subtype(subtypeCode, subtypeName));

				subtypeName = enumSubtype.Next(out subtypeCode);
			}

			return result;
		}

		/// <summary>
		/// Gets the subtype code for the name from object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="name">The name.</param>
		/// <returns>the subtype code; -1 if the subtype is not found</returns>
		public static int GetSubtypeCode([NotNull] IObjectClass objectClass,
		                                 [NotNull] string name)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			var subtypes = objectClass as ISubtypes;
			if (subtypes != null && subtypes.HasSubtype)
			{
				IEnumSubtype enumSubtype = subtypes.Subtypes;

				int subtypeCode;
				string subtypeName = enumSubtype.Next(out subtypeCode);

				while (subtypeName != null)
				{
					if (subtypeName.Equals(name, StringComparison.OrdinalIgnoreCase))
					{
						return subtypeCode;
					}

					subtypeName = enumSubtype.Next(out subtypeCode);
				}
			}

			return -1;
		}

		public static bool AreSameObjectClass(IEnumerable<IObjectClass> objectClasses)
		{
			IObjectClass first = null;
			foreach (IObjectClass objectClass in objectClasses)
			{
				if (first == null)
				{
					first = objectClass;
				}
				else
				{
					if (! IsSameObjectClass(first, objectClass))
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Determine if two object classes are the same in the sense
		/// that they refer to the same database table, ignoring versions.
		/// </summary>
		/// <param name="class1">The first object class.</param>
		/// <param name="class2">The other object class.</param>
		/// <returns>
		/// true if the two object classes are the same; otherwise, false.
		/// </returns>
		public static bool IsSameObjectClass([NotNull] IObjectClass class1,
		                                     [NotNull] IObjectClass class2)
		{
			Assert.ArgumentNotNull(class1, nameof(class1));
			Assert.ArgumentNotNull(class2, nameof(class2));

			// Test for reference-equals in real ArcObjects object class instances but also allow
			// synthetic and mock feature classes to provide their own equality implementation:
			if (class1.Equals(class2))
			{
				return true;
			}

			bool class1IsRelQuery = class1 is IRelQueryTable;
			bool class2IsRelQuery = class2 is IRelQueryTable;

			if (class1IsRelQuery != class2IsRelQuery)
			{
				return false;
			}

			if (class1IsRelQuery)
			{
				var relQueryTable1 = (IRelQueryTable) class1;
				var relQueryTable2 = (IRelQueryTable) class2;
				return IsSameObjectClass((IObjectClass) relQueryTable1.DestinationTable,
				                         (IObjectClass) relQueryTable2.DestinationTable) &&
				       IsSameObjectClass((IObjectClass) relQueryTable1.SourceTable,
				                         (IObjectClass) relQueryTable2.SourceTable);
			}

			if (class1.ObjectClassID != class2.ObjectClassID)
			{
				return false;
			}

			var dataset1 = (IDataset) class1;
			var dataset2 = (IDataset) class2;

			if (dataset1.FullName is IDatasetName2 dsName1 &&
			    dataset2.FullName is IDatasetName2 dsName2)
			{
				if (! dsName1.Name.Equals(dsName2.Name))
				{
					return false;
				}
			}
			else
			{
				if (! dataset1.Name.Equals(dataset2.Name))
				{
					return false;
				}
			}

			// class id and names are equal; could still be different db instances

			return WorkspaceUtils.IsSameDatabase(dataset1.Workspace, dataset2.Workspace);
		}

		public static bool HasZ([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			IGeometryDef geometryDef = GetGeometryDef(featureClass);

			return geometryDef.HasZ;
		}

		public static bool HasM([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			IGeometryDef geometryDef = GetGeometryDef(featureClass);

			return geometryDef.HasM;
		}

		/// <summary>
		/// Gets a copy of the GeometryDef of a feature class. Use this overload if the
		/// resulting GeometryDef is going to be modified.
		/// </summary>
		/// <param name="featureClass"></param>
		/// <returns></returns>
		[NotNull]
		public static IGeometryDef GetGeometryDefCopy([NotNull] IFeatureClass featureClass)
		{
			return (IGeometryDef) ((IClone) GetGeometryDef(featureClass)).Clone();
		}

		/// <summary>
		/// Gets the GeometryDef of a feature class. The resulting GeometryDef must never be
		/// changed in order to avoid side effects when storing geometries in the featureClass!
		/// </summary>
		/// <param name="featureClass"></param>
		/// <returns></returns>
		[NotNull]
		public static IGeometryDef GetGeometryDef([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			return GetGeometryDef(featureClass.Fields, featureClass.ShapeFieldName,
			                      () => GetName(featureClass));
		}

		public static IGeometryDef GetGeometryDef([NotNull] IReadOnlyFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			return GetGeometryDef(featureClass.Fields, featureClass.ShapeFieldName,
			                      () => featureClass.Name);
		}

		[NotNull]
		private static IGeometryDef GetGeometryDef([NotNull] IFields fields,
		                                           [NotNull] string shapeFieldName,
		                                           Func<string> getDatasetName)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));
			Assert.ArgumentNotNull(shapeFieldName, nameof(shapeFieldName));

			int shapeIndex = fields.FindField(shapeFieldName);

			// TODO: test with QueryFeatureClass etc.
			Assert.True(shapeIndex >= 0,
			            "Shape field not found in FeatureClass {0}.",
			            getDatasetName());

			IGeometryDef geometryDef = fields.Field[shapeIndex].GeometryDef;

			return geometryDef;
		}

		/// <summary>
		/// Gets the GeometryDef of a feature. The resulting GeometryDef must never be
		/// changed in order to avoid side effects when storing geometries in the source
		/// featureClass!
		/// </summary>
		/// <param name="feature"></param>
		/// <returns></returns>
		[NotNull]
		public static IGeometryDef GetGeometryDef([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetGeometryDef((IFeatureClass) feature.Class);
		}

		/// <summary>
		/// Gets the name of the dataset.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns>fully qualified name ({database.}owner.table) of the dataset.</returns>
		[NotNull]
		public static string GetName([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return dataset.Name;
		}

		/// <summary>
		/// Gets the name of the object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>fully qualified name ({database.}owner.table) of the object class.</returns>
		[NotNull]
		public static string GetName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return GetName((IDataset) objectClass);
		}

		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>fully qualified name ({database.}owner.table) of the table.</returns>
		[NotNull]
		public static string GetName([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetName((IDataset) table);
		}

		/// <summary>
		/// Gets the name of the relationship class.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>fully qualified name ({database.}owner.table) of the relationship class.</returns>
		[NotNull]
		public static string GetName([NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			return GetName((IDataset) relationshipClass);
		}

		[NotNull]
		public static string GetAliasName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			try
			{
				string aliasName = objectClass.AliasName;

				return StringUtils.IsNotEmpty(aliasName)
					       ? aliasName
					       : GetName(objectClass);
			}
			catch (NotImplementedException)
			{
				return GetName(objectClass);
			}
		}

		[NotNull]
		public static string GetTableDisplayName([NotNull] IObjectClass objectClass)
		{
			string className = GetName(objectClass);

			string aliasName = GetAliasName(objectClass);

			if (className.Equals(aliasName, StringComparison.CurrentCultureIgnoreCase))
			{
				// the alias name is equal to the name (but may have different case)
				// unqualify the alias name to preserve it's case.
				IWorkspace workspace = ((IDataset) objectClass).Workspace;

				return GetTableName(workspace, aliasName);
			}

			// the alias name is different from the class name. Use it.
			return aliasName;
		}

		/// <summary>
		/// Gets the relationship classes for an object class (given esriRelRole) as a list.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="esriRelRole">The esriRelRole</param>
		/// <returns>Rolebased list of relationship classes for the object class.</returns>
		[NotNull]
		public static IList<IRelationshipClass> GetRelationshipClasses(
			[NotNull] IObjectClass objectClass,
			esriRelRole esriRelRole = esriRelRole.esriRelRoleAny)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var list = new List<IRelationshipClass>();

			IEnumRelationshipClass enumRelClass =
				objectClass.RelationshipClasses[esriRelRole];

			enumRelClass.Reset();

			IRelationshipClass relClass;
			while ((relClass = enumRelClass.Next()) != null)
			{
				list.Add(relClass);
			}

			return list;
		}

		[NotNull]
		public static IWorkspace GetWorkspace([NotNull] ITable table)
		{
			return GetWorkspace((IDataset) table);
		}

		[NotNull]
		public static IWorkspace GetWorkspace([NotNull] IObjectClass objectClass)
		{
			return GetWorkspace((IDataset) objectClass);
		}

		[NotNull]
		public static IWorkspace GetWorkspace([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			// Null in case of image server layers
			var workspace = dataset.Workspace;
			return Assert.NotNull(workspace, "Workspace of {0} is null", GetName(dataset));
		}

		[NotNull]
		public static IName GetDatasetName([NotNull] IObjectClass objectClass)
		{
			return GetDatasetName((IDataset) objectClass);
		}

		[NotNull]
		public static IName GetDatasetName([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			// NotImplementedException in case of image server layers
			return dataset.FullName;
		}

		[NotNull]
		public static IDatasetName GetDatasetName(
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			return (IDatasetName) ((IDataset) relationshipClass).FullName;
		}

		/// <summary>
		/// Gets the name of the feature dataset that contains the dataset indicated by the specified dataset name.
		/// </summary>
		/// <param name="datasetName">Dataset name of the dataset for which the dataset name of the containing feature dataset is to be returned.</param>
		/// <returns>The dataset name of the feature dataset that contains the dataset, 
		/// or null if the dataset is not contained in a feature dataset.</returns>
		[CanBeNull]
		public static IDatasetName GetFeatureDatasetName([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var featureClassName = datasetName as IFeatureClassName;
			if (featureClassName != null)
			{
				return featureClassName.FeatureDatasetName;
			}

			var tableName = datasetName as ITableName;
			if (tableName != null)
			{
				return null;
			}

			var relationshipClassName = datasetName as IRelationshipClassName;
			if (relationshipClassName != null)
			{
				return relationshipClassName.FeatureDatasetName;
			}

#if !Server
			var topologyName = datasetName as ITopologyName;
			if (topologyName != null)
			{
				return topologyName.FeatureDatasetName;
			}

			var geometricNetworkName = datasetName as IGeometricNetworkName;
			if (geometricNetworkName != null)
			{
				return geometricNetworkName.FeatureDatasetName;
			}

			var terrainName = datasetName as ITerrainName;
			if (terrainName != null)
			{
				return terrainName.FeatureDatasetName;
			}

			var networkDatasetName = datasetName as INetworkDatasetName;
			if (networkDatasetName != null)
			{
				return networkDatasetName.FeatureDatasetName;
			}

			var fabricName = datasetName as ICadastralFabricName;
			if (fabricName != null)
			{
				return fabricName.FeatureDatasetName;
			}

#endif
			// other dataset name, assume not in a feature dataset
			return null;
		}

		/// <summary>
		/// Determines whether a given object class is based on a query. 
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>
		/// 	<c>true</c> if the specified object class is based on a query; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>An object class is considered to be query-based if it is based on a name object that
		/// implements <see cref="IQueryName"/> or if the object class itself implements <see cref="IRelQueryTable"/></remarks>
		public static bool IsQueryBasedClass([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return objectClass is IRelQueryTable || IsQueryNameBasedClass(objectClass);
		}

		/// <summary>
		/// Determines whether a given object class is based on a QueryDef-based dataset name.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>
		/// 	<c>true</c> if the object class is based on a QueryDef-based dataset name; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsQueryNameBasedClass([NotNull] IObjectClass objectClass)
		{
			return GetQueryName(objectClass) != null;
		}

		[CanBeNull]
		public static IQueryName GetQueryName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var dataset = (IDataset) objectClass;

			try
			{
				return dataset.FullName as IQueryName;
			}
			catch (NotImplementedException)
			{
				// not implemented for image service layers --> no query name
				return null;
			}
		}

		/// <summary>
		/// Determines whether the specified dataset is owned by connected user.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns>
		/// 	<c>true</c> if the dataset is owned by the connected user; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsOwnedByConnectedUser([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IWorkspace workspace = dataset.Workspace;

			if (workspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				return true;
			}

			string connectedUser = ((IDatabaseConnectionInfo2) workspace).ConnectedUser;

			string ownerName = GetOwnerName(workspace, dataset.Name);

			return string.Equals(connectedUser, ownerName,
			                     StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Determine if two object classes have the same GDB version.
		/// </summary>
		/// <param name="class1">The first object class.</param>
		/// <param name="class2">The other object class.</param>
		/// <returns>
		/// True if both object classes have the same GDB version; otherwise, false.
		/// </returns>
		public static bool IsSameVersion([NotNull] IObjectClass class1,
		                                 [NotNull] IObjectClass class2)
		{
			if (class1 == class2)
			{
				return true; // same object class instance => same version
			}

			IWorkspace workspace1 = GetWorkspace(class1);
			IWorkspace workspace2 = GetWorkspace(class2);

			return WorkspaceUtils.IsSameVersion(workspace1, workspace2);
		}

		/// <summary>
		/// Gets the display value for a given subtype value
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="subtypeValue">The subtype value.</param>
		/// <returns></returns>
		[CanBeNull]
		public static object GetDisplayValue([NotNull] IObjectClass objectClass,
		                                     int subtypeValue)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			int subtypeFieldIndex = GetSubtypeFieldIndex(objectClass);

			Assert.True(subtypeFieldIndex >= 0, "Object class has no subtypes");

			return GetDisplayValue(objectClass, subtypeFieldIndex,
			                       subtypeValue, subtypeValue);
		}

		/// <summary>
		/// Gets the display value for a given field value
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="fieldValue">The field value.</param>
		/// <param name="subtypeValue">The subtype value.</param>
		/// <returns></returns>
		[CanBeNull]
		public static object GetDisplayValue([NotNull] IObjectClass objectClass,
		                                     int fieldIndex,
		                                     [CanBeNull] object fieldValue,
		                                     [CanBeNull] int? subtypeValue)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			IField field = objectClass.Fields.Field[fieldIndex];
			var subtypes = objectClass as ISubtypes;

			if (fieldValue == null || fieldValue is DBNull)
			{
				return null;
			}

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				var fieldCodedValueDomain = field.Domain as ICodedValueDomain;

				return fieldCodedValueDomain != null
					       ? DomainUtils.GetCodedValueName(fieldCodedValueDomain, fieldValue)
					       : fieldValue;
			}

			if (subtypes.SubtypeFieldIndex == fieldIndex)
			{
				// This is the subtype field. Get name for subtype value
				return GetSubtypeName(objectClass, subtypes, fieldValue);
			}

			// get the field domain for the current subtype value
			IDomain domain;
			if (! subtypeValue.HasValue)
			{
				_msg.Debug("Subtype is null, using default domain for field");
				domain = field.Domain;
			}
			else
			{
				int subtypeCode = subtypeValue.Value;
				try
				{
					domain = subtypes.Domain[subtypeCode, field.Name];
				}
				catch (Exception e)
				{
					_msg.Debug(
						string.Format("Error getting domain of subtype {0}; " +
						              "using default domain for field",
						              fieldValue), e);
					domain = field.Domain;
				}
			}

			var codedValueDomain = domain as ICodedValueDomain;
			if (codedValueDomain != null)
			{
				return DomainUtils.GetCodedValueName(codedValueDomain, fieldValue);
			}

			// TODO: Remove that block, after the CONFLICT_ROLE / CONFLICT_ID /
			// and the two INTEGRATION_* attributes are stored with the
			// right domain for every subtype
			var codedValueDomainNoSub = field.Domain as ICodedValueDomain;

			object result = codedValueDomainNoSub != null
				                ? DomainUtils.GetCodedValueName(codedValueDomainNoSub, fieldValue)
				                : fieldValue;
			// Block end

			// TODO: Uncomment when removing the block before...
			//result = value;

			return result;
		}

		/// <summary>
		/// Returns the index of the field in <paramref name="fields"/> matching the 
		/// <paramref name="field"/> or -1 if no match was found.
		/// </summary>
		/// <param name="field">The field to match</param>
		/// <param name="fields">The fields to search</param>
		/// <param name="fieldComparison">Options to identify matching fields (e.g. only by name)</param>
		/// <returns></returns>
		public static int FindMatchingFieldIndex([NotNull] IField field,
		                                         [NotNull] IFields fields,
		                                         FieldComparison fieldComparison)
		{
			return FindMatchingFieldIndex(field, string.Empty, fields, fieldComparison);
		}

		/// <summary>
		/// Returns the index of the field in <paramref name="fields"/> matching the 
		/// <paramref name="field"/> or -1 if no match was found.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="tableName"></param>
		/// <param name="fields"></param>
		/// <param name="fieldComparison">Options to identify matching fields. Note that if FieldNameDomainName
		/// is used only the main domain of the field will be compared. Domains varying by subtype are ignored.
		/// Domain names are also ignored when a <paramref name="tableName"/> is provided.</param>
		/// <returns></returns>
		public static int FindMatchingFieldIndex(
			[NotNull] IField field,
			[CanBeNull] string tableName,
			[NotNull] IFields fields,
			FieldComparison fieldComparison = FieldComparison.FieldNameDomainName)
		{
			// TODO: to properly compare Domain names / or even allowed values the signature must be changed
			//       -> Class is needed to access subtypes.
			Assert.ArgumentNotNull(field, nameof(field));
			Assert.ArgumentNotNull(fields, nameof(fields));

			esriFieldType fieldType = field.Type;

			string targetFieldName;
			if (string.IsNullOrEmpty(tableName))
			{
				targetFieldName = field.Name;
			}
			else
			{
				targetFieldName = tableName + "." + field.Name;
				if (fieldType == esriFieldType.esriFieldTypeOID)
				{
					fieldType = esriFieldType.esriFieldTypeInteger;
					// TODO: but: joined field can be of type OID also since 10.4 --> no match (TOP-
				}
			}

			int fieldCount = fields.FieldCount;
			for (var index = 0; index < fieldCount; index++)
			{
				IField sourceField = fields.Field[index];

				if (! targetFieldName.Equals(sourceField.Name,
				                             StringComparison.OrdinalIgnoreCase))
				{
					// names don't match -> no match
					continue;
				}

				if (fieldType != sourceField.Type)
				{
					if (string.IsNullOrEmpty(tableName))
					{
						// no join, types don't match --> no match
						continue;
					}

					if (sourceField.Type == esriFieldType.esriFieldTypeOID &&
					    fieldType == esriFieldType.esriFieldTypeInteger)
					{
						// consider as match
						// as of 10.4, the joined fields may be of type OID
					}
					else
					{
						continue;
					}

					if (fieldType != sourceField.Type)
					{
						if (string.IsNullOrEmpty(tableName))
						{
							// no join, types don't match --> no match
							continue;
						}

						if (sourceField.Type == esriFieldType.esriFieldTypeOID &&
						    fieldType == esriFieldType.esriFieldTypeInteger)
						{
							// consider as match
							// as of 10.4, the joined fields may be of type OID
						}
						else
						{
							continue;
						}
					}
				}

				// TODO: review this - are there no domains in joined case?
				if (! string.IsNullOrEmpty(tableName))
				{
					return index;
				}

				if (fieldComparison == FieldComparison.FieldName)
				{
					return index;
				}

				if (field.Domain == null && sourceField.Domain == null)
				{
					return index;
				}

				if (field.Domain != null && sourceField.Domain != null &&
				    field.Domain.Name.Equals(sourceField.Domain.Name,
				                             StringComparison.OrdinalIgnoreCase))
				{
					return index;
				}
			}

			return -1;
		}

		///// <summary>
		///// Does not support fields from joined layers
		///// </summary>
		///// <param name="matchField"></param>
		///// <param name="sourceTable"></param>
		///// <param name="searchTable"></param>
		///// <param name="fieldComparison"></param>
		///// <returns></returns>
		//public static int FindMatchingFieldIndex(IField matchField, ITable sourceTable, 
		//    ITable searchTable, FieldComparison fieldComparison)
		//{
		//    // start from scratch - keep the old methods for backward compatibility
		//    // and for the support of joined layers
		//    Assert.ArgumentNotNull(matchField, "matchField");
		//    Assert.ArgumentNotNull(searchTable, "searchTable");

		//    esriFieldType matchFieldType = matchField.Type;
		//    string matchFieldName = matchField.Name;

		//    for (int index = 0; index < searchTable.Fields.FieldCount; index++)
		//    {
		//        IField searchedField = searchTable.Fields.get_Field(index);

		//        if (matchFieldType == searchedField.Type &&
		//            searchedField.Name.Equals(matchFieldName))
		//        {
		//            if (fieldComparison == FieldComparison.FieldName)
		//            {
		//                return index;
		//            }

		//            ISubtypes sourceSubtypes = sourceTable as ISubtypes;
		//            ISubtypes searchSubtypes = searchTable as ISubtypes;
		//            if (matchField.Domain == null && searchedField.Domain == null &&
		//                )
		//        }
		//    }
		//    return -1;
		//}

		/// <summary>
		/// Searches the field index of the given field list (fields) for
		/// the given field.
		/// Match means: Same name, type and domain.
		/// </summary>
		/// <param name="field">Field that defines the matching parameters</param>
		/// <param name="fields">List of fields to search for a matching field</param>
		/// <returns>The index of the fields list matching the given field,
		/// if no matching field is found, -1 is returned.</returns>
		public static int FindMatchingFieldIndex([NotNull] IField field,
		                                         [NotNull] IFields fields)
		{
			return FindMatchingFieldIndex(field, "", fields);
		}

		[CanBeNull]
		public static IWorkspace GetUniqueWorkspace<T>(
			[NotNull] IEnumerable<T> objectClasses)
			where T : class, IObjectClass
		{
			Assert.ArgumentNotNull(objectClasses, nameof(objectClasses));

			IWorkspace result = null;

			foreach (T objectClass in objectClasses)
			{
				if (objectClass == null)
				{
					continue;
				}

				IWorkspace workspace = GetWorkspace(objectClass);

				if (result == null)
				{
					result = workspace;
				}
				else
				{
					if (result != workspace)
					{
						throw new ArgumentException(
							string.Format(
								"Object class {0} is from a different workspace: {1} (expected: {2})",
								GetName(objectClass),
								WorkspaceUtils.GetConnectionString(workspace, true),
								WorkspaceUtils.GetConnectionString(result, true)));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Gets all relationship classes in a workspace (both top-level and within
		/// feature datasets)
		/// </summary>
		/// <param name="featureWorkspace"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IRelationshipClass> GetRelationshipClasses(
			[NotNull] IFeatureWorkspace featureWorkspace)
		{
			const bool noRecurse = false;
			return GetRelationshipClasses(featureWorkspace, noRecurse);
		}

		/// <summary>
		/// Gets all relationship classes in a workspace (both top-level and within
		/// feature datasets)
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		/// <param name="noRecurse">if set to <c>true</c> only root level relationship classes are returned.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IRelationshipClass> GetRelationshipClasses(
			[NotNull] IFeatureWorkspace featureWorkspace, bool noRecurse)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			const int initialCapacity = 200;

			var list = new List<IRelationshipClass>(initialCapacity);

			if (noRecurse)
			{
				// only root-level relationship classes
				IEnumDataset datasets =
					((IWorkspace) featureWorkspace).Datasets[
						esriDatasetType.esriDTRelationshipClass];

				datasets.Reset();
				var relClass = (IRelationshipClass) datasets.Next();
				while (relClass != null)
				{
					list.Add(relClass);
					relClass = (IRelationshipClass) datasets.Next();
				}
			}
			else
			{
				// all relationship classes in the workspace
				var datasetNames = new List<IDatasetName>(initialCapacity);
				datasetNames.AddRange(GetDatasetNames(featureWorkspace,
				                                      esriDatasetType.esriDTRelationshipClass));

				foreach (IDatasetName datasetName in datasetNames)
				{
					try
					{
						if (! UserHasReadAccess(datasetName))
						{
							continue;
						}

						IRelationshipClass relClass =
							featureWorkspace.OpenRelationshipClass(datasetName.Name);

						list.Add(relClass);
					}
					catch (COMException ex)
					{
						//TODO: Is there a general method to determine privileges
						//if (ex.ErrorCode == -2147220970 || ex.ErrorCode == -2147216100)
						switch (ex.ErrorCode)
						{
							// case 0x80040216:
							case -2147220970:
							case (int) fdoError.FDO_E_NO_PERMISSION:
								_msg.DebugFormat(
									"Relationship class {0} could not be opened.",
									datasetName.Name);
								break;
							default:
								throw;
						}
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Gets the relationship classes between two object classes
		/// </summary>
		/// <param name="objectClass1">The object class 1.</param>
		/// <param name="objectClass2">The object class 2.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IRelationshipClass> GetRelationshipClasses(
			[NotNull] IObjectClass objectClass1,
			[NotNull] IObjectClass objectClass2)
		{
			Assert.ArgumentNotNull(objectClass1, nameof(objectClass1));
			Assert.ArgumentNotNull(objectClass2, nameof(objectClass2));

			var result = new List<IRelationshipClass>();
			foreach (
				IRelationshipClass relationshipClass in GetRelationshipClasses(objectClass1))
			{
				if (relationshipClass.DestinationClass == objectClass2 ||
				    relationshipClass.OriginClass == objectClass2)
				{
					result.Add(relationshipClass);
				}
			}

			return result;
		}

		[NotNull]
		public static IRelationshipClass GetUniqueRelationshipClass(
			[NotNull] IObjectClass objectClass1,
			[NotNull] IObjectClass objectClass2)
		{
			IRelationshipClass
				result = FindUniqueRelationshipClass(objectClass1, objectClass2);

			Assert.NotNull(result,
			               "Exactly one relationship class expected between {0} and {1}",
			               GetName(objectClass1),
			               GetName(objectClass2));

			return result;
		}

		[CanBeNull]
		public static IRelationshipClass FindUniqueRelationshipClass(
			[NotNull] IObjectClass objectClass1,
			[NotNull] IObjectClass objectClass2)
		{
			IList<IRelationshipClass> relClasses =
				GetRelationshipClasses(objectClass1, objectClass2);

			if (relClasses.Count == 1)
			{
				return relClasses[0];
			}

			return null;
		}

		public static IDatasetName GetFeatureDatasetName([NotNull] ITable table)
		{
			return GetFeatureDatasetName((IDataset) table);
		}

		[CanBeNull]
		public static IDatasetName GetFeatureDatasetName([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IFeatureDataset featureDataset = GetFeatureDataset(dataset);

			return featureDataset == null
				       ? null
				       : (IDatasetName) GetDatasetName(featureDataset);
		}

		/// <summary>
		/// returns containing featureDataset for dataset. if none exists, returns null. if
		/// unsupported dataset, returns null.
		/// </summary>
		/// <param name="dataset">supports feature classes, terrains, topologies and relationship classes</param>
		/// <returns></returns>
		[CanBeNull]
		public static IFeatureDataset GetFeatureDataset([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (dataset is IFeatureClass featureClass)
			{
				return featureClass.FeatureDataset;
			}

#if !Server
			if (dataset is ITerrain terrain)
			{
				return terrain.FeatureDataset;
			}

			if (dataset is ITopology topology)
			{
				return topology.FeatureDataset;
			}
#endif

			if (dataset is IRelationshipClass relationshipClass)
			{
				return relationshipClass.FeatureDataset;
			}

			return null;
		}

		/// <summary>
		/// Recursively gets the source objectclass for a joined object class, or returns the
		/// object class itself if it is not joined.
		/// </summary>
		/// <param name="objectClass">The (potentially joined) object class.</param>
		/// <returns></returns>
		[NotNull]
		public static IObjectClass GetSourceObjectClass([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var relQueryTable = objectClass as IRelQueryTable;

			return relQueryTable == null
				       ? objectClass
				       : (IObjectClass) GetSourceTable(relQueryTable);
		}

		/// <summary>
		/// Recursively gets the source objectclass for a RelQueryTable.
		/// </summary>
		/// <param name="relQueryTable">The rel query table.</param>
		/// <returns></returns>
		[NotNull]
		public static IObjectClass GetSourceObjectClass(
			[NotNull] IRelQueryTable relQueryTable)
		{
			Assert.ArgumentNotNull(relQueryTable, nameof(relQueryTable));

			return (IObjectClass) GetSourceTable(relQueryTable);
		}

		/// <summary>
		/// Recursively gets the source table for a RelQueryTable.
		/// </summary>
		/// <param name="relQueryTable">The rel query table.</param>
		/// <returns></returns>
		[NotNull]
		public static ITable GetSourceTable([NotNull] IRelQueryTable relQueryTable)
		{
			Assert.ArgumentNotNull(relQueryTable, nameof(relQueryTable));

			ITable sourceTable = relQueryTable.SourceTable;
			Assert.NotNull(sourceTable, "source table is null");

			var sourceRelQueryTable = sourceTable as IRelQueryTable;

			return sourceRelQueryTable != null
				       ? GetSourceTable(sourceRelQueryTable)
				       : sourceTable;
		}

		/// <summary>
		/// Gets the joined object classes making up a relquery-based object class. Or the 
		/// object class itself if it is not reqquery-based. 
		/// </summary>
		/// <param name="objectClass">The objectclass.</param>
		/// <returns>List of IObjectClass, if no join return the objectClass self</returns>
		[NotNull]
		public static IList<IObjectClass> GetJoinedObjectClasses(
			[NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var result = new List<IObjectClass>();

			var relQueryTable = objectClass as IRelQueryTable;
			if (relQueryTable != null)
			{
				AppendJoinedObjectClasses(relQueryTable, result);
			}
			else
			{
				//add the only one objectclass (no join available)
				result.Add(objectClass);
			}

			return result;
		}

		/// <summary>
		/// Gets the named field or throw an exception if the there is no such field.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		[NotNull]
		public static IField GetField([NotNull] IObjectClass objectClass,
		                              [NotNull] string fieldName)
		{
			return GetField((ITable) objectClass, fieldName);
		}

		/// <summary>
		/// Gets the named field or throw an exception if the there is no such field.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		[NotNull]
		public static IField GetField([NotNull] ITable table,
		                              [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			IField field = table.Fields.Field[GetFieldIndex(table, fieldName)];

			return Assert.NotNull(field, "field '{0}' not found in '{1}'",
			                      fieldName, GetName(table));
		}

		[NotNull]
		public static IField GetField([NotNull] IReadOnlyTable table,
		                              [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			IField field = table.Fields.Field[GetFieldIndex(table, fieldName)];

			return Assert.NotNull(field, "field '{0}' not found in '{1}'",
			                      fieldName, table.Name);
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return GetFields((ITable) objectClass);
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetFields(table.Fields);
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="fields">The fields.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] IFields fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			int fieldCount = fields.FieldCount;

			var result = new List<IField>(fieldCount);

			result.AddRange(EnumFields(fields));

			return result;
		}

		[NotNull]
		public static IEnumerable<IField> EnumFields([NotNull] IFields fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			int fieldCount = fields.FieldCount;

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				IField field = fields.Field[fieldIndex];

				yield return field;
			}
		}

		/// <summary>
		/// Get the <see cref="esriFieldType"/> of the named field or throw an exception
		/// if there is no such field.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		public static esriFieldType GetFieldType([NotNull] IObjectClass objectClass,
		                                         [NotNull] string fieldName)
		{
			return GetFieldType((ITable) objectClass, fieldName);
		}

		/// <summary>
		/// Get the <see cref="esriFieldType"/> of the named field or throw an exception
		/// if there is no such field.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		public static esriFieldType GetFieldType([NotNull] ITable table,
		                                         [NotNull] string fieldName)
		{
			IField field = GetField(table, fieldName);
			return field.Type;
		}

		/// <summary>
		/// Get the index of the named field or throw an exception
		/// if there is no such field.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		public static int GetFieldIndex([NotNull] IObjectClass objectClass,
		                                [NotNull] string fieldName)
		{
			return GetFieldIndex((ITable) objectClass, fieldName);
		}

		/// <summary>
		/// Get the index of the named field or throw an exception
		/// if there is no such field.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">Field not found in table.</exception>
		public static int GetFieldIndex([NotNull] ITable table,
		                                [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIndex = table.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentException(
					string.Format("Field '{0}' not found in '{1}'", fieldName, GetName(table)),
					nameof(fieldName));
			}

			return fieldIndex;
		}

		public static int GetFieldIndex([NotNull] IReadOnlyTable table,
		                                [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIndex = table.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentException(
					string.Format("Field '{0}' not found in '{1}'", fieldName, table.Name),
					nameof(fieldName));
			}

			return fieldIndex;
		}

		/// <summary>
		/// Gets the index of the subtype field in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The index of the subtype field, or -1 
		/// if the table has no subtype field.</returns>
		public static int GetSubtypeFieldIndex([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var subtypes = table as ISubtypes;

			return subtypes != null && subtypes.HasSubtype
				       ? subtypes.SubtypeFieldIndex
				       : -1;
		}

		/// <summary>
		/// Gets the index of the subtype field in a given object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>The index of the subtype field, or -1 
		/// if the object class has no subtype field.</returns>
		public static int GetSubtypeFieldIndex([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return subtypes != null && subtypes.HasSubtype
				       ? subtypes.SubtypeFieldIndex
				       : -1;
		}

		/// <summary>
		/// Gets the name of the subtype field in a given object class.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>The name of the subtype field, or an empty string
		/// if the object class has no subtype field.</returns>
		[NotNull]
		public static string GetSubtypeFieldName([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var subtypes = objectClass as ISubtypes;

			return subtypes == null
				       ? string.Empty
				       : subtypes.SubtypeFieldName;
		}

		public static bool UserHasWriteAccess([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return UserHasWriteAccess((IDataset) table);
		}

		public static bool UserHasWriteAccess([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return UserHasWriteAccess((IDataset) objectClass);
		}

		/// <summary>
		/// Returns a value indicating if the user has read privileges for a given dataset name.
		/// </summary>
		/// <param name="datasetName">The dataset name.</param>
		/// <returns></returns>
		/// <remarks>For datasets not in an ArcSDE database, <c>true</c> is returned.</remarks>
		public static bool UserHasReadAccess([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var privileges = datasetName as ISQLPrivilege;

			if (privileges == null)
			{
				return true;
			}

			try
			{
				return (privileges.SQLPrivileges & (int) esriSQLPrivilege.esriSelectPrivilege) !=
				       0;
			}
			catch (COMException e)
			{
				switch (e.ErrorCode)
				{
					case (int) fdoError.FDO_E_SE_NO_PERMISSIONS:
					case (int) fdoError.FDO_E_NO_PERMISSION:
						return false;
				}

				throw;
			}
		}

		/// <summary>
		/// Returns a value indicating if the user has write privileges for a given dataset.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <returns></returns>
		/// <remarks>For datasets not in an ArcSDE database, <c>true</c> is returned.</remarks>
		public static bool UserHasWriteAccess([NotNull] IDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			var datasetName = (IDatasetName) dataset.FullName;

			return UserHasWriteAccess(datasetName);
		}

		/// <summary>
		/// Returns a value indicating if the user has write privileges for a given dataset name.
		/// </summary>
		/// <param name="datasetName">The dataset name.</param>
		/// <returns></returns>
		/// <remarks>For datasets not in an ArcSDE database, <c>true</c> is returned.</remarks>
		public static bool UserHasWriteAccess([NotNull] IDatasetName datasetName)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var privileges = datasetName as ISQLPrivilege;

			if (privileges == null)
			{
				return true;
			}

			const esriSQLPrivilege fullAccess = esriSQLPrivilege.esriSelectPrivilege |
			                                    esriSQLPrivilege.esriInsertPrivilege |
			                                    esriSQLPrivilege.esriUpdatePrivilege |
			                                    esriSQLPrivilege.esriDeletePrivilege;

			return privileges.SQLPrivileges == (int) fullAccess;
		}

		[NotNull]
		public static string GetConnectedUser([NotNull] ITable table)
		{
			return WorkspaceUtils.GetConnectedUser(GetWorkspace(table));
		}

		[NotNull]
		public static string GetConnectedUser([NotNull] IObjectClass objectClass)
		{
			return WorkspaceUtils.GetConnectedUser(GetWorkspace(objectClass));
		}

		public static bool TryGetXyTolerance([NotNull] IFeatureClass featureClass,
		                                     out double xyTolerance,
		                                     bool requireBigEnough = false)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			ISpatialReference spatialReference = ((IGeoDataset) featureClass).SpatialReference;
			return TryGetXyTolerance(spatialReference, out xyTolerance, requireBigEnough);
		}

		public static bool TryGetXyTolerance([NotNull] ISpatialReference spatialReference,
		                                     out double xyTolerance,
		                                     bool requireBigEnough = false)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var spatialReferenceTolerance = spatialReference as ISpatialReferenceTolerance;

			if (spatialReferenceTolerance != null &&
			    IsToleranceValid(spatialReferenceTolerance.XYToleranceValid,
			                     requireBigEnough))
			{
				xyTolerance = spatialReferenceTolerance.XYTolerance;
				return ! double.IsNaN(xyTolerance);
			}

			xyTolerance = double.NaN;
			return false;
		}

		public static double GetMaximumXyTolerance(
			[NotNull] IEnumerable<IFeatureClass> featureClasses)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			double result = 0;
			foreach (IFeatureClass featureClass in featureClasses)
			{
				double xyTolerance;
				if (TryGetXyTolerance(featureClass, out xyTolerance))
				{
					if (xyTolerance > result)
					{
						result = xyTolerance;
					}
				}
			}

			return result;
		}

		public static bool TryGetZTolerance([NotNull] IFeatureClass featureClass,
		                                    out double zTolerance,
		                                    bool requireBigEnough = false)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			ISpatialReference spatialReference = ((IGeoDataset) featureClass).SpatialReference;
			return TryGetZTolerance(spatialReference, out zTolerance, requireBigEnough);
		}

		public static bool TryGetZTolerance([CanBeNull] ISpatialReference spatialReference,
		                                    out double zTolerance,
		                                    bool requireBigEnough = false)
		{
			var spatialReferenceTolerance = spatialReference as ISpatialReferenceTolerance;

			if (spatialReferenceTolerance != null &&
			    IsToleranceValid(spatialReferenceTolerance.ZToleranceValid,
			                     requireBigEnough))
			{
				zTolerance = spatialReferenceTolerance.ZTolerance;
				return ! double.IsNaN(zTolerance);
			}

			zTolerance = double.NaN;
			return false;
		}

		public static bool TryGetMTolerance([NotNull] IFeatureClass s,
		                                    out double mTolerance,
		                                    bool requireBigEnough = false)
		{
			Assert.ArgumentNotNull(s, nameof(s));

			ISpatialReference spatialReference = ((IGeoDataset) s).SpatialReference;
			return TryGetMTolerance(spatialReference, out mTolerance, requireBigEnough);
		}

		public static bool TryGetMTolerance([NotNull] ISpatialReference spatialReference,
		                                    out double mTolerance,
		                                    bool requireBigEnough = false)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var spatialReferenceTolerance = spatialReference as ISpatialReferenceTolerance;

			if (spatialReferenceTolerance != null &&
			    IsToleranceValid(spatialReferenceTolerance.MToleranceValid,
			                     requireBigEnough))
			{
				mTolerance = spatialReferenceTolerance.MTolerance;
				return ! double.IsNaN(mTolerance);
			}

			mTolerance = double.NaN;
			return false;
		}

		[CanBeNull]
		public static ISpatialReference GetSpatialReference(
			[NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return ((IGeoDataset) feature.Class).SpatialReference;
		}

		[CanBeNull]
		public static ISpatialReference GetSpatialReference(
			[NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return ((IGeoDataset) featureClass).SpatialReference;
		}

		[CanBeNull]
		public static ISpatialReference GetSpatialReference(
			[NotNull] IFeatureDataset featureDataset)
		{
			Assert.ArgumentNotNull(featureDataset, nameof(featureDataset));

			return ((IGeoDataset) featureDataset).SpatialReference;
		}

		[CanBeNull]
		public static ISpatialReference GetUniqueSpatialReference<T>(
			[NotNull] IEnumerable<T> datasets)
		{
			return GetUniqueSpatialReference(datasets, SpatialReferenceUtils.AreEqual);
		}

		[CanBeNull]
		public static ISpatialReference GetUniqueSpatialReference<T>(
			[NotNull] IEnumerable<T> datasets,
			[NotNull] Func<ISpatialReference, ISpatialReference, bool> compareFunction)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));
			Assert.ArgumentNotNull(compareFunction, nameof(compareFunction));

			ISpatialReference result = null;
			string referenceDatasetName = null;

			foreach (T dataset in datasets)
			{
				var geoDataset = dataset as IGeoDataset;
				if (geoDataset == null)
				{
					continue;
				}

				ISpatialReference spatialReference = geoDataset.SpatialReference;

				string datasetName = dataset is IDataset ds ? ds.Name : "<unnamed>";

				if (result == null)
				{
					result = spatialReference;
					referenceDatasetName = datasetName;

					continue;
				}

				if (! compareFunction(result, spatialReference))
				{
					throw new InvalidOperationException(
						string.Format("Spatial references are not equal for {0} and {1}",
						              referenceDatasetName, datasetName));
				}
			}

			return result;
		}

		/// <summary>
		/// Deletes rows in a table based on a collection of object IDs.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="oids">The oids.</param>
		public static void DeleteRows([NotNull] ITable table,
		                              [NotNull] IEnumerable oids)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(oids, nameof(oids));

			const int maxLength = 2000;

			var sb = new StringBuilder();

			foreach (object oidObj in oids)
			{
				// Convert the (potentially boxed int) object:
				long oid = Convert.ToInt64(oidObj);

				if (sb.Length == 0)
				{
					sb.Append(oid);
				}
				else if (sb.Length < maxLength)
				{
					sb.AppendFormat(",{0}", oid);
				}
				else
				{
					// maximum exceeded, delete current oid list
					DeleteRowsByOIDString(table, sb.ToString());

					// clear string builder
					sb.Remove(0, sb.Length);
				}
			}

			if (sb.Length > 0)
			{
				DeleteRowsByOIDString(table, sb.ToString());
			}
		}

		public static bool IsBeingEdited([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var datasetEdit = objectClass as IDatasetEdit;

			// RelQueryTables, CAD datasets such as DGN do not implement IDatasetEdit
			return datasetEdit != null && datasetEdit.IsBeingEdited();
		}

		[CanBeNull]
		public static IField GetUniqueIntegerField([NotNull] IObjectClass objectClass,
		                                           bool requireUniqueIndex = true)
		{
			return GetUniqueIntegerField((ITable) objectClass, requireUniqueIndex);
		}

		[CanBeNull]
		public static IField GetUniqueIntegerField([NotNull] ITable table,
		                                           bool requireUniqueIndex = true)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			if (table.HasOID)
			{
				int oidFieldIndex = table.FindField(table.OIDFieldName);
				if (oidFieldIndex >= 0)
				{
					return table.Fields.Field[oidFieldIndex];
				}
			}

			// there is no OID field - try to find a suitable field:
			// - must be int32
			// - must be defined as 'not null'
			// - must have a unique index defined

			// get all unique single-field indexes
			List<IIndex> uniqueIndexes =
				requireUniqueIndex
					? GetIndexes(table).Where(index => index.IsUnique &&
					                                   index.Fields.FieldCount == 1)
					                   .ToList()
					: null;

			IList<IField> fields = GetFields(table);

			List<IField> candidates =
				fields.Where(f => f.Type == esriFieldType.esriFieldTypeInteger &&
				                  ! f.IsNullable &&
				                  (uniqueIndexes == null ||
				                   uniqueIndexes.Any(ix => ix.Fields.Field[0].Name == f.Name)))
				      .ToList();

			if (candidates.Count == 0)
			{
				// Try again without the not-null constraint, because fields in views are always nullable
				candidates = fields.Where(f => f.Type == esriFieldType.esriFieldTypeInteger &&
				                               (uniqueIndexes == null ||
				                                uniqueIndexes.Any(
					                                ix => ix.Fields.Field[0].Name == f.Name)))
				                   .ToList();

				_msg.DebugFormat("{0}: Candidates with Nullable fields have been included.",
				                 GetName(table));

				if (candidates.Count == 0)
				{
					return null;
				}
			}

			if (candidates.Count == 1)
			{
				return candidates[0];
			}

			_msg.DebugFormat(
				"Table {0}: Found {1} integer fields that could serve as Object ID fields. " +
				"Fields called OBJECTID, OID, FID or ID will take precedence.", GetName(table),
				candidates.Count);

			foreach (string preferredName in new[] { "OBJECTID", "OID", "FID", "ID" })
			{
				IField preferredField =
					candidates.FirstOrDefault(field => field.Name.Equals(preferredName,
						                          StringComparison.CurrentCultureIgnoreCase));

				if (preferredField != null)
				{
					return preferredField;
				}
			}

			// just use the first of the candidates
			return candidates[0];
		}

		#region Non-public methods

		/// <summary>
		/// Recursively appends the underlying joined object classes involved in a RelQueryTable to a list.
		/// </summary>
		/// <param name="relQueryTable">The RelQueryTable.</param>
		/// <param name="objectClassList">The list of joined objectclasses.</param>
		public static void AppendJoinedObjectClasses(
			[NotNull] IRelQueryTable relQueryTable,
			[NotNull] ICollection<IObjectClass> objectClassList)
		{
			Assert.ArgumentNotNull(relQueryTable, nameof(relQueryTable));
			Assert.ArgumentNotNull(objectClassList, nameof(objectClassList));

			ITable sourceTable = relQueryTable.SourceTable;
			ITable destinationTable = relQueryTable.DestinationTable;

			// See if the source and destination tables are RelQueryTables.
			var sourceRelQueryTable = sourceTable as IRelQueryTable;
			var destinationRelQueryTable = destinationTable as IRelQueryTable;

			if (sourceRelQueryTable != null)
			{
				// Call this method on the source table.
				AppendJoinedObjectClasses(sourceRelQueryTable, objectClassList);
			}
			else
			{
				objectClassList.Add((IObjectClass) sourceTable);
			}

			if (destinationRelQueryTable != null)
			{
				// Call this method on the destination table.
				AppendJoinedObjectClasses(destinationRelQueryTable, objectClassList);
			}
			else
			{
				objectClassList.Add((IObjectClass) destinationTable);
			}
		}

		[NotNull]
		internal static string GetSubtypeName([NotNull] IObjectClass objectClass,
		                                      [NotNull] ISubtypes subtypes,
		                                      [NotNull] object fieldValue)
		{
			try
			{
				int subtypeCode = GetSubtypeCode(fieldValue);

				return subtypes.SubtypeName[subtypeCode];
			}
			catch (Exception e)
			{
				// probably an illegal subtype
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("Error getting name of subtype {0} for class {1}: {2}",
					                 fieldValue, GetName(objectClass), e.Message);
				}

				return string.Format("<unknown subtype: {0}>", fieldValue);
			}
		}

		private static void LogFields([NotNull] IFields fields)
		{
			_msg.DebugFormat("Field count: {0}", fields.FieldCount);

			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var notUniqueNames = new List<string>();

			for (var i = 0; i < fields.FieldCount; i++)
			{
				IField field = fields.Field[i];

				_msg.DebugFormat("- {0}: {1}", field.Name, field.Type);

				if (! names.Add(field.Name))
				{
					notUniqueNames.Add(field.Name);
				}
			}

			if (notUniqueNames.Count > 0)
			{
				_msg.WarnFormat("The field names are not unique ({0})",
				                StringUtils.Concatenate(notUniqueNames, ", "));
			}
		}

		private static bool IsToleranceValid(esriSRToleranceEnum toleranceValidity,
		                                     bool requireBigEnough)
		{
			switch (toleranceValidity)
			{
				case esriSRToleranceEnum.esriSRToleranceIsNaN:
					return false;

				case esriSRToleranceEnum.esriSRToleranceIsTooSmall:
					return requireBigEnough
						       ? false
						       : true;

				case esriSRToleranceEnum.esriSRToleranceOK:
					return true;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(toleranceValidity), toleranceValidity,
						@"Unsupported tolerance validity value");
			}
		}

		private static int CompareSubtypes([CanBeNull] Subtype x,
		                                   [CanBeNull] Subtype y)
		{
			if (x == null)
			{
				if (y == null)
				{
					// If x is null and y is null, they're equal. 
					return 0;
				}

				// If x is null and y is not null, y is greater. 
				return -1;
			}

			// If x is not null...
			if (y == null)
				// ...and y is null, x is greater.
			{
				return 1;
			}

			// ...and y is not null, compare the subtypes by their code
			// lengths of the two strings.
			return x.Code.CompareTo(y.Code);
		}

		/// <summary>
		/// Gets the top-level datasets of specified types in a workspace 
		/// (does not recurse into feature datasets).
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="datasetTypes">The dataset types.</param>
		/// <returns></returns>
		[NotNull]
		private static IEnumerable<IDataset> GetDatasets(
			[NotNull] IWorkspace workspace,
			params esriDatasetType[] datasetTypes)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			foreach (esriDatasetType datasetType in datasetTypes)
			{
				IEnumDataset enumDataset = workspace.Datasets[datasetType];
				enumDataset.Reset();

				IDataset dataset = enumDataset.Next();
				while (dataset != null)
				{
					yield return dataset;

					dataset = enumDataset.Next();
				}

				enumDataset.Reset();
			}
		}

		private static bool MustBeInFeatureDataset(esriDatasetType datasetType)
		{
			switch (datasetType)
			{
				case esriDatasetType.esriDTGeometricNetwork:
				case esriDatasetType.esriDTTopology:
				case esriDatasetType.esriDTNetworkDataset:
				case esriDatasetType.esriDTTerrain:
				case esriDatasetType.esriDTCadastralFabric:
					return true;

				case esriDatasetType.esriDTFeatureDataset:
				case esriDatasetType.esriDTFeatureClass:
				case esriDatasetType.esriDTText:
				case esriDatasetType.esriDTTable:
				case esriDatasetType.esriDTRelationshipClass:
				case esriDatasetType.esriDTRasterDataset:
				case esriDatasetType.esriDTMosaicDataset:
				case esriDatasetType.esriDTRasterBand:
				case esriDatasetType.esriDTTin:
				case esriDatasetType.esriDTCadDrawing:
				case esriDatasetType.esriDTRasterCatalog:
				case esriDatasetType.esriDTToolbox:
				case esriDatasetType.esriDTTool:
				case esriDatasetType.esriDTRepresentationClass:
				case esriDatasetType.esriDTSchematicDataset:
				case esriDatasetType.esriDTLocator:
					return false;

				default:
					_msg.DebugFormat("Unknown dataset type: {0}", datasetType);
					return false;
			}
		}

		private static bool CanBeInFeatureDataset(esriDatasetType datasetType)
		{
			switch (datasetType)
			{
				case esriDatasetType.esriDTGeometricNetwork:
				case esriDatasetType.esriDTTopology:
				case esriDatasetType.esriDTNetworkDataset:
				case esriDatasetType.esriDTTerrain:
				case esriDatasetType.esriDTCadastralFabric:
				case esriDatasetType.esriDTFeatureClass:
				case esriDatasetType.esriDTRelationshipClass:
				case esriDatasetType.esriDTRepresentationClass:
					return true;

				case esriDatasetType.esriDTFeatureDataset:
				case esriDatasetType.esriDTTable:
				case esriDatasetType.esriDTRasterDataset:
				case esriDatasetType.esriDTMosaicDataset:
				case esriDatasetType.esriDTRasterBand:
				case esriDatasetType.esriDTText:
				case esriDatasetType.esriDTTin:
				case esriDatasetType.esriDTCadDrawing:
				case esriDatasetType.esriDTRasterCatalog:
				case esriDatasetType.esriDTToolbox:
				case esriDatasetType.esriDTTool:
				case esriDatasetType.esriDTSchematicDataset:
				case esriDatasetType.esriDTLocator:
					return false;

				default:
					_msg.DebugFormat("Unknown dataset type: {0}", datasetType);
					return false;
			}
		}

		/// <summary>
		/// Adds the dataset names from an enum to a list
		/// </summary>
		/// <param name="list">The list.</param>
		/// <param name="enumDatasetNames">The enum dataset names.</param>
		/// <param name="uniqueNames">The unique set of names returned from the enum.</param>
		/// <param name="owner">The dataset owner to return datasets for (optional). 
		/// If an owner is specified, it is expected to have no leading/trailing blanks.</param>
		/// <param name="sqlSyntax">The SQL syntax for parsing the dataset name.</param>
		private static void AddDatasetNames([NotNull] ICollection<IDatasetName> list,
		                                    [NotNull] IEnumDatasetName enumDatasetNames,
		                                    [NotNull] ICollection<string> uniqueNames,
		                                    [CanBeNull] string owner,
		                                    [CanBeNull] ISQLSyntax sqlSyntax)
		{
			Assert.ArgumentNotNull(list, nameof(list));
			Assert.ArgumentNotNull(enumDatasetNames, nameof(enumDatasetNames));
			Assert.ArgumentNotNull(uniqueNames, nameof(uniqueNames));

			enumDatasetNames.Reset();
			IDatasetName datasetName = enumDatasetNames.Next();

			while (datasetName != null)
			{
				string name = datasetName.Name;
				if (! uniqueNames.Contains(name))
				{
					uniqueNames.Add(name);

					if (string.IsNullOrEmpty(owner) ||
					    sqlSyntax == null ||
					    DatasetOwnerMatches(name, owner, sqlSyntax))
					{
						list.Add(datasetName);
					}
				}
				else
				{
					_msg.VerboseDebug(
						() => $"Dataset name returned more than once: {datasetName.Name}");
				}

				datasetName = enumDatasetNames.Next();
			}
		}

		private static bool DatasetOwnerMatches([NotNull] string fullName,
		                                        [NotNull] string ownerName,
		                                        [NotNull] ISQLSyntax sqlSyntax)
		{
			string datasetOwnerName;
			sqlSyntax.ParseTableName(fullName, out _, out datasetOwnerName, out _);

			if (string.IsNullOrEmpty(datasetOwnerName))
			{
				// "no dataset owner" matches also
				return true;
			}

			return string.Equals(ownerName, datasetOwnerName,
			                     StringComparison.OrdinalIgnoreCase);
		}

		[NotNull]
		private static string GetNewIndexName([NotNull] ITable table,
		                                      [CanBeNull] string prefix)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			string suffix = GetTableName(table);
			const string format = "{0}{1}_{2}";

			IIndexes indexes = table.Indexes;
			var indexSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			int indexCount = indexes.IndexCount;
			for (var i = 0; i < indexCount; i++)
			{
				string indexName = indexes.Index[i].Name.Trim();
				indexSet.Add(indexName);
			}

			for (var infix = 1;; infix++)
			{
				string indexName = string.Format(format, prefix, infix, suffix).Trim();

				// NOTE: indexes.FindIndex(indexName, out indexPos) throws E_FAIL
				if (! indexSet.Contains(indexName))
				{
					return indexName;
				}
			}
		}

		private static void RegisterAsVersioned([NotNull] IVersionedObject versionedObject)
		{
			Assert.ArgumentNotNull(versionedObject, nameof(versionedObject));

			if (! versionedObject.IsRegisteredAsVersioned)
			{
				versionedObject.RegisterAsVersioned(true);
			}
		}

		private static int GetSubtypeCode([NotNull] object subtypeFieldValue)
		{
			Assert.ArgumentNotNull(subtypeFieldValue, nameof(subtypeFieldValue));

			return subtypeFieldValue as int? ?? Convert.ToInt32(subtypeFieldValue);
		}

		[NotNull]
		private static UID GetAnnotationFeatureClassExtensionUID()
		{
#if Server
			return new UIDClass { Value = "{F245DFEB-851B-4981-9860-4BACC8C0A688}" };
			//return new UIDClass { Value = "esriCarto.AnnotationFeatureClassExtension" };
#else
			return new UIDClass { Value = "{24429589-D711-11D2-9F41-00C04F6BC6A5}" };
#endif
		}

		[NotNull]
		private static UID GetAnnotationFeatureUID()
		{
			return new UIDClass { Value = "{3FF1675E-4FFB-4D9B-9438-767CE04DE34A}" };
			//return new UIDClass {Value = "esriCarto.AnnotationFeature"};
		}

		[NotNull]
		private static UID GetFeatureUID()
		{
			return new UIDClass { Value = "{52353152-891A-11D0-BEC6-00805F7C4268}" };
			//return new UIDClass {Value = "esriGeoDatabase.Feature"};
		}

		[NotNull]
		private static UID GetObjectUID()
		{
			return new UIDClass { Value = "{7A566981-C114-11D2-8A28-006097AFF44E}" };
			//return new UIDClass {Value = "esriGeoDatabase.Object"};
		}

		[CanBeNull]
		private static IWorkspace TryOpenWorkspace([NotNull] IDatasetName datasetName,
		                                           bool openDefaultVersion)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			try
			{
				return WorkspaceUtils.OpenWorkspace(datasetName, openDefaultVersion);
			}
			catch (Exception e)
			{
				_msg.Debug(e.Message, e);
				return null;
			}
		}

		[CanBeNull]
		private static ITin OpenTin([NotNull] string directory, [NotNull] string tinName,
		                            bool fail)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(tinName, nameof(tinName));

			ITinWorkspace workspace = WorkspaceUtils.OpenTinWorkspace(directory);

			string tinDirectory = Path.Combine(directory, tinName);
			if (! Directory.Exists(tinDirectory))
			{
				if (fail)
				{
					Ex.Throw<DirectoryNotFoundException>("Directory not found: {0}", tinDirectory);
				}
				else
				{
					return null;
				}
			}

			if (! workspace.IsTin[tinName])
			{
				if (fail)
				{
					throw new ArgumentException("Not a valid Tin: " + tinDirectory);
				}

				return null;
			}

			try
			{
				return workspace.OpenTin(tinName);
			}
			catch
			{
				if (fail)
				{
					throw;
				}

				return null;
			}
		}

		/// <summary>
		/// Deletes rows in a table based on a string containing a comma-separated
		/// list of object ids.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="oidString">The oid string.</param>
		private static void DeleteRowsByOIDString([NotNull] ITable table,
		                                          [NotNull] string oidString)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(oidString, nameof(oidString));

			string whereClause = string.Format("{0} IN ({1})", table.OIDFieldName, oidString);

			IQueryFilter filter = new QueryFilterClass { WhereClause = whereClause };

			DeleteRowsByFilter(table, filter);
		}

		public static void DeleteRowsByFilter([NotNull] ITable table,
		                                      [NotNull] IQueryFilter filter)
		{
			Stopwatch watch = _msg.DebugStartTiming(
				"Deleting rows from {0} using where clause {1}",
				GetName(table), filter.WhereClause);

			// In some situations for (unregistered) PostGIS tables the Search() method changes the SubFields
			// to the full list of attributes in escaped quotations marks, which makes subsequent queries
			// using the same filter queries fail. -> One-time filters would be better!
			string subFieldsBefore = filter.SubFields;

			try
			{
				table.DeleteSearchedRows(filter);
			}
			catch (Exception e)
			{
				_msg.Debug($"Error deleting rows in {GetName(table)} using filter:", e);
				GdbQueryUtils.LogFilterProperties(filter);
				throw;
			}
			finally
			{
				if (subFieldsBefore != filter.SubFields)
				{
					filter.SubFields = subFieldsBefore;
				}
			}

			_msg.DebugStopTiming(watch, "Rows deleted");
		}

		private static bool IsGdbWorkspacePath(string catalogPath)
		{
			if (catalogPath.EndsWith(".sde",
			                         StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			if (catalogPath.EndsWith(".gdb",
			                         StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			if (catalogPath.EndsWith(".mdb",
			                         StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			return Directory.Exists(catalogPath);
		}

		private static void LogCreateFeatureClassParameters(IFeatureWorkspace workspace,
		                                                    string fclassName,
		                                                    IFields fields,
		                                                    string configKeyWord)
		{
			try
			{
				_msg.DebugFormat("Error creating feature class '{0}'", fclassName);
				_msg.DebugFormat("Workspace: {0}", WorkspaceUtils.GetConnectionString(
					                 (IWorkspace) workspace, true));
				_msg.DebugFormat("Config keyword: {0}", configKeyWord ?? "<null>");
				LogFields(fields);
			}
			catch (Exception e)
			{
				_msg.Debug("Error writing to log", e);
			}
		}

		#endregion
	}
}
