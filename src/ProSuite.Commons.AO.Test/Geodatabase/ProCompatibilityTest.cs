using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	[Ignore("Run in x64 server solution")]
	public class ProCompatibilityTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			_msg.IsVerboseDebugEnabled = true;

			// todo get license..
		}

		[OneTimeTearDown]
		public void TeardownFixture() { }

		public static string GetProCompatibilityTestFileGdbPath()
		{
			//TODO: unzip
			var locator = new TestDataLocator();
			return locator.GetPath("procompatibilitytest.gdb");
		}

		[Test]
		[Ignore("Passes or fails depending on license/runtime")]
		public void LearningTestAnnotationFeatureClass()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenFileGdbWorkspace(GetProCompatibilityTestFileGdbPath());

			//Ao10 AnnotationFeatureClass
			IFeatureClass fClass = DatasetUtils.OpenFeatureClass(
				workspace, "Lines_Anno");

			Assert.NotNull(fClass);

			LogFeatureClassProperties(fClass);

			//Ao11 AnnotationFeatureClass (using "Upgrade Dataset" from Pro)
			IFeatureClass fClassUpgraded = DatasetUtils.OpenFeatureClass(
				workspace, "Lines_Anno_Upgraded");

			Assert.NotNull(fClassUpgraded);

			LogFeatureClassProperties(fClassUpgraded);

			//TODO read/write access to data
		}

		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Compare_annotation_FeatureClass_properties_AO10_and_AO11()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace("...");

			// FeatureClass
			IFeatureClass fc = DatasetUtils.OpenFeatureClass(
				workspace, "...");

			Assert.NotNull(fc);
			LogFeatureClassProperties(fc);
			Console.WriteLine();

			// AO10 AnnotationFeatureClass
			IFeatureClass annotationFc_AO10 = DatasetUtils.OpenFeatureClass(
				workspace, "...");

			Assert.NotNull(annotationFc_AO10);
			LogFeatureClassProperties(annotationFc_AO10);
			Console.WriteLine();

			// AO1 AnnotationFeatureClass
			IFeatureClass annotationFc_AO11 = DatasetUtils.OpenFeatureClass(
				workspace, "...");

			Assert.NotNull(annotationFc_AO11);
			LogFeatureClassProperties(annotationFc_AO11);
		}

		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Can_create_annotation_FeatureClass_with_AO11_0()
		{
			// result:
			// AliasName: ...1
			// FeatureType: esriFTAnnotation
			// CLSID: {3FF1675E-4FFB-4D9B-9438-767CE04DE34A}
			// EXTCLSID: {F245DFEB-851B-4981-9860-4BACC8C0A688}
			//
			// ExtentionProperties is null!
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"...");

			var name = "...";
			try
			{
				DatasetUtils.DeleteFeatureClass(workspace, name);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			IFeatureClass fc = DatasetUtils.OpenFeatureClass(
				workspace, "...");

			IFields fields = fc.Fields;

			IFeatureWorkspace fws = (IFeatureWorkspace) workspace;

			IFeatureDataset featureDataset = fws.OpenFeatureDataset("...");

			IFeatureClass annoFc =
				DatasetUtils.CreateAnnotationFeatureClass(featureDataset, name, fields);

			Assert.NotNull(annoFc);
			LogFeatureClassProperties(annoFc);
		}

