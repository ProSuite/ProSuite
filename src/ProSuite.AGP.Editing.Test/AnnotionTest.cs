using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;

namespace ProSuite.AGP.Editing.Test;

[TestFixture]
public class AnnotionTest
{
	[SetUp]
	public void Setup()
	{
		// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
		SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
	}

	[OneTimeSetUp]
	public void OneTimeSetup()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void Can_find_AnnotationClassID_in_LabelClassCollection()
	{
		string path = TestDataPreparer.ExtractZip("WU226_Rapperswil.gdb.zip").GetPath();

		var gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));

		var annoFc = gdb.OpenDataset<AnnotationFeatureClass>("DKM50_FREIZEITAREAL_ANNO");

		// Kinderzoo
		Feature feature = GdbQueryUtils.GetFeature(annoFc, 1);

		var annotationFeature = feature as AnnotationFeature;
		Assert.NotNull(annotationFeature);

		int annotationClassID = annotationFeature.GetAnnotationClassID();

		AnnotationFeatureClassDefinition definition = annoFc.GetDefinition();

		IReadOnlyList<CIMLabelClass> labelClassCollection = definition.GetLabelClassCollection();

		bool success = false;
		foreach (CIMLabelClass labelClass in labelClassCollection)
		{
			if (labelClass.ID == annotationClassID)
			{
				success = true;
			}
		}

		Assert.True(success, "AnnotationClassID not found in label class collection");
	}
}
