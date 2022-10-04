using System;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public interface IAttributeDependencyObserver : IViewObserver
	{
		void EntityBound();

		void DatasetChanged();

		void AddSourceAttributesClicked();

		void RemoveSourceAttributesClicked();

		void AddTargetAttributesClicked();

		void RemoveTargetAttributesClicked();

		void AttributeSelectionChanged();

		void ImportMappingsClicked();

		void ExportMappingsClicked();

		object FormatMappingValue(object value, int columnIndex, Type desiredType);

		object ParseMappingValue(object formattedValue, int columnIndex,
		                         Type desiredType);

		void MappingRowAdded();

		void MappingRowDeleted();

		void MappingValueChanged();
	}
}
