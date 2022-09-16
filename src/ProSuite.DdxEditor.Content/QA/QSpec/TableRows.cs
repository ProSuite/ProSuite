using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	internal static class TableRows
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull]
		public static List<QualitySpecificationTableRow> GetQualitySpecifications(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] QualityCondition qualityCondition,
			[CanBeNull] DdxModel model = null)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			Stopwatch stopWatch = _msg.DebugStartTiming();

			return modelBuilder.ReadOnlyTransaction(
				() =>
				{
					List<QualitySpecificationTableRow> result =
						GetQualitySpecificationsTx(modelBuilder, model)
							.Select(qs => new QualitySpecificationTableRow(qs)
							              {
								              Selectable = ! qs.Contains(qualityCondition)
							              })
							.ToList();

					_msg.DebugStopTiming(stopWatch, "Read {0} quality specification(s)",
					                     result.Count);

					return result;
				});
		}

		[NotNull]
		public static List<QualitySpecificationTableRow> GetQualitySpecificationTableRows(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[CanBeNull] ICollection<QualitySpecification> nonSelectable,
			[CanBeNull] DdxModel model = null)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			Stopwatch stopWatch = _msg.DebugStartTiming();

			return modelBuilder.ReadOnlyTransaction(
				() =>
				{
					List<QualitySpecificationTableRow> result =
						GetQualitySpecificationsTx(modelBuilder, model)
							.Select(qspec => new QualitySpecificationTableRow(qspec)
							                 {
								                 Selectable = nonSelectable == null ||
								                              ! nonSelectable.Contains(qspec)
							                 })
							.ToList();

					_msg.DebugStopTiming(stopWatch, "Read {0} quality specification(s)",
					                     result.Count);

					return result;
				}
			);
		}

		[NotNull]
		private static IEnumerable<QualitySpecification> GetQualitySpecificationsTx(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder, [CanBeNull] DdxModel model)
		{
			IQualitySpecificationRepository repository = modelBuilder.QualitySpecifications;
			if (model == null)
			{
				return repository.GetAll();
			}

			// rearead instead of reattach (not needed here, avoids potential 'different instance' exception)
			DdxModel rereadModel = modelBuilder.Models.Get(model.Id);

			if (rereadModel == null)
			{
				return new List<QualitySpecification>();
			}

			return repository.Get(rereadModel.GetDatasets());
		}
	}
}