#if !ARCGIS_12_0_OR_GREATER
		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Can_create_and_update_annotation_FeatureClass_with_AO11_1()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"...");

			IFeatureClass sourceFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");
			IFeatureClass templateFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");
			Assert.NotNull(templateFc.ExtensionProperties);

			IFields fields = templateFc.Fields;
			IFeatureWorkspace fws = (IFeatureWorkspace) workspace;
			IFeatureDataset featureDataset = fws.OpenFeatureDataset("...");

			var name = "...";

			IDictionary<string, object> propertyDict =
				PropertySetUtils.GetDictionary(templateFc.ExtensionProperties);

			Assert.True(propertyDict.TryGetValue("SymbolCollection", out object property));

			var featureWorkspaceAnno = (IFeatureWorkspaceAnno) workspace;
			object referenceScale = 2500;
			object annoProperties = null;
			object symbolCollection = property;

			IFeatureClass annoFc =
				featureWorkspaceAnno.CreateAnnotationClass(name, fields,
				                                           new UIDClass
				                                           {
					                                           Value =
						                                           "{3FF1675E-4FFB-4D9B-9438-767CE04DE34A}"
				                                           },
				                                           new UIDClass
				                                           {
					                                           Value =
						                                           "{F245DFEB-851B-4981-9860-4BACC8C0A688}"
				                                           },
				                                           "SHAPE", string.Empty, featureDataset,
				                                           sourceFc, annoProperties, referenceScale,
				                                           symbolCollection, false);

			Assert.NotNull(annoFc);
			LogFeatureClassProperties(annoFc);
		}
#endif

		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Can_create_and_update_annotation_FeatureClass_with_AO11_2()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"...");

			IFeatureClass templateFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");
			Assert.NotNull(templateFc.ExtensionProperties);

			IFields fields = templateFc.Fields;
			IFeatureWorkspace fws = (IFeatureWorkspace) workspace;
			IFeatureDataset featureDataset = fws.OpenFeatureDataset("...");

			var fcName = "...";

			IDictionary<string, object> template =
				PropertySetUtils.GetDictionary(templateFc.ExtensionProperties);

			IFeatureClass annoFc =
				DatasetUtils.CreateAnnotationFeatureClass(featureDataset, fcName, fields);
			IPropertySet properties = annoFc.ExtensionProperties;

			object property;

			var name = "ReferenceScale";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "ReferenceScaleUnit";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "SymbolCollection";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "LabelClassCollection";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "MaxLabelClassID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "GeneralPlacementProperties";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			Assert.NotNull(annoFc);
			LogFeatureClassProperties(annoFc);
		}

#if !ARCGIS_12_0_OR_GREATER
		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Can_create_and_update_annotation_FeatureClass_with_AO11_3()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"...");

			IFeatureClass sourceFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");

			IFeatureClass templateFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");
			Assert.NotNull(templateFc.ExtensionProperties);

			IFields fields = templateFc.Fields;
			IFeatureWorkspace fws = (IFeatureWorkspace) workspace;
			IFeatureDataset featureDataset = fws.OpenFeatureDataset("...");

			var fcName = "...";

			IDictionary<string, object> template =
				PropertySetUtils.GetDictionary(templateFc.ExtensionProperties);

			object property;

			var name = "SymbolCollection";
			Assert.True(template.TryGetValue(name, out property));

			var featureWorkspaceAnno = (IFeatureWorkspaceAnno) workspace;
			object referenceScale = 2500;
			object annoProperties = null;
			object symbolCollection = property;

			IFeatureClass annoFc =
				featureWorkspaceAnno.CreateAnnotationClass(fcName, fields,
				                                           new UIDClass
				                                           {
					                                           Value =
						                                           "{3FF1675E-4FFB-4D9B-9438-767CE04DE34A}"
				                                           },
				                                           new UIDClass
				                                           {
					                                           Value =
						                                           "{F245DFEB-851B-4981-9860-4BACC8C0A688}"
				                                           },
				                                           "SHAPE", string.Empty, featureDataset,
				                                           sourceFc, annoProperties, referenceScale,
				                                           symbolCollection, false);

			IPropertySet properties = annoFc.ExtensionProperties;
			properties.RemoveProperty("AnnoProperties");
			properties.RemoveProperty("SourceFeatureClass");

			IDictionary<string, object> dictionary =
				PropertySetUtils.GetDictionary(annoFc.ExtensionProperties);
			Assert.False(dictionary.ContainsKey("AnnoProperties"));
			Assert.False(dictionary.ContainsKey("SourceFeatureClass"));

			var sourceSubTypes = (ISubtypes) templateFc;
			Console.WriteLine(sourceSubTypes.SubtypeFieldName);

			IList<Subtype> subtypes = DatasetUtils.GetSubtypes(templateFc);
			using (new SchemaLock(annoFc))
			{
				var annoSubtypes = (ISubtypes) annoFc;
				annoSubtypes.SubtypeFieldName = sourceSubTypes.SubtypeFieldName;

				foreach (Subtype subtype in subtypes)
				{
					Console.WriteLine($"{subtype.Name}: {subtype.Code}");
					annoSubtypes.AddSubtype(subtype.Code, subtype.Name);

					annoSubtypes.DefaultSubtypeCode = sourceSubTypes.DefaultSubtypeCode;
				}
			}

			name = "SymbolCollection";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "CIMVersion";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "Build";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "Version";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "AllowSymbolOverrides";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "RequireSymbolID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "AutoCreate";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "UpdateOnShapeChange";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			//name = "ReferenceScale";
			//if (template.TryGetValue(name, out property))
			//{
			//	properties.SetProperty(name, property);
			//}

			name = "ReferenceScaleUnit";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "MaxSymbolID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "LabelClassCollection";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "MaxLabelClassID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "GeneralPlacementProperties";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			Assert.NotNull(annoFc);
			LogFeatureClassProperties(annoFc);
		}
