using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.AGP.Core.Utils
{
	public class PseudoFeature : IRowValues
	{
		private readonly int _shapeFieldIndex;
		private readonly string[] _fieldNames;
		private readonly object[] _values;

		public PseudoFeature(IEnumerable<string> fieldNames, string shapeFieldName)
		{
			var fields = fieldNames?.ToList() ?? new List<string>();

			_shapeFieldIndex = -1;
			for (int i = 0; i < fields.Count; i++)
			{
				if (string.Equals(fields[i], shapeFieldName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					_shapeFieldIndex = i;
					break;
				}
			}

			if (_shapeFieldIndex < 0)
			{
				_shapeFieldIndex = fields.Count;
				fields.Add(shapeFieldName);
			}

			_fieldNames = fields.ToArray();
			_values = new object[fields.Count];
		}

		public static PseudoFeature FromFeature(Feature feature)
		{
			using var fc = feature.GetTable();
			using var defn = fc.GetDefinition();
			var pf = FromDefinition(defn);
			var fields = defn.GetFields();
			int count = fields.Count;
			for (int i = 0; i < count; i++)
			{
				pf[i] = feature[i];
			}
			pf.Shape = feature.GetShape();
			return pf;
		}

		public static PseudoFeature FromDefinition(FeatureClassDefinition definition, int subtypeCode = -1)
		{
			var shapeField = definition.GetShapeField();
			var fields = definition.GetFields();

			var result = new PseudoFeature(fields.Select(f => f.Name), shapeField);

			result.SetDefaultValues(definition, subtypeCode);

			return result;
		}

		/// <summary>
		/// Set subtype defaults (if valid subtype code is given) or table defaults
		/// (otherwise). This feature and the given table definition are assumed to
		/// have the SAME fields in the SAME order; this assumption is not checked!
		/// </summary>
		private void SetDefaultValues(TableDefinition definition, int subtypeCode = -1)
		{
			if (definition is null)
				throw new ArgumentNullException(nameof(definition));

			var subtypes = definition.GetSubtypes();
			var subtype = subtypes?.FirstOrDefault(s => s.GetCode() == subtypeCode);
			var subtypeField = definition.GetSubtypeField();

			var fields = definition.GetFields();
			for (int i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				var value = field.GetDefaultValue(subtype);

				// empirical: subtype field has no per-subtype default value: set it anyway!
				if (subtype != null && subtypeField != null &&
				    string.Equals(field.Name, subtypeField, StringComparison.OrdinalIgnoreCase))
				{
					_values[i] = subtypeCode;
				}
				else if (value != null)
				{
					_values[i] = value;
				}
			}

			if (subtypes != null)
			{
				foreach (var disposable in subtypes)
				{
					disposable.Dispose();
				}
			}
		}

		public Geometry Shape
		{
			get => _values[_shapeFieldIndex] as Geometry;
			set => _values[_shapeFieldIndex] = value;
		}

		public IReadOnlyList<string> FieldNames => _fieldNames;

		public object this[int index]
		{
			get => _values[index];
			set => _values[index] = value;
		}

		public int FindField(string fieldName)
		{
			if (fieldName == null) return -1;
			return Array.FindIndex(_fieldNames, s => string.Equals(s, fieldName,
				                       StringComparison.OrdinalIgnoreCase));
		}

		public bool Exists(string name)
		{
			if (name == null) return false;
			return FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			int index = FindField(name);
			return index < 0 ? null : _values[index];
		}
	}
}
