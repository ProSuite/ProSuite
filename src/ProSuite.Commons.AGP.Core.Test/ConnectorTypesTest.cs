using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using NUnit.Framework;

namespace ProSuite.Commons.AGP.Core.Test
{
	/// <summary>
	/// Guards the connector types that WorkspaceUtils switches over. ArcGIS Pro 3.8 added
	/// ArcGISServerConnectionFile, which Pro 3.7 did not have; without this test such an
	/// addition only shows up as "connector &lt;type&gt; is not implemented" at run time.
	/// </summary>
	[TestFixture]
	public class ConnectorTypesTest
	{
		private static readonly string[] _expectedConnectorTypes =
		{
			// Not switched over in WorkspaceUtils: it exists from ArcGIS Pro 3.8 on only and
			// cannot be named in code that is also compiled against earlier versions.
			"ArcGIS.Core.Data.ArcGISServerConnectionFile",

			"ArcGIS.Core.Data.BimFileConnectionProperties",
			"ArcGIS.Core.Data.DatabaseConnectionFile",
			"ArcGIS.Core.Data.DatabaseConnectionProperties",
			"ArcGIS.Core.Data.FileGeodatabaseConnectionPath",
			"ArcGIS.Core.Data.FileSystemConnectionPath",
			"ArcGIS.Core.Data.Knowledge.KnowledgeGraphConnectionProperties",
			"ArcGIS.Core.Data.MemoryConnectionProperties",
			"ArcGIS.Core.Data.MobileGeodatabaseConnectionPath",
			"ArcGIS.Core.Data.PluginDatastore.PluginDatasourceConnectionPath",
			"ArcGIS.Core.Data.Realtime.RealtimeServiceConnectionProperties",
			"ArcGIS.Core.Data.SQLiteConnectionPath",
			"ArcGIS.Core.Data.ServiceConnectionProperties"
		};

		[Test]
		public void ArcGisProHasNoUnexpectedConnectorTypes()
		{
			List<string> added = GetConnectorTypeNames()
			                     .Except(_expectedConnectorTypes)
			                     .OrderBy(name => name, StringComparer.Ordinal)
			                     .ToList();

			Assert.IsEmpty(added,
			               "ArcGIS Pro has new connector types: {0}. Handle each of them in " +
			               "WorkspaceUtils.TryGetWorkspaceFactory, GetConnectionString and " +
			               "OpenDatastore, then add it to this test.",
			               string.Join(", ", added));
		}

		[Test]
		public void ArcGisProStillHasAllExpectedConnectorTypes()
		{
			ICollection<string> present = GetConnectorTypeNames();

			// Pro 3.7 and earlier have fewer connector types than the current version, so
			// only the ones this project cannot be built without are required here.
			List<string> removed = _expectedConnectorTypes
			                       .Except(present)
			                       .Except(new[] { "ArcGIS.Core.Data.ArcGISServerConnectionFile" })
			                       .OrderBy(name => name, StringComparer.Ordinal)
			                       .ToList();

			Assert.IsEmpty(removed, "ArcGIS Pro no longer has connector types: {0}",
			               string.Join(", ", removed));
		}

		private static ICollection<string> GetConnectorTypeNames()
		{
			return typeof(Connector).Assembly
			                        .GetExportedTypes()
			                        .Where(type => typeof(Connector).IsAssignableFrom(type) &&
			                                       ! type.IsAbstract)
			                        .Select(type => type.FullName)
			                        .ToList();
		}
	}
}
