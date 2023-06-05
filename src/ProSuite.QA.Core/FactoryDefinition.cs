namespace ProSuite.QA.Core
{
	/// <summary>
	/// Base class for instance definitions. The definitions can be instantiated in every
	/// environment in order to get the metadata.
	/// </summary>
	public abstract class TestFactoryDefinition : InstanceInfoBase
	{
		public override string[] TestCategories => InstanceUtils.GetCategories(GetType());

		public override string ToString()
		{
			return $"{GetType().Name} with parameters: {InstanceUtils.GetTestSignature(this)}";
		}
	}
}
