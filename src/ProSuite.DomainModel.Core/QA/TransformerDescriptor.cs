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
		                             [NotNull] ClassDescriptor testClass,
		                             int constructorId,
		                             string description = null)
			: base(name, testClass, constructorId, description) { }
	}
}
