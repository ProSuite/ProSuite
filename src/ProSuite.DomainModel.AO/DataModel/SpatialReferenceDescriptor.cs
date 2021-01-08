using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class SpatialReferenceDescriptor : EntityWithMetadata, INamed, IAnnotated
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private string _xmlString;

		private ISpatialReference _spatialReference;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptor"/> class.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		[CLSCompliant(false)]
		public SpatialReferenceDescriptor([NotNull] ISpatialReference spatialReference)
			: this(spatialReference.Name, SpatialReferenceUtils.ToXmlString(spatialReference)) { }

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

		[NotNull]
		[CLSCompliant(false)]
		public ISpatialReference SpatialReference
			=> _spatialReference ?? (_spatialReference = CreateSpatialReference());

		public override string ToString()
		{
			return _name;
		}

		#region Non-public members

		[NotNull]
		private ISpatialReference CreateSpatialReference()
		{
			if (string.IsNullOrEmpty(_xmlString))
			{
				throw new InvalidConfigurationException(
					"Spatial reference xml string not defined");
			}

			return SpatialReferenceUtils.FromXmlString(_xmlString);
		}

		#endregion
	}
}
