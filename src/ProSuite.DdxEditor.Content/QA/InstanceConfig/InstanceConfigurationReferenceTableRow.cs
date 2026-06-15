using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class InstanceConfigurationReferenceTableRow : IEntityRow
	{
		public InstanceConfigurationReferenceTableRow(
			[NotNull] InstanceConfiguration instanceConfig)
		{
			Assert.ArgumentNotNull(instanceConfig, nameof(instanceConfig));

			InstanceConfig = instanceConfig;

			Name = instanceConfig.Name;

			Image = TestTypeImageLookup.GetImage(instanceConfig);
			Image.Tag = TestTypeImageLookup.GetDefaultSortIndex(instanceConfig);

			if (instanceConfig.Category != null)
			{
				Category = instanceConfig.Category.GetQualifiedName();
			}
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		public Image Image { get; }

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string Type
		{
			get
			{
				switch (InstanceConfig)
				{
					case QualityCondition _:
						return "Quality Condition";
					case TransformerConfiguration _:
						return "Transformer";
					case IssueFilterConfiguration _:
						return "Issue Filter";
					default:
						throw new InvalidOperationException(
							$"Unknown configuration type: {InstanceConfig}");
				}
			}
		}

		[UsedImplicitly]
		public string Category { get; private set; }

		[UsedImplicitly]
		public string AlgorithmImplementation => InstanceConfig.InstanceDescriptor.Name;

		[UsedImplicitly]
		public string Description => InstanceConfig.Description;

		[Browsable(false)]
		[NotNull]
		public InstanceConfiguration InstanceConfig { get; private set; }

		Entity IEntityRow.Entity => InstanceConfig;
	}
}
