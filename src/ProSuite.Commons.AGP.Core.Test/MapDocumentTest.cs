using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.CIM;
using ProSuite.Commons.AGP.Core.Carto;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class MapDocumentTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanTraverse()
	{
		const string mapxPath = @"C:\Temp\DKM25_Thun.mapx";
		var mapDoc = MapDocument.Open(mapxPath);

		mapDoc.Traverse(Visitor, "state");
	}

	private static bool Visitor(CIMDefinition member, Stack<CIMDefinition> parents, string state)
	{
		Assert.NotNull(member);
		Assert.NotNull(parents);

		var parent = string.Join(" / ", parents.Reverse().Select(cim => cim.Name));
		var text = string.IsNullOrEmpty(parent) ? member.Name : $"{parent} / {member.Name}";
		Console.WriteLine(text);

		return true;
	}

	[Test]
	public void CanFind()
	{
		const string mapxPath = @"C:\Temp\DKM25_Thun.mapx";
		var mapDoc = MapDocument.Open(mapxPath);

		var lyrs1 = mapDoc.FindLayers<CIMFeatureLayer>("DKM25_STRASSE");
		var lyrs2 = mapDoc.FindLayers<CIMGroupLayer>("DKM25");
		var lyrs3 = mapDoc.FindLayers<CIMBaseLayer>("DKM25_STRASSE", parent: "DKM25");
		var lyrs4 = mapDoc.FindLayers<CIMBaseLayer>("DKM25_STRASSE", ancestor: "DKM25");
		var lyrs5 = mapDoc.FindLayers<CIMBaseLayer>("Dkm25_Strasse", ancestor: "dkm25", ignoreCase: true);
	}
}
