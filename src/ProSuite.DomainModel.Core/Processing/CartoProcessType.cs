using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Processing
{
	public class CartoProcessType : EntityWithMetadata, INamed, IAnnotated
	{
		#region Fields

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private ClassDescriptor _cartoProcessClassDescriptor;

		#endregion

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public CartoProcessType() { }

		public CartoProcessType([NotNull] string name, [CanBeNull] string description,
		                        [NotNull] ClassDescriptor classDescriptor)
		{
			Assert.ArgumentNotNull(name, nameof(name));
			Assert.ArgumentNotNull(classDescriptor, nameof(classDescriptor));

			_name = name;
			_description = description;
			_cartoProcessClassDescriptor = classDescriptor;
		}

		#endregion

		#region Properties

		#region INamed Members

		[Required]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion

		#region IAnnotated Members

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		#endregion

		[Required]
		public ClassDescriptor CartoProcessClassDescriptor
		{
			get { return _cartoProcessClassDescriptor; }
			set { _cartoProcessClassDescriptor = value; }
		}

		#endregion

		public override string ToString()
		{
			return Name ?? string.Empty;
		}
	}
}
