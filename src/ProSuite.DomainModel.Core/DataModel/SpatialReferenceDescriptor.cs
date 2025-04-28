using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class SpatialReferenceDescriptor : EntityWithMetadata, INamed, IAnnotated
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private string _xmlString;

		//Used for a cached ISpatialReference during runtime
		public object SpatialReferenceCache { get; set; }

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		public SpatialReferenceDescriptor() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptor"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="xmlString">The XML spatial reference string.</param>
		public SpatialReferenceDescriptor([NotNull] string name, [NotNull] string xmlString)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNullOrEmpty(xmlString, nameof(xmlString));

			_name = name;
			_xmlString = xmlString;
		}

		#endregion

		[Required]
		[UsedImplicitly]
		public string Name
		{
			get { return _name; }
			set
			{
				if (! Equals(_name, value))
				{
					_name = value;
				}
			}
		}

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[Required]
		public string XmlString
		{
			get { return _xmlString; }
			set { _xmlString = value; }
		}

		public override string ToString()
		{
			return _name;
		}
	}
}
