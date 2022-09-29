using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.AttributeDependencies;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public interface IAttributeDependencyView :
		IWrappedEntityControl<AttributeDependency>, IWin32Window
	{
		IAttributeDependencyObserver Observer { get; set; }

		Func<object> FindDatasetDelegate { get; set; }

		void BindToAvailableAttributeRows(IList<AttributeTableRow> rows);

		void BindToSourceAttributeRows(IList<AttributeTableRow> rows);

		void BindToTargetAttributeRows(IList<AttributeTableRow> rows);

		void SetupMappingGrid(IList<AttributeInfo> sourceAttrs,
		                      IList<AttributeInfo> targetAttrs,
		                      string descriptionFieldName);

		void BindToAttributeValueMappings(DataView mappings);

		IList<AttributeTableRow> GetSelectedAvailableAttributes();

		IList<AttributeTableRow> GetSelectedSourceAttributes();

		IList<AttributeTableRow> GetSelectedTargetAttributes();

		int SelectedAvailableAttributeCount { get; }
		int SelectedSourceAttributeCount { get; }
		int SelectedTargetAttributeCount { get; }

		bool AddSourceAttributesEnabled { get; set; }
		bool RemoveSourceAttributesEnabled { get; set; }

		bool AddTargetAttributesEnabled { get; set; }
		bool RemoveTargetAttributesEnabled { get; set; }

		bool ImportMappingsEnabled { get; set; }
		bool ExportMappingsEnabled { get; set; }
	}
}
