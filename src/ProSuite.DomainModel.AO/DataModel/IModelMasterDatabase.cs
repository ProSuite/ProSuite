namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IModelMasterDatabase
	{
		/// <remarks>Like <see cref="IMasterDatabaseWorkspaceContextFactory.Create"/>
		/// but without the model parameter. Helps disentangle Model and AO/Enterprise
		/// dependency: concrete model subclasses shall implement this
		/// </remarks>
		IWorkspaceContext CreateMasterDatabaseWorkspaceContext();
	}
}
