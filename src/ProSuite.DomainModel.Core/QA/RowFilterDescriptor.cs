using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public class RowFilterDescriptor : InstanceDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		public RowFilterDescriptor() { }

		public RowFilterDescriptor([NotNull] string name,
		                           [NotNull] ClassDescriptor testClass,
		                           int testConstructorId,
		                           string description = null)
			: base(name, testClass, testConstructorId, description) { }
	}
}