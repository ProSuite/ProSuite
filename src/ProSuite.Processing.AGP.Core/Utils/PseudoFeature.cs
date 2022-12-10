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
			var fields = fieldNames.ToList();

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

		public static PseudoFeature FromDefinition(FeatureClassDefinition definition)
		{
			var shapeField = definition.GetShapeField();
			var fields = definition.GetFields().Select(f => f.Name);

			return new PseudoFeature(fields, shapeField);
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
