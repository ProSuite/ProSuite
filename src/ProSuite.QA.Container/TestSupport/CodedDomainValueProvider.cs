using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Container.TestSupport
{
	/// <summary>
	/// Provides values from the names of a coded value domain, converted to a specified value
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>Currently upports only fields with one fixed domain (i.e. subtype-dependent domains not supported)</remarks>
	public class CodedDomainValueProvider<T> : IValueProvider<T> where T : struct
	{
		private readonly int _codedFieldIndex;
		private readonly Dictionary<object, T?> _valueDict;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public CodedDomainValueProvider([NotNull] IReadOnlyTable table,
		                                [NotNull] string codedDomainField,
		                                [CanBeNull] IFormatProvider formatProvider)
		{
			_codedFieldIndex = table.FindField(codedDomainField);

			IField field = table.Fields.Field[_codedFieldIndex];
			var domain = (ICodedValueDomain) field.Domain;

			_valueDict = new Dictionary<object, T?>();
			foreach (CodedValue codedValue in DomainUtils.GetCodedValueList(domain))
			{
				object code = codedValue.Value;
				string name = codedValue.Name;

				try
				{
					var value =
						(T) Assert.NotNull(Convert.ChangeType(name, typeof(T), formatProvider));

					_valueDict.Add(code, value);
				}
				catch (Exception e)
				{
					_msg.VerboseDebug(
						() => $"Unable to convert '{name}' to type {typeof(T).Name}: {e.Message}",
						e);
					_valueDict.Add(code, null);
				}
			}
		}

		T? IValueProvider<T>.GetValue(IReadOnlyRow row)
		{
			object code = row.get_Value(_codedFieldIndex);

			T? value;
			return _valueDict.TryGetValue(code, out value)
				       ? value
				       : null;
		}
	}
}