#endif

#if !ARCGIS_12_0_OR_GREATER
		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Can_copy_annotation_features_with_AO11_4()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"...");

			IFeatureClass sourceFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");

			IFeatureClass templateFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");
			Assert.NotNull(templateFc.ExtensionProperties);

			IFields fields = templateFc.Fields;
			IFeatureWorkspace fws = (IFeatureWorkspace) workspace;
			IFeatureDataset featureDataset = fws.OpenFeatureDataset("...");

			var fcName = "...";

			IDictionary<string, object> template =
				PropertySetUtils.GetDictionary(templateFc.ExtensionProperties);

			object property;

			var name = "SymbolCollection";
			Assert.True(template.TryGetValue(name, out property));

			var featureWorkspaceAnno = (IFeatureWorkspaceAnno) workspace;
			object referenceScale = 2500;
			object annoProperties = null;
			object symbolCollection = property;

			IFeatureClass annoFc =
				featureWorkspaceAnno.CreateAnnotationClass(fcName, fields,
				                                           new UIDClass
				                                           {
					                                           Value =
						                                           "{3FF1675E-4FFB-4D9B-9438-767CE04DE34A}"
				                                           },
				                                           new UIDClass
				                                           {
					                                           Value =
						                                           "{F245DFEB-851B-4981-9860-4BACC8C0A688}"
				                                           },
				                                           "SHAPE", string.Empty, featureDataset,
				                                           sourceFc, annoProperties, referenceScale,
				                                           symbolCollection, false);

			IPropertySet properties = annoFc.ExtensionProperties;
			properties.RemoveProperty("AnnoProperties");
			properties.RemoveProperty("SourceFeatureClass");

			IDictionary<string, object> dictionary =
				PropertySetUtils.GetDictionary(annoFc.ExtensionProperties);
			Assert.False(dictionary.ContainsKey("AnnoProperties"));
			Assert.False(dictionary.ContainsKey("SourceFeatureClass"));

			var sourceSubTypes = (ISubtypes) templateFc;
			Console.WriteLine(sourceSubTypes.SubtypeFieldName);

			IList<Subtype> subtypes = DatasetUtils.GetSubtypes(templateFc);
			using (new SchemaLock(annoFc))
			{
				var annoSubtypes = (ISubtypes) annoFc;
				annoSubtypes.SubtypeFieldName = sourceSubTypes.SubtypeFieldName;

				foreach (Subtype subtype in subtypes)
				{
					Console.WriteLine($"{subtype.Name}: {subtype.Code}");
					annoSubtypes.AddSubtype(subtype.Code, subtype.Name);

					annoSubtypes.DefaultSubtypeCode = sourceSubTypes.DefaultSubtypeCode;
				}
			}

			name = "SymbolCollection";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "CIMVersion";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "Build";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "Version";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "AllowSymbolOverrides";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "RequireSymbolID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "AutoCreate";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "UpdateOnShapeChange";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			//name = "ReferenceScale";
			//if (template.TryGetValue(name, out property))
			//{
			//	properties.SetProperty(name, property);
			//}

			name = "ReferenceScaleUnit";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "MaxSymbolID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "LabelClassCollection";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "MaxLabelClassID";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			name = "GeneralPlacementProperties";
			if (template.TryGetValue(name, out property))
			{
				properties.SetProperty(name, property);
			}

			Assert.NotNull(annoFc);
			LogFeatureClassProperties(annoFc);

			IDictionary<int, int> matrix =
				GdbObjectUtils.CreateCopyIndexMatrix(templateFc, annoFc, null,
				                                     FieldComparison.FieldName);

			NotificationCollection notifications = new NotificationCollection();

			foreach (IFeature templateFeature in GdbQueryUtils.GetFeatures(templateFc, false)
			                                                  .Take(3))
			{
				IFeature annoFeature = GdbObjectUtils.CreateFeature(annoFc);

				if (! GdbObjectUtils.TryCopyAttributeValues(
					    templateFeature, annoFeature, matrix, notifications))
				{
					_msg.WarnFormat("Unable to copy all fields from {0} to {1}. {2}",
					                RowFormat.GetDisplayValue(templateFeature),
					                RowFormat.GetDisplayValue(annoFeature),
					                NotificationUtils.Concatenate(notifications, ". "));
				}

				GdbObjectUtils.SetFeatureShape(annoFeature, templateFeature.ShapeCopy);

				annoFeature.Store();
			}
		}
