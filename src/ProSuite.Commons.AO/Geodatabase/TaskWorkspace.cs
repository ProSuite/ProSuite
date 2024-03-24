using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using System;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class TaskWorkspace
	{
		public TaskWorkspace(IWorkspace workspace)
		{
			IWorkspaceFactory factory = workspace.WorkspaceFactory;
			FactoryUid = $"{factory.GetClassID().Value}";
			PropertySet = PropertySetUtils.ToXmlString(workspace.ConnectionProperties);
		}

		private string FactoryUid { get; }
		private string PropertySet { get; }

		public IWorkspace GetWorkspace()
		{
			Guid factoryGuid = new Guid(FactoryUid);
			Type factoryClass = Type.GetTypeFromCLSID(factoryGuid);
			var factory =
				Assert.NotNull(
					(IWorkspaceFactory)Activator.CreateInstance(Assert.NotNull(factoryClass)));
			IPropertySet connectionProps =
				PropertySetUtils.FromXmlString(PropertySet);
			IWorkspace workspace = factory.Open(connectionProps, 0);

			return workspace;
		}
	}
}
