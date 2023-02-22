using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class QualityConditionSearchByNameProvider : ISearchProvider
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public QualityConditionSearchByNameProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;

			Text = "Find Quality Condition by &Name...";
			Image = Resources.Find;
		}

		public string Text { get; }

		public Image Image { get; }

		public Entity SearchEntity(IWin32Window owner)
		{
			using (var form = new FindQualityConditionByNameForm(GetNames(), GetByName))
			{
				var result = form.ShowDialog(owner);

				return result == DialogResult.OK
						   ? form.QualityCondition
						   : null;
			}
		}

		[CanBeNull]
		private QualityCondition GetByName(string name)
		{
			return _modelBuilder.ReadOnlyTransaction(
				() => _modelBuilder.QualityConditions.Get(name));
		}

		[NotNull]
		private IEnumerable<string> GetNames()
		{
			return _modelBuilder.ReadOnlyTransaction(
				() => _modelBuilder.QualityConditions.GetNames(
					_modelBuilder
						.IncludeQualityConditionsBasedOnDeletedDatasets));
		}
	}
}
