using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class ColumnDescriptor
	{
		private readonly Type _type;
		private readonly PropertyInfo _propertyInfo;
		private readonly string _fieldName;
		private readonly string _headerText;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ColumnDescriptor"/> class.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="headerText">The header text.</param>
		public ColumnDescriptor([NotNull] string fieldName,
		                        [CanBeNull] string headerText = null)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			_fieldName = fieldName;
			_headerText = headerText;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ColumnDescriptor"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="propertyInfo">The property info.</param>
		public ColumnDescriptor([NotNull] Type type, [NotNull] PropertyInfo propertyInfo)
		{
			Assert.ArgumentNotNull(type, nameof(type));
			Assert.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

			_type = type;
			_propertyInfo = propertyInfo;

			_fieldName = propertyInfo.Name;
			_headerText = null;
		}

		#endregion

		[NotNull]
		public static IEnumerable<ColumnDescriptor> GetColumns<T>() where T : class
		{
			Type type = typeof(T);

			foreach (PropertyInfo property in type.GetProperties())
			{
				if (property.GetGetMethod() != null && IsBrowsable(property))
				{
					yield return new ColumnDescriptor(type, property);
				}
			}
		}

		[NotNull]
		public string FieldName => _fieldName;

		[NotNull]
		public DataGridViewColumn CreateColumn<T>() where T : class
		{
			PropertyInfo propertyInfo = GetPropertyInfo(typeof(T));

			Type propertyType = propertyInfo.PropertyType;

			DataGridViewColumn column;
			string defaultHeader = _fieldName;
			if (typeof(Image).IsAssignableFrom(propertyType))
			{
				var imageColumn = new DataGridViewImageColumn();
				column = imageColumn;
				defaultHeader = string.Empty;
			}
			else if (propertyType == typeof(bool))
			{
				var checkBoxColumn = new DataGridViewCheckBoxColumn(false);
				column = checkBoxColumn;
			}
			else
			{
				column = new DataGridViewTextBoxColumn();
			}

			ColumnConfigurationAttribute columnConfiguration =
				GetColumnConfiguration(propertyInfo);

			column.DataPropertyName = _fieldName;
			column.Name = _fieldName;
			column.HeaderText = GetHeaderText(propertyInfo, defaultHeader);
			column.ReadOnly = true;
			column.SortMode = DataGridViewColumnSortMode.Automatic;
			column.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;

			if (columnConfiguration != null)
			{
				column.DefaultCellStyle.Padding = columnConfiguration.Padding;

				if (columnConfiguration.Alignment != DataGridViewContentAlignment.NotSet)
				{
					column.DefaultCellStyle.Alignment = columnConfiguration.Alignment;
				}

				if (columnConfiguration.Width > 0)
				{
					column.Width = columnConfiguration.Width;
					// set AutoSizeMode to None, otherwise Width has no effect
					column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				}

				if (columnConfiguration.AutoSizeColumnMode !=
				    DataGridViewAutoSizeColumnMode.NotSet)
				{
					column.AutoSizeMode = columnConfiguration.AutoSizeColumnMode;
				}

				if (columnConfiguration.WrapMode != TriState.NotSet)
				{
					column.DefaultCellStyle.WrapMode =
						GetDataGridViewTriState(columnConfiguration.WrapMode);
				}

				if (columnConfiguration.MinimumWidth > 0)
				{
					column.MinimumWidth = columnConfiguration.MinimumWidth;
				}
			}

			return column;
		}

		[CanBeNull]
		private static ColumnConfigurationAttribute GetColumnConfiguration(
			[NotNull] MemberInfo propertyInfo)
		{
			Assert.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

			return Attribute.GetCustomAttribute(propertyInfo,
			                                    typeof(ColumnConfigurationAttribute))
				       as ColumnConfigurationAttribute;
		}

		private static DataGridViewTriState GetDataGridViewTriState(TriState triState)
		{
			switch (triState)
			{
				case TriState.NotSet:
					return DataGridViewTriState.NotSet;

				case TriState.True:
					return DataGridViewTriState.True;

				case TriState.False:
					return DataGridViewTriState.False;

				default:
					throw new ArgumentOutOfRangeException(nameof(triState));
			}
		}

		private PropertyInfo GetPropertyInfo(Type type)
		{
			PropertyInfo propertyInfo;

			if (_type != null)
			{
				Assert.AreEqual(type, _type, "Type mismatch");
				Assert.NotNull(_propertyInfo, "_propertyInfo is null");

				propertyInfo = _propertyInfo;
			}
			else
			{
				propertyInfo = type.GetProperty(_fieldName);

				if (propertyInfo == null)
				{
					throw new InvalidOperationException(
						string.Format("Property {0} not found on class {1}", _fieldName,
						              type.Name));
				}
			}

			return propertyInfo;
		}

		#region Non-public members

		private string GetHeaderText(MemberInfo property, string defaultText)
		{
			if (_headerText != null)
			{
				return _headerText;
			}

			var attribute =
				Attribute.GetCustomAttribute(property, typeof(DisplayNameAttribute))
					as DisplayNameAttribute;

			if (attribute != null &&
			    ! string.IsNullOrEmpty(attribute.DisplayName))
			{
				return attribute.DisplayName;
			}

			return defaultText;
		}

		private static bool IsBrowsable(MemberInfo memberInfo)
		{
			Assert.ArgumentNotNull(memberInfo, nameof(memberInfo));

			var attribute =
				Attribute.GetCustomAttribute(memberInfo, typeof(BrowsableAttribute))
					as BrowsableAttribute;

			if (attribute != null)
			{
				return attribute.Browsable;
			}

			return true;
		}

		#endregion
	}
}
