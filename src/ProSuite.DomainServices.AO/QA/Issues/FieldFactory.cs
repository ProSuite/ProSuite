using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class FieldFactory
	{
		/// <summary>
		/// Creates a field according to the specified definition. In the future, there could be
		/// other field factories such as ProFieldFactory....
		/// </summary>
		/// <param name="fieldDefinition">The field definition.</param>
		/// <param name="gdbDomains">The available domains in the geodatabase.</param>
		/// <returns></returns>
		public static IField CreateField([NotNull] FieldDefinition fieldDefinition,
		                                 [CanBeNull] IList<IDomain> gdbDomains)
		{
			IField result;
			if (fieldDefinition is TextFieldDefintion textFieldDefinition)
			{
				result = FieldUtils.CreateTextField(textFieldDefinition.Name,
				                                    textFieldDefinition.Length,
				                                    textFieldDefinition.AliasName);
			}
			else
			{
				result = FieldUtils.CreateField(fieldDefinition.Name,
				                                fieldDefinition.Type,
				                                fieldDefinition.AliasName);
			}

			if (fieldDefinition.Domain != null)
			{
				Assert.NotNull(gdbDomains,
				               "gdbDomains not specified despite the field having a domain.");

				IDomain domain =
					gdbDomains.FirstOrDefault(d => d.Name == fieldDefinition.Domain.Name);

				if (domain != null)
				{
					((IFieldEdit) result).Domain_2 = domain;
				}
			}

			return result;
		}
	}
}
