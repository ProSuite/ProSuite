using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.DependencyGraph;
using ProSuite.DomainModel.Core.QA.DependencyGraph.GraphML;
using ProSuite.DomainModel.Core.QA.Xml;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public static class QualitySpecificationsItemUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static void ExportDatasetDependencies(
			[NotNull] ICollection<KeyValuePair<string, ICollection<QualitySpecification>>>
				qualitySpecificationsByFileName,
			[NotNull] IEnumerable<string> deletableFiles,
			[NotNull] ExportDatasetDependenciesOptions options,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
		{
			Assert.ArgumentNotNull(qualitySpecificationsByFileName,
			                       nameof(qualitySpecificationsByFileName));
			Assert.ArgumentNotNull(deletableFiles, nameof(deletableFiles));

			HashSet<string> deletableFilesSet = GetDeletableFilesSet(deletableFiles);

			using (new WaitCursor())
			{
				modelBuilder.UseTransaction(
					delegate
					{
						foreach (
							KeyValuePair<string, ICollection<QualitySpecification>> pair in
							qualitySpecificationsByFileName)
						{
							string fileName = pair.Key;
							List<QualitySpecification> qualitySpecifications = pair.Value.ToList();

							PrepareFileLocation(fileName, deletableFilesSet);

							ExportDependencyGraph(qualitySpecifications, fileName, options);

							LogSuccessfulDependencyExport(qualitySpecifications, fileName);
						}
					});
			}
		}

		public static void ExportQualitySpecifications(
			[NotNull] IDictionary<string, ICollection<QualitySpecification>>
				qualitySpecificationsByFileName,
			[NotNull] IEnumerable<string> deletableFiles,
			bool exportMetadata,
			bool? exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
		{
			Assert.ArgumentNotNull(qualitySpecificationsByFileName,
			                       nameof(qualitySpecificationsByFileName));
			Assert.ArgumentNotNull(deletableFiles, nameof(deletableFiles));
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			HashSet<string> deletableFilesSet = GetDeletableFilesSet(deletableFiles);

			using (new WaitCursor())
			{
				modelBuilder.UseTransaction(
					delegate
					{
						foreach (
							KeyValuePair<string, ICollection<QualitySpecification>> pair in
							qualitySpecificationsByFileName)
						{
							string fileName = pair.Key;
							List<QualitySpecification> qualitySpecifications = pair.Value.ToList();

							PrepareFileLocation(fileName, deletableFilesSet);

							modelBuilder.DataQualityExporter.Export(qualitySpecifications,
								fileName,
								exportMetadata,
								exportWorkspaceConnections,
								exportConnectionFilePaths,
								exportAllDescriptors,
								exportAllCategories,
								exportNotes);

							LogSuccessfulExport(qualitySpecifications, fileName);
						}
					});
			}
		}

		public static void ImportQualitySpecifications(
			[NotNull] string fileName,
			bool ignoreConditionsForUnknownDatasets,
			bool updateTestDescriptorNames,
			bool updateTestDescriptorProperties,
			[NotNull] IXmlDataQualityImporter importer)
		{
			using (new WaitCursor())
			{
				IList<QualitySpecification> imported;

				using (_msg.IncrementIndentation(
					       "Importing all quality specifications from {0}", fileName))
				{
					imported = importer.Import(
						fileName, QualitySpecificationImportType.UpdateOrAdd,
						ignoreConditionsForUnknownDatasets,
						updateTestDescriptorNames,
						updateTestDescriptorProperties);
				}

				// TODO report stats (inserted, updated qcons and testdescs)
				var sb = new StringBuilder();
				sb.AppendFormat("Quality specifications imported from {0}", fileName);
				sb.AppendLine();

				foreach (QualitySpecification specification in imported)
				{
					sb.AppendFormat("- {0} ({1})", specification.Name,
					                GetElementCountMessage(
						                specification));
					sb.AppendLine();
				}

				_msg.Info(sb);
			}
		}

		[NotNull]
		public static string GetElementCountMessage(
			[NotNull] QualitySpecification qualitySpecification)
		{
			int elementCount = qualitySpecification.Elements.Count;
			return elementCount == 1
				       ? "1 quality condition"
				       : string.Format("{0} quality conditions",
				                       elementCount);
		}

		private static void ExportDependencyGraph(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			[NotNull] string fileName,
			[NotNull] ExportDatasetDependenciesOptions options)
		{
			DatasetDependencyGraph graph = DependencyGraphUtils.GetGraph(
				qualitySpecifications,
				options.ExportBidirectionalDependenciesAsUndirectedEdges,
				options.IncludeSelfDependencies);

			graphmltype document = GraphMLUtils.GetGraphMLDocument(graph,
				options
					.ExportModelsAsParentNodes);

			XmlUtils.Serialize(document, fileName);
		}

		[NotNull]
		private static HashSet<string> GetDeletableFilesSet(
			[NotNull] IEnumerable<string> deletableFiles)
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string file in deletableFiles)
			{
				result.Add(file.Trim());
			}

			return result;
		}

		private static void PrepareFileLocation([NotNull] string fileName,
		                                        [NotNull] ICollection<string> deletableFiles)
		{
			if (File.Exists(fileName) && deletableFiles.Contains(fileName.Trim()))
			{
				File.Delete(fileName);
			}

			string directoryName = Path.GetDirectoryName(fileName);

			if (StringUtils.IsNotEmpty(directoryName) && ! Directory.Exists(directoryName))
			{
				// fails if there are invalid characters etc.
				Directory.CreateDirectory(directoryName);
			}
		}

		private static void LogSuccessfulExport(
			[NotNull] IList<QualitySpecification> qualitySpecifications,
			[NotNull] string fileName)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			if (qualitySpecifications.Count == 1)
			{
				QualitySpecification qspec = qualitySpecifications[0];
				_msg.InfoFormat("Exported '{0}' ({1}) to {2}",
				                qspec.Name, GetElementCountMessage(qspec), fileName);
			}
			else
			{
				var sb = new StringBuilder();
				sb.AppendFormat("{0} quality specifications exported to {1}",
				                qualitySpecifications.Count, fileName);
				sb.AppendLine();

				foreach (QualitySpecification qualitySpecification in qualitySpecifications)
				{
					sb.AppendFormat("- {0} ({1})", qualitySpecification.Name,
					                GetElementCountMessage(qualitySpecification));
					sb.AppendLine();
				}

				_msg.Info(sb);
			}
		}

		private static void LogSuccessfulDependencyExport(
			[NotNull] IList<QualitySpecification> qualitySpecifications,
			[NotNull] string fileName)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			if (qualitySpecifications.Count == 1)
			{
				QualitySpecification qspec = qualitySpecifications[0];
				_msg.InfoFormat("Exported dataset dependencies for '{0}' ({1}) to {2}",
				                qspec.Name, GetElementCountMessage(qspec), fileName);
			}
			else
			{
				var sb = new StringBuilder();
				sb.AppendFormat(
					"Dataset dependencies for {0} quality specifications exported to {1}",
					qualitySpecifications.Count, fileName);
				sb.AppendLine();

				foreach (QualitySpecification qualitySpecification in qualitySpecifications)
				{
					sb.AppendFormat("- {0} ({1})", qualitySpecification.Name,
					                GetElementCountMessage(qualitySpecification));
					sb.AppendLine();
				}

				_msg.Info(sb);
			}
		}
	}
}
