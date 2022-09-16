using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public class TransformerDescriptor : InstanceDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TransformerDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		public TransformerDescriptor() { }

		public TransformerDescriptor([NotNull] string name,
		                             [NotNull] ClassDescriptor implementationClass,
		                             int constructorId,
		                             string description = null)
			: base(name, implementationClass, constructorId, description) { }

		#region Overrides of InstanceDescriptor

		public override string TypeDisplayName => "Transformer Descriptor";

		public override InstanceConfiguration CreateConfiguration()
		{
			return new TransformerConfiguration(true)
			       {
				       InstanceDescriptor = this
			       };
		}

		#endregion
	}
}
