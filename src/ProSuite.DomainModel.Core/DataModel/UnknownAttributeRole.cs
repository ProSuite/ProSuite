namespace ProSuite.DomainModel.Core.DataModel
{
	public class UnknownAttributeRole : AttributeRole
	{
		public UnknownAttributeRole(int id) : base(id) { }

		protected override string GetName()
		{
			return string.Format("<unknown> ({0})", Id);
		}
	}
}