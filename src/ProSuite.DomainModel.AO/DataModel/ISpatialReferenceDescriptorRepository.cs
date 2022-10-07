using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface ISpatialReferenceDescriptorRepository :
		IRepository<SpatialReferenceDescriptor>
	{
		SpatialReferenceDescriptor Get(string name);
	}
}
