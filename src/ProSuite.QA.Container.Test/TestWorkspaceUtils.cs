using System;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Path = System.IO.Path;

namespace ProSuite.QA.Container.Test
{
	public static class TestWorkspaceUtils
	{
		[NotNull]
		public static IFeatureWorkspace CreateTestWorkspace([NotNull] string mdbName)
		{
			string dir = Path.GetTempPath();

			string mdb = Path.Combine(dir, mdbName) + ".gdb";

			if (Directory.Exists(mdb))
			{
				Directory.Delete(mdb, true);
			}

			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(dir, mdbName);
			return (IFeatureWorkspace) ((IName) wsName).Open();
		}

		[NotNull]
		public static IFeatureWorkspace CreateTestFgdbWorkspace(
			[NotNull] string gdbName)
		{
			string dir = Path.GetTempPath();

			string gdb = Path.Combine(dir, gdbName) + ".gdb";

			if (Directory.Exists(gdb))
			{
				Directory.Delete(gdb, true);
			}

			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(dir, gdbName);

			return (IFeatureWorkspace) ((IName) wsName).Open();
		}

		[NotNull]
		public static IFeatureWorkspace CreateInMemoryWorkspace([NotNull] string name)
		{
			IWorkspaceName wsName = WorkspaceUtils.CreateInMemoryWorkspace(name);

			return (IFeatureWorkspace) ((IName) wsName).Open();
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

			var classSchemaEdit = (IClassSchemaEdit) table;
			try
			{
				classSchemaEdit.RegisterAsObjectClass(table.OIDFieldName, configKeyWord);
			}
			catch (NotImplementedException)
			{
				// Not implemented for InMemory Workspaces
			}

			// make sure the table is known by the workspace
			((IWorkspaceEdit) workspace).StartEditing(false);
			((IWorkspaceEdit) workspace).StopEditing(true);

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
					(IObjectClass) tableOrig, (IObjectClass) tableRel,
					"forLabel", "backLabel",
					esriRelCardinality.esriRelCardinalityOneToMany,
					esriRelNotification.esriRelNotificationNone, false, false, null,
					orig, dest, dest, orig);
			// make sure the table is known by the workspace
			((IWorkspaceEdit) workspace).StartEditing(false);
			((IWorkspaceEdit) workspace).StopEditing(true);
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
					(IObjectClass) tableOrig, (IObjectClass) tableRel,
					"forLabel", "backLabel",
					esriRelCardinality.esriRelCardinalityManyToMany,
					esriRelNotification.esriRelNotificationBoth, false, false, null,
					"ObjectId", "ObjectID", orig, dest);
			// make sure the table is known by the workspace
			((IWorkspaceEdit) workspace).StartEditing(false);
			((IWorkspaceEdit) workspace).StopEditing(true);
			return rel;
		}

		[NotNull]
		public static IFeatureClass CreateSimpleFeatureClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string name,
			[CanBeNull] IFieldsEdit fieldsWithoutShapeField,
			esriGeometryType geometryType,
			esriSRProjCS2Type projType,
			double xyTolerance = 0,
			bool hasZ = false)
		{
			if (fieldsWithoutShapeField == null)
			{
				fieldsWithoutShapeField = new FieldsClass();
				fieldsWithoutShapeField.AddField(FieldUtils.CreateOIDField());
			}

			ISpatialReference spatialReference = SpatialReferenceUtils
				.CreateSpatialReference
					((int) projType, true);
			if (xyTolerance > 0)
			{
				((ISpatialReferenceTolerance) spatialReference).XYTolerance =
					xyTolerance;
			}

			if (hasZ)
			{
				SpatialReferenceUtils.SetZDomain(spatialReference, -10000, 10000,
				                                 0.0001, 0.001);
			}

			fieldsWithoutShapeField.AddField(
				FieldUtils.CreateShapeField("Shape", geometryType, spatialReference, 1000, hasZ));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(
					workspace, name, fieldsWithoutShapeField);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) workspace).StartEditing(false);
			((IWorkspaceEdit) workspace).StopEditing(true);

			return featureClass;
		}
	}
}
