using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.DataModel.Repositories
{
	public interface ISpatialReferenceDescriptorRepository : IRepository<SpatialReferenceDescriptor>
	{
		SpatialReferenceDescriptor Get(string name);
	}
}
