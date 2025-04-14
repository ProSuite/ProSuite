using ProSuite.Commons.DomainModels;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface ISpatialReferenceDescriptorRepository : IRepository<SpatialReferenceDescriptor>
	{
		SpatialReferenceDescriptor Get(string name);
	}
}
