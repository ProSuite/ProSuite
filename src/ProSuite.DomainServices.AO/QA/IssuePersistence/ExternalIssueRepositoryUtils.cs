using System;
using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainServices.AO.QA.Issues;
using Path = System.IO.Path;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	public static class ExternalIssueRepositoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static bool IssueRepositoryExists(
			[NotNull] string directoryFullPath,
			[NotNull] string gdbName,
			IssueRepositoryType issueRepositoryType)
		{
			string fullPath = Path.Combine(directoryFullPath, gdbName);

			return IssueRepositoryExists(fullPath, issueRepositoryType);
		}

		public static bool IssueRepositoryExists([NotNull] string fullPath,
		                                         IssueRepositoryType issueRepositoryType)
		{
			switch (issueRepositoryType)
			{
				case IssueRepositoryType.None:
					return false;
				case IssueRepositoryType.FileGdb:
					return WorkspaceUtils.FileGdbWorkspaceExists(fullPath);
				case IssueRepositoryType.Shapefiles:
					return WorkspaceUtils.ShapefileWorkspaceExists(fullPath);
				default:
					throw new ArgumentOutOfRangeException(nameof(issueRepositoryType));
			}
		}

		[CanBeNull]
		public static IIssueRepository GetIssueRepository(
			[NotNull] string gdbFullPath,
			[CanBeNull] ISpatialReference spatialReference,
			IssueRepositoryType issueRepositoryType)
		{
			Assert.ArgumentNotNullOrEmpty(gdbFullPath, nameof(gdbFullPath));

			var watch = _msg.DebugStartTiming("Creating issue repository {0}...", gdbFullPath);

			string directoryPath = Path.GetDirectoryName(gdbFullPath);
			Assert.NotNull(directoryPath,
			               "Invalid full path to gdb (undefined directory): {0}",
			               gdbFullPath);

			string gdbName = Path.GetFileName(gdbFullPath);
			Assert.NotNull(gdbName, "Invalid full path to gdb (undefined file name): {0}",
			               gdbFullPath);

			IIssueRepository result = GetIssueRepository(directoryPath, gdbName, spatialReference,
			                                             issueRepositoryType);

			_msg.DebugStopTiming(watch, "Created issue repository");

			return result;
		}

		[CanBeNull]
		public static IIssueRepository GetIssueRepository(
			[NotNull] string directoryFullPath,
			[NotNull] string gdbName,
			[CanBeNull] ISpatialReference spatialReference,
			IssueRepositoryType issueRepositoryType,
			double gridSize1 = 0d,
			double gridSize2 = 0d,
			double gridSize3 = 0d,
			bool addExceptionFields = false)
		{
			if (string.IsNullOrEmpty(directoryFullPath))
			{
				return null;
			}

			IFeatureWorkspace workspace =
				CreateDatabase(directoryFullPath, gdbName, issueRepositoryType);

			if (workspace == null)
			{
				return null;
			}

			IIssueTableFieldManagement fields = GetFields(issueRepositoryType,
			                                              addExceptionFields);
			Assert.NotNull(fields, "No field definition for repository type {0}",
			               issueRepositoryType);

			var issueGdbCreator = new IssueGeodatabaseCreator(workspace,
			                                                  fields,
			                                                  spatialReference,
			                                                  gridSize1,
			                                                  gridSize2,
			                                                  gridSize3);

			return issueGdbCreator.GetIssueRepository();
		}

		[CanBeNull]
		public static IFeatureWorkspace CreateDatabase(
			[NotNull] string directoryFullPath,
			[NotNull] string gdbName,
			IssueRepositoryType issueRepositoryType)
		{
			return GetOrCreateDatabase(directoryFullPath, gdbName, issueRepositoryType,
			                           forceNew: true);
		}

		[CanBeNull]
		public static IFeatureWorkspace GetOrCreateDatabase(
			[NotNull] string directoryFullPath,
			[NotNull] string gdbName,
			IssueRepositoryType issueRepositoryType,
			bool forceNew = false)
		{
			if (issueRepositoryType != IssueRepositoryType.None &&
			    ! Directory.Exists(directoryFullPath))
			{
				Directory.CreateDirectory(directoryFullPath);
			}

			IWorkspaceName workspaceName;
			try
			{
				workspaceName = GetWorkspaceName(issueRepositoryType, directoryFullPath, gdbName,
				                                 checkExists: ! forceNew);
			}
			catch (COMException e)
			{
				_msg.Debug($"Error creating issue workspace {gdbName} in {directoryFullPath}.", e);
				throw new IOException(
					$"Error creating issue workspace {gdbName} in {directoryFullPath}. " +
					"Make sure the executing process has the appropriate privileges and the disk has free space.",
					e);
			}

			return workspaceName == null
				       ? null
				       : (IFeatureWorkspace) WorkspaceUtils.OpenWorkspace(workspaceName);
		}

		[CanBeNull]
		private static IIssueTableFieldManagement GetFields(
			IssueRepositoryType issueRepositoryType,
			bool addExceptionFields)
		{
			switch (issueRepositoryType)
			{
				case IssueRepositoryType.None:
					return null;

				case IssueRepositoryType.FileGdb:
					return IssueTableFieldsFactory.GetIssueTableFields(addExceptionFields);

				case IssueRepositoryType.Shapefiles:
					return IssueTableFieldsFactory.GetIssueTableFields(addExceptionFields,
						useDbfFieldNames: true);

				default:
					throw new ArgumentOutOfRangeException(nameof(issueRepositoryType));
			}
		}

		[CanBeNull]
		private static IWorkspaceName GetWorkspaceName(
			IssueRepositoryType issueRepositoryType,
			[NotNull] string directoryPath,
			[NotNull] string gdbName, bool checkExists)
		{
			if (checkExists)
			{
				string path = Path.Combine(directoryPath, gdbName);
				IWorkspace ws = null;
				switch (issueRepositoryType)
				{
					case IssueRepositoryType.None:
						return null;

					case IssueRepositoryType.FileGdb:
						if (WorkspaceUtils.FileGdbWorkspaceExists(path))
							ws = WorkspaceUtils.OpenFileGdbWorkspace(path);
						break;

					case IssueRepositoryType.Shapefiles:
						if (WorkspaceUtils.ShapefileWorkspaceExists(path))
							ws = (IWorkspace) WorkspaceUtils.OpenShapefileWorkspace(path);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(issueRepositoryType));
				}

				if (ws != null)
				{
					return (IWorkspaceName) ((IDataset) ws).FullName;
				}
			}

			switch (issueRepositoryType)
			{
				case IssueRepositoryType.None:
					return null;

				case IssueRepositoryType.FileGdb:
					return WorkspaceUtils.CreateFileGdbWorkspace(directoryPath, gdbName);

				case IssueRepositoryType.Shapefiles:
					return WorkspaceUtils.CreateShapefileWorkspace(directoryPath, gdbName);

				default:
					throw new ArgumentOutOfRangeException(nameof(issueRepositoryType));
			}
		}
	}
}
