using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase
{
	/// <summary>
	/// Utility methods for formatting row values.
	/// </summary>
	public static class RowFormat
	{
		#region Fields

		private const string _envelopeFormat = "{0:0N}x{1:0N} - {2:0N}x{3:0N}";
		private const string _shapeArea = "SHAPE.AREA";
		private const string _shapeEnv = "SHAPE.ENVELOPE";
		private const string _shapeLength = "SHAPE.LENGTH";
		private const string _shapeParts = "SHAPE.PARTCOUNT";
		private const string _shapeType = "SHAPE.TYPE";

		private static readonly Regex _regex = new Regex(@"\{(\w+\.?)*[,:\}]",
		                                                 RegexOptions.Compiled);

		#endregion

		#region Public methods

		/// <summary>
		/// Formats the specified gdb object using a default format.
		/// </summary>
		/// <param name="obj">The gdb object to format.</param>
		/// <param name="includeClassAlias">if set to <c>true</c> the class alias is included in 
		/// the default format, otherwise only subtype and oid.</param>
		/// <returns>Formatted gdb object information</returns>
		[NotNull]
		public static string Format([NotNull] IObject obj, bool includeClassAlias = false)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			string format = FieldDisplayUtils.GetDefaultRowFormat(obj.Class,
				includeClassAlias);

			return Format(format, obj);
		}

		/// <summary>
		/// Formats the specified gdb object.
		/// <seealso cref="Format(string, IRow, string)"/>
		/// </summary>
		/// <param name="format">The format.</param>
		/// <param name="obj">The gdb object.</param>
		/// <param name="nullValueText">The null value text (optional).</param>
		/// <returns>Formatted gdb object information</returns>
		[NotNull]
		public static string Format([NotNull] string format, [NotNull] IObject obj,
		                            [CanBeNull] string nullValueText = null)
		{
			return Format(format, (IRow) obj, nullValueText);
		}

		/// <summary>
		/// Formats the specified row.
		/// </summary>
		/// <param name="format">The format.</param>
		/// <param name="row">The row.</param>
		/// <param name="nullValueText">The null value text (optional).</param>
		/// <returns>Formatted row information</returns>
		/// <example>Example format: <para/>
		/// <c>oid={OBJECTID} type={OBJEKTART} length={SHAPE_LENGTH:N0} parts={SHAPE.PARTCOUNT} area={SHAPE.AREA:N1}</c>
		/// </example>
		[NotNull]
		public static string Format([NotNull] string format, [NotNull] IRow row,
		                            [CanBeNull] string nullValueText = null)
		{
			IList<string> rowValueNames = GetValueNames(format);

			string endFormat = PrepareEndFormat(format, rowValueNames);

			object[] args = GetRowValues(row, rowValueNames, nullValueText);

			return string.Format(endFormat, args);
		}

		/// <summary>
		/// Formats the specified row allowing to inject actual display value by field.
		/// </summary>
		/// <param name="format">The format.</param>
		/// <param name="row">The row.</param>
		/// <param name="renderValue">An (optional) method that provides the display value from the row for a specific field name.</param>
		/// <returns>Formatted row information</returns>
		/// <example>Example format: <para/>
		/// <c>oid={OBJECTID} type={OBJEKTART} length={SHAPE_LENGTH:N0} parts={SHAPE.PARTCOUNT} area={SHAPE.AREA:N1}</c>
		/// </example>
		[NotNull]
		public static string Format(
			[NotNull] string format,
			[NotNull] IRow row,
			[CanBeNull] Func<IRow, string, object> renderValue)
		{
			IList<string> rowValueNames = GetValueNames(format);

			string endFormat = PrepareEndFormat(format, rowValueNames);

			object[] args = GetRowValues(row, rowValueNames, null, renderValue);

			return string.Format(endFormat, args);
		}

		/// <summary>
		/// Formats all gdb objects in the specified list.
		/// </summary>
		/// <param name="format">The row format.</param>
		/// <param name="objects">The objects to be formatted.</param>
		/// <returns>The list of formatted strings.</returns>
		[NotNull]
		public static IList<string> Format([CanBeNull] string format,
		                                   [NotNull] IEnumerable<IObject> objects)
		{
			var result = new List<string>();

			foreach (IObject obj in objects)
			{
				result.Add(string.IsNullOrEmpty(format)
					           ? GetDisplayValue(obj)
					           : Format(format, obj));
			}

			return result;
		}

		/// <summary>
		/// Formats all gdb objects in the specified list.
		/// </summary>
		/// <param name="format">The row format.</param>
		/// <param name="fieldValues">The objects to be formatted by replacing their respective key.</param>
		/// <returns>The list of formatted strings.</returns>
		[NotNull]
		public static string Format(
			[NotNull] string format,
			[NotNull] ICollection<KeyValuePair<string, object>> fieldValues)
		{
			IList<string> rowValueNames = new List<string>(fieldValues.Select(pair => pair.Key));

			string endFormat;
			try
			{
				endFormat = PrepareEndFormat(format, rowValueNames);
			}
			catch (ArgumentOutOfRangeException e)
			{
				throw new ArgumentOutOfRangeException(
					string.Format("One or more names ({0}) not found in {1}.",
					              StringUtils.Concatenate(rowValueNames, ", "), format), e);
			}

			object[] args = fieldValues.Select(pair => pair.Value).ToArray();

			return string.Format(endFormat, args);
		}

		/// <summary>
		/// Describes the given envelope
		/// </summary>
		/// <param name="envelope">The envelope to describe</param>
		/// <returns>Describing string of the envelope</returns>
		[NotNull]
		public static string FormatEnvelope([NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			return string.Format(_envelopeFormat,
			                     envelope.XMin, envelope.YMin,
			                     envelope.XMax, envelope.YMax);
		}

		/// <summary>
		/// Describes the given object in a simple format.
		/// ("[ClassName] - ID: [id]")
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetDisplayValue([NotNull] IObject obj)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			var className = "Unknown class";
			var id = "no ID";

			IObjectClass objectClass = obj.Class;

			if (objectClass != null)
			{
				string aliasName = DatasetUtils.GetAliasName(objectClass);
				if (aliasName.Length > 0)
				{
					className = aliasName;
				}
				else
				{
					var dataset = objectClass as IDataset;
					if (dataset != null)
					{
						className = dataset.Name;
					}
				}
			}

			if (obj.HasOID)
			{
				id = obj.OID.ToString(CultureInfo.CurrentCulture);
			}

			return string.Format("{0} - ID: {1}", className, id);
		}

		#endregion

		#region Private methods

		/// <summary>
		/// Gets all value name out of the formating string
		/// The value name is the part between '{' and a [',' or ':' or '}']
		/// </summary>
		/// <param name="format">Format that holds value names</param>
		/// <returns>List of the value names found in the format</returns>
		[NotNull]
		private static IList<string> GetValueNames([NotNull] string format)
		{
			var result = new List<string>();

			// TODO revise: static? precompiled?
			MatchCollection matches = _regex.Matches(format);

			foreach (Match match in matches)
			{
				string matchedValue = match.Value;

				result.Add(matchedValue.Substring(1, matchedValue.Length - 2));
			}

			return result;
		}

		/// <summary>
		/// Replaces the found valueName <see cref=" GetValueNames(string)"/> by
		/// a ordering number.
		/// </summary>
		/// <param name="format">Original format</param>
		/// <param name="valueNames">List with value names</param>
		/// <returns>New format useable for string.format functions</returns>
		/// <example>
		/// <b>Format: </b> "{VALUE_1:N0} some text {VALUE_2}"<br/>
		/// <b>Result: </b> "{0:N0} some text {1}"<para/>
		/// </example>
		[NotNull]
		private static string PrepareEndFormat([NotNull] string format,
		                                       [NotNull] IEnumerable<string> valueNames)
		{
			string endFormat = format;

			var valueCount = 0;
			foreach (string valueName in valueNames)
			{
				// Replace value names including curly braces to avoid replacing identical parts of other fields (e.g. VALUE in VALUE_OLD)
				string searchString = $"{{{valueName}}}";
				string replaceString = $"{{{valueCount.ToString(CultureInfo.InvariantCulture)}}}";

				endFormat = StringUtils.Replace(endFormat, searchString,
				                                replaceString,
				                                StringComparison.InvariantCultureIgnoreCase);
				valueCount++;
			}

			return endFormat;
		}

		/// <summary>
		/// Gets the values for the row
		/// </summary>
		/// <param name="row">Row holding the values</param>
		/// <param name="rowValueNames">Names of the attribute or shape property needed
		/// from the row</param>
		/// <param name="nullValueText">The null value text (optional).</param>
		/// <param name="renderValue">An (optional) method to obtain the display value.</param>
		/// <returns>
		/// An array of objects useable for string.format
		/// </returns>
		[NotNull]
		private static object[] GetRowValues(
			[NotNull] IRow row,
			[NotNull] ICollection<string> rowValueNames,
			[CanBeNull] string nullValueText,
			[CanBeNull] Func<IRow, string, object> renderValue = null)
		{
			var values = new object[rowValueNames.Count];

			var valueIndex = 0;
			foreach (string valueName in rowValueNames)
			{
				values[valueIndex] = GetRowValue(row, valueName, nullValueText, renderValue);
				valueIndex++;
			}

			return values;
		}

		/// <summary>
		/// Gets one value for the given row
		/// </summary>
		/// <param name="row">Row holding the value</param>
		/// <param name="valueName">Name of the attribute or shape property need
		/// from the row</param>
		/// <param name="nullValueText">The null value text (optional).</param>
		/// <param name="renderValue">An (optional) method to obtain the display value.</param>
		/// <returns>An object representing the value</returns>
		[CanBeNull]
		private static object GetRowValue([NotNull] IRow row,
		                                  [NotNull] string valueName,
		                                  [CanBeNull] string nullValueText,
		                                  [CanBeNull] Func<IRow, string, object> renderValue)
		{
			object value = string.Format("Value [{0}] does not exist", valueName);

			if (! GetShapeValue(row, valueName, ref value))
			{
				if (renderValue != null)
				{
					value = renderValue(row, valueName);
				}
				else
				{
					int fieldIndex = row.Fields.FindField(valueName);
					if (fieldIndex > -1)
					{
						value = GdbObjectUtils.GetDisplayValue((IObject) row, fieldIndex) ??
						        nullValueText;
					}
				}
			}

			return value;
		}

		/// <summary>
		/// Gets the different kind of shape property values
		/// </summary>
		/// <param name="row">Row holding the shape</param>
		/// <param name="valueName">Name of the shape properties (see constant fields)</param>
		/// <param name="value">The value for the given property</param>
		/// <returns>TRUE if the valueName matches an shape property name, FALSE
		/// otherwise (value = null)</returns>
		private static bool GetShapeValue([NotNull] IRow row,
		                                  [NotNull] string valueName,
		                                  ref object value)
		{
			if (valueName.Equals(_shapeArea))
			{
				value = GetShapeArea(row);
				return true;
			}

			if (valueName.Equals(_shapeEnv))
			{
				value = GetShapeEnvelope(row);
				return true;
			}

			if (valueName.Equals(_shapeLength))
			{
				value = GetShapeLength(row);
				return true;
			}

			if (valueName.Equals(_shapeParts))
			{
				value = GetShapePartsCount(row);
				return true;
			}

			if (valueName.Equals(_shapeType))
			{
				if (row is IFeature)
				{
					IGeometry shape = ((IFeature) row).Shape;
					value = Format(shape.GeometryType);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the shape area.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>Shape area value as object</returns>
		[NotNull]
		private static object GetShapeArea([NotNull] IRow row)
		{
			object value = "No shape area";

			if (row is IFeature)
			{
				var polygon = (IPolygon) ((IFeature) row).Shape;
				value = polygon.GetArea();
			}

			return value;
		}

		/// <summary>
		/// Gets the shape envelope.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>Shape envelope value as object</returns>
		[NotNull]
		private static object GetShapeEnvelope([NotNull] IRow row)
		{
			object value = "No envelope";

			if (row is IFeature)
			{
				IEnvelope envelope = ((IFeature) row).Shape.Envelope;
				value = FormatEnvelope(envelope);
			}

			return value;
		}

		/// <summary>
		/// Gets the length of the shape.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>Shape length value as object</returns>
		[NotNull]
		private static object GetShapeLength([NotNull] IRow row)
		{
			object value = "No shape length";

			if (row is IFeature)
			{
				var curve = (IPolycurve) ((IFeature) row).Shape;
				value = curve.Length;
			}

			return value;
		}

		/// <summary>
		/// Gets the shape parts count.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>Shape part count value as object</returns>
		[NotNull]
		private static object GetShapePartsCount([NotNull] IRow row)
		{
			object value = "No shape parts";

			if (row is IFeature)
			{
				var geoCollection = (IGeometryCollection) ((IFeature) row).Shape;
				value = geoCollection.GeometryCount;
			}

			return value;
		}

		#endregion

		// TODO: Move to GeometryUtils
		/// <summary>
		/// Translates the given geometryType enum value to an human readable form.
		/// </summary>
		[NotNull]
		public static string Format(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryLine:
					return "Line";

				case esriGeometryType.esriGeometryPoint:
					return "Point";

				case esriGeometryType.esriGeometryMultipoint:
					return "Multi-Point";

				case esriGeometryType.esriGeometryPolygon:
					return "Polygon";

				case esriGeometryType.esriGeometryPolyline:
					return "Polyline";

				case esriGeometryType.esriGeometryEnvelope:
					return "Envelope";

				case esriGeometryType.esriGeometryAny:
					return "Any";

				case esriGeometryType.esriGeometryBag:
					return "Bag";

				case esriGeometryType.esriGeometryBezier3Curve:
					return "Bezier-3-curve";

				case esriGeometryType.esriGeometryCircularArc:
					return "Circular-Arc";

				case esriGeometryType.esriGeometryEllipticArc:
					return "Elliptic-Arc";

				case esriGeometryType.esriGeometryMultiPatch:
					return "Multi-Patch";

				case esriGeometryType.esriGeometryNull:
					return "NULL";

				case esriGeometryType.esriGeometryPath:
					return "Path";

				case esriGeometryType.esriGeometryRay:
					return "Ray";

				case esriGeometryType.esriGeometryRing:
					return "Ring";

				case esriGeometryType.esriGeometrySphere:
					return "Sphere";

				case esriGeometryType.esriGeometryTriangleFan:
					return "Triangle-Fan";

				case esriGeometryType.esriGeometryTriangles:
					return "Triangles";

				case esriGeometryType.esriGeometryTriangleStrip:
					return "Triangle-Strip";

				default:
					return $"Unknown geometry type: {geometryType}";
			}
		}
	}
}
