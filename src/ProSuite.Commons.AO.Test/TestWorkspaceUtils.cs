using System;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Test
{
	public static class TestWorkspaceUtils
	{
		[NotNull]
		public static IFeatureWorkspace CreateTestFgdbWorkspace([NotNull] string gdbName)
		{
			string dir = Path.GetTempPath();

			string gdb = Path.Combine(dir, gdbName) + ".gdb";

			if (Directory.Exists(gdb))
			{
				Directory.Delete(gdb, true);
			}

			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(dir, gdbName);

			return (IFeatureWorkspace)((IName)wsName).Open();
		}

		[NotNull]
		public static IFeatureWorkspace CreateTestAccessWorkspace(
			[NotNull] string mdbName)
		{
			if (Environment.Is64BitProcess) throw new InconclusiveException("AccessDB not supported for 64Bit-Process");

			string dir = Path.GetTempPath();

			string mdb = Path.Combine(dir, mdbName) + ".mdb";

			if (File.Exists(mdb))
			{
				File.Delete(mdb);
			}

			IFeatureWorkspace testWs = WorkspaceUtils.CreatePgdbWorkspace(dir, mdbName);
			return testWs;
		}

		[NotNull]
		public static IFeatureWorkspace CreateInMemoryWorkspace([NotNull] string name)
		{
			IWorkspaceName wsName = WorkspaceUtils.CreateInMemoryWorkspace(name);

			return (IFeatureWorkspace)((IName)wsName).Open();
		}

		[NotNull]
		public static IFeatureWorkspace CreateTestShapefileWorkspace(
			[NotNull] string gdbName)
		{
			string dir = Path.GetTempPath();

			string folder = Path.Combine(dir, gdbName);

			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, true);
			}

			IWorkspaceName wsName = WorkspaceUtils.CreateShapefileWorkspace(dir, gdbName);

			return (IFeatureWorkspace)((IName)wsName).Open();
		}

		[NotNull]
		public static ITable CreateSimpleTable(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string tableName,
			[NotNull] IFields fields,
			[CanBeNull] string configKeyWord = null)
		{
			// create the feature class
			ITable table = workspace.CreateTable(
				tableName, fields, null, null, configKeyWord);

			var classSchemaEdit = (IClassSchemaEdit)table;
			try
			{
				classSchemaEdit.RegisterAsObjectClass(table.OIDFieldName, configKeyWord);
			}
			catch (NotImplementedException)
			{
				// Not implemented for InMemory Workspaces
			}

			// make sure the table is known by the workspace
			((IWorkspaceEdit)workspace).StartEditing(false);
			((IWorkspaceEdit)workspace).StopEditing(true);

			return table;
		}

		[NotNull]
		public static IRelationshipClass CreateSimple1NRelationship(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string name,
			[NotNull] ITable tableOrig,
			[NotNull] ITable tableRel,
			[NotNull] string orig,
			[NotNull] string dest)
		{
			IRelationshipClass rel =
				workspace.CreateRelationshipClass(
					name,
					(IObjectClass)tableOrig, (IObjectClass)tableRel,
					"forLabel", "backLabel",
					esriRelCardinality.esriRelCardinalityOneToMany,
					esriRelNotification.esriRelNotificationNone, false, false, null,
					orig, dest, dest, orig);
			// make sure the table is known by the workspace
			((IWorkspaceEdit)workspace).StartEditing(false);
			((IWorkspaceEdit)workspace).StopEditing(true);
			return rel;
		}

		[NotNull]
		public static IRelationshipClass CreateSimpleMNRelationship(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string name,
			[NotNull] ITable tableOrig,
			[NotNull] ITable tableRel,
			[NotNull] string orig,
			[NotNull] string dest)
		{
			IRelationshipClass rel =
				workspace.CreateRelationshipClass(
					name,
					(IObjectClass)tableOrig, (IObjectClass)tableRel,
					"forLabel", "backLabel",
					esriRelCardinality.esriRelCardinalityManyToMany,
					esriRelNotification.esriRelNotificationBoth, false, false, null,
					"ObjectId", "ObjectID", orig, dest);
			// make sure the table is known by the workspace
			((IWorkspaceEdit)workspace).StartEditing(false);
			((IWorkspaceEdit)workspace).StopEditing(true);
			return rel;
		}

		[NotNull]
		public static IFeatureClass CreateSimpleFeatureClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string name,
			esriGeometryType geometryType,
			[CanBeNull] IFieldsEdit fieldsWithoutShapeField = null,
			esriSRProjCS2Type projType = esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			double xyTolerance = 0,
			bool hasZ = false)
		{
			IFieldsEdit allFields = InitFieldsWithOID(fieldsWithoutShapeField);

			ISpatialReference spatialReference = SpatialReferenceUtils
				.CreateSpatialReference
					((int)projType, true);
			if (xyTolerance > 0)
			{
				((ISpatialReferenceTolerance)spatialReference).XYTolerance =
					xyTolerance;
			}

			if (geometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				hasZ = true;
			}
			if (hasZ)
			{
				SpatialReferenceUtils.SetZDomain(spatialReference, -10000, 10000,
												 0.0001, 0.001);
			}

			allFields.AddField(
				FieldUtils.CreateShapeField("Shape", geometryType, spatialReference, 1000, hasZ));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(workspace, name, allFields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit)workspace).StartEditing(false);
			((IWorkspaceEdit)workspace).StopEditing(true);

			return featureClass;
		}

		private static IFieldsEdit InitFieldsWithOID(IFieldsEdit customFields)
		{
			bool hasOidField = false;
			if (customFields != null)
			{
				for (int iField = 0; iField < customFields.FieldCount; iField++)
				{
					IField field = customFields.Field[iField];
					if (field.Type == esriFieldType.esriFieldTypeOID)
					{
						hasOidField = true;
						break;
					}
				}
			}

			IFieldsEdit fields = customFields ?? new FieldsClass();
			if (! hasOidField)
			{
				fields.AddField(FieldUtils.CreateOIDField());
			}

			return fields;
		}
	}
}
