using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IAttributeConfigurator
	{
		[NotNull]
		IList<ObjectAttributeType> DefineAttributeTypes();

		/// <summary>
		/// Configures an existing attribute by inferring properties based on a
		/// IField instance.
		/// </summary>
		/// <param name="attribute">The attribute to be configured.</param>
		/// <param name="field">The field.</param>
		/// <param name="assignedAttributeType">if an attribute type was assigned to the attribute 
		/// in this call, it is returned..</param>
		void Configure([NotNull] ObjectAttribute attribute,
		               [NotNull] IField field,
		               [CanBeNull] out ObjectAttributeType assignedAttributeType);

		void Configure([NotNull] IObjectDataset dataset, [NotNull] IObjectClass objectClass);
	}
}
