using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	/// <summary>
	/// A <see cref="ArcDatasetName.WorkspaceName"/> must not assume the dataset's workspace is an
	/// <see cref="ArcWorkspace"/>. Datasets backed by a <see cref="BasicWorkspace"/> (shapefiles,
	/// plug-in datasources) are not an ArcWorkspace and previously triggered an InvalidCastException.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ArcDatasetNameTest
	{
		private ArcGIS.Core.Data.Geodatabase _gdb;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();

			_gdb = SchemaBuilder.CreateGeodatabase(
				new MemoryConnectionProperties("ArcDatasetNameTest"));
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_gdb?.Dispose();
		}

		[Test]
		public void WorkspaceName_does_not_require_an_ArcWorkspace()
		{
			// A BasicWorkspace is the non-ArcWorkspace workspace used for shapefiles and plug-in
			// datasources. (Backed here by a memory datastore purely for test convenience - what
			// matters is that it is NOT an ArcWorkspace.)
			var basicWorkspace = new BasicWorkspace(_gdb);

			var datasetName = new ArcDatasetName(new DatasetStub(basicWorkspace));

			IWorkspaceName workspaceName = null;

			Assert.DoesNotThrow(
				() => workspaceName = datasetName.WorkspaceName,
				"WorkspaceName must support datasets whose workspace is a BasicWorkspace, " +
				"not only ArcWorkspace.");

			Assert.NotNull(workspaceName);
			Assert.IsInstanceOf<BasicWorkspaceName>(workspaceName);
		}

		/// <summary>
		/// Minimal <see cref="IDataset"/> that only exposes a workspace; all other members are
		/// irrelevant to <see cref="ArcDatasetName.WorkspaceName"/>.
		/// </summary>
		private sealed class DatasetStub : IDataset
		{
			private readonly IWorkspace _workspace;

			public DatasetStub(IWorkspace workspace)
			{
				_workspace = workspace;
			}

			public IWorkspace Workspace => _workspace;

			public string Name => "stub";

			public IName FullName => throw new NotImplementedException();

			public string BrowseName
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			public esriDatasetType Type => esriDatasetType.esriDTFeatureClass;

			public string Category => throw new NotImplementedException();

			public IEnumerable<IDataset> Subsets => throw new NotImplementedException();

			public object NativeImplementation => throw new NotImplementedException();

			public bool CanCopy() => false;

			public bool CanDelete() => false;

			public void Delete() => throw new NotImplementedException();

			public bool CanRename() => false;

			public void Rename(string name) => throw new NotImplementedException();
		}
	}
}