#endif

		[Test]
		[Ignore("Learning test: dependes on local data")]
		public void Can_copy_annotation_features_to_existing_anno_feataure_class_with_AO11()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"...");

			IFeatureClass templateFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");

			IFeatureClass annoFc =
				DatasetUtils.OpenFeatureClass(workspace, "...");

			IDictionary<int, int> matrix =
				GdbObjectUtils.CreateCopyIndexMatrix(templateFc, annoFc, null,
				                                     FieldComparison.FieldName);

			NotificationCollection notifications = new NotificationCollection();

			foreach (IFeature templateFeature in GdbQueryUtils.GetFeatures(templateFc, false)
			                                                  .Take(3))
			{
				IFeature annoFeature = GdbObjectUtils.CreateFeature(annoFc);

				if (! GdbObjectUtils.TryCopyAttributeValues(
					    templateFeature, annoFeature, matrix, notifications))
				{
					_msg.WarnFormat("Unable to copy all fields from {0} to {1}. {2}",
					                RowFormat.GetDisplayValue(templateFeature),
					                RowFormat.GetDisplayValue(annoFeature),
					                NotificationUtils.Concatenate(notifications, ". "));
				}

				GdbObjectUtils.SetFeatureShape(annoFeature, templateFeature.ShapeCopy);

				annoFeature.Store();
			}
		}

		private static void LogFeatureClassProperties([NotNull] IFeatureClass fClass)
		{
			Console.WriteLine("AliasName: {0}", fClass.AliasName);
			Console.WriteLine("FeatureType: {0}", fClass.FeatureType);
			Console.WriteLine("CLSID: {0}", fClass.CLSID.Value);
			if (fClass.EXTCLSID != null)
			{
				Console.WriteLine("EXTCLSID: {0}", fClass.EXTCLSID.Value);
			}

			if (fClass.ExtensionProperties == null)
			{
				return;
			}

			IDictionary<string, object> propertyDict =
				PropertySetUtils.GetDictionary(fClass.ExtensionProperties);

			foreach (KeyValuePair<string, object> keyValuePair in propertyDict)
			{
				string displayKey = keyValuePair.Key;
				string displayValue =
					keyValuePair.Value == null ? "null" : keyValuePair.Value.ToString();
				Console.WriteLine("  {0}: {1}", displayKey, displayValue);
			}
		}
	}
}
