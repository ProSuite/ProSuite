using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public class IssueFilterDescriptor : InstanceDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IssueFilterDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		public IssueFilterDescriptor() { }

		public IssueFilterDescriptor([NotNull] string name,
		                             [NotNull] ClassDescriptor implementationClass,
		                             int constructorId,
		                             string description = null)
			: base(name, implementationClass, constructorId, description) { }

		#region Overrides of InstanceDescriptor

		public override string TypeDisplayName => "Issue Filter Descriptor";

		public override InstanceConfiguration CreateConfiguration()
		{
			return new IssueFilterConfiguration(true)
			       {
				       InstanceDescriptor = this
			       };
		}

		#endregion
	}
}
