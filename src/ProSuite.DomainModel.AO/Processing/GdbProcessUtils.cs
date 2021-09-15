using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.DomainModel.Core.Processing.Reporting;

namespace ProSuite.DomainModel.AO.Processing
{
	public static class GdbProcessUtils
	{
		#region Process parameters

		[NotNull]
		public static IList<PropertyInfo> GetProcessParameters([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (! IsGdbProcessType(type))
			{
				throw new InvalidOperationException(
					$"Type {type.Name} is not a GdbProcess type");
			}

			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			           .Where(GdbProcessCoreUtils.IsProcessParameter)
			           .OrderBy(GdbProcessCoreUtils.GetParameterOrder)
			           .ToList();
		}

		public static string GetParameterDescription([NotNull] IGdbProcess process)
		{
			Assert.ArgumentNotNull(process, nameof(process));

			var sb = new StringBuilder();

			Type type = process.GetType();

			sb.AppendFormat("GdbProcess Parameters (Name: {0}, Type: {1}):",
			                process.Name, type.Name);

			foreach (PropertyInfo property in GetProcessParameters(type))
			{
				sb.AppendLine();
				sb.AppendFormat("  {0} = {1}", property.Name, property.GetValue(process, null));
			}

			return sb.ToString();
		}

		private static bool IsGdbProcessType(Type candidateType)
		{
			if (candidateType == null) return false;

			Type testType = typeof(IGdbProcess);

			return testType.IsAssignableFrom(candidateType) &&
			       candidateType.IsPublic && ! candidateType.IsAbstract;
		}

		#endregion

		public static void WriteProcessReport([NotNull] IList<Assembly> assemblies,
		                                      [NotNull] string htmlFileName)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			using (Stream stream = new FileStream(htmlFileName, FileMode.Create))
			{
				IProcessReportBuilder builder =
					new HtmlProcessReportBuilder("ProSuite Carto Process Documentation");

				builder.AddHeaderItem("ProSuite Version",
				                      ReflectionUtils.GetAssemblyVersionString(
					                      Assembly.GetExecutingAssembly()));

				builder.IncludeObsolete = true;
				builder.IncludeAssemblyInfo = true;

				foreach (Assembly assembly in assemblies)
				{
					foreach (Type type in assembly.GetTypes())
					{
						if (IsGdbProcessType(type))
						{
							builder.AddProcessType(type);
						}
					}
				}

				builder.WriteReport(stream);
			}
		}

		public static bool Execute([NotNull] IProcessingContext context,
		                           [NotNull] IProcessingFeedback feedback,
		                           [NotNull] IEnumerable<IGdbProcess> processes,
		                           [CanBeNull] string actionName)
		{
			Assert.ArgumentNotNull(context, nameof(context));
			Assert.ArgumentNotNull(feedback, nameof(feedback));
			Assert.ArgumentNotNull(processes, nameof(processes));

			IGdbTransaction transaction = context.GetTransaction();
			IWorkspace workspace = context.GetWorkspace();

			// TODO - Consider: transient processes: execute a list of ProcessDescriptors;
			// TODO - for each descriptor, instantiate and configure a GdbProcess.

			return transaction.Execute(
				workspace,
				() => Execute(context, feedback, processes),
				actionName ?? GetActionName(processes));
		}

		public static string FormatProcessingStopped()
		{
			return "Processing stopped. If in an edit session " +
			       "use 'Undo' to discard all processed results.";
		}

		public static string FormatProcessingCompleted(int errors, int warnings,
		                                               TimeSpan? duration = null)
		{
			var sb = new StringBuilder("Processing completed");

			if (duration.HasValue)
			{
				sb.Append(" in ");
				sb.AppendDuration(duration.Value);
			}

			if (errors > 0)
			{
				bool one = errors == 1;
				sb.AppendFormat(" - there {0} been {1} error{2}",
				                one ? "has" : "have", errors, one ? "" : "s");
			}
			else if (warnings > 0)
			{
				bool one = warnings == 1;
				sb.AppendFormat(" - there {0} been {1} warning{2}",
				                one ? "has" : "have", warnings, one ? "" : "s");
			}

			return sb.ToString();
		}

		#region Private utils

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static void Execute([NotNull] IProcessingContext context,
		                            [NotNull] IProcessingFeedback feedback,
		                            [NotNull] IEnumerable<IGdbProcess> processes)
		{
			int current = 0, total = processes.Count();

			try
			{
				foreach (IGdbProcess process in processes)
				{
					current += 1;

					if (feedback.CancellationPending)
					{
						throw new OperationCanceledException();
					}

					ReportProcessStarting(feedback, process, current, total);

					if (process is IGroupGdbProcess)
					{
						feedback.CurrentGroup = process.Name;
						feedback.CurrentProcess = null;
					}
					else
					{
						feedback.CurrentGroup = null;
						feedback.CurrentProcess = process.Name;

						_msg.Debug(GetParameterDescription(process));
					}

					try
					{
						DateTime startTime = DateTime.Now;

						using (_msg.IncrementIndentation())
						{
							process.Execute(context, feedback);
						}

						TimeSpan duration = DateTime.Now - startTime;
						ReportProcessCompleted(feedback, process, duration);
					}
					catch (OperationCanceledException)
					{
						throw; // rethrow (but catch all other exceptions)
					}
					catch (Exception ex)
					{
						feedback.ReportError(
							string.Format("Error executing {0} {1}: {2}",
							              process is IGroupGdbProcess
								              ? "Process Group"
								              : "GdbProcess", process.Name,
							              ex.Message), ex);
					}
				}

				feedback.ReportCompleted();
			}
			catch (OperationCanceledException)
			{
				feedback.ReportStopped();
			}
		}

		private static string GetActionName([NotNull] IEnumerable<IGdbProcess> processes)
		{
			List<IGdbProcess> processList = processes.ToList();

			if (processList.Count == 1)
			{
				IGdbProcess process = processList[0];
				return string.Format("Execute {0} '{1}'", process is IGroupGdbProcess
					                                          ? "Process Group"
					                                          : "GdbProcess", process.Name);
			}

			return string.Format("Execute {0} GdbProcesses", processList.Count);
		}

		private static void ReportProcessStarting([NotNull] IProcessingFeedback feedback,
		                                          [NotNull] IGdbProcess process,
		                                          int current, int total)
		{
			var sb = new StringBuilder();

			sb.Append("Executing ");
			sb.Append(process is IGroupGdbProcess ? "Process Group" : "GdbProcess");
			sb.AppendFormat(" '{0}'", process.Name);

			if (total > 1 && 0 < current && current <= total)
			{
				sb.AppendFormat(" ({0} of {1})", current, total);
			}

			feedback.ReportInfo(sb.ToString());
		}

		private static void ReportProcessCompleted([NotNull] IProcessingFeedback feedback,
		                                           [NotNull] IGdbProcess process,
		                                           TimeSpan duration)
		{
			var sb = new StringBuilder();

			string what = process is IGroupGdbProcess ? "Process Group" : "GdbProcess";

			sb.AppendFormat("{0} '{1}' completed in ", what, process.Name);
			sb.AppendDuration(duration);

			feedback.ReportInfo(sb.ToString());
		}

		private static void AppendDuration(this StringBuilder sb, TimeSpan duration)
		{
			if (duration.Days > 1)
			{
				sb.AppendFormat("{0} days ", duration.Days);
			}
			else if (duration.Days == 1)
			{
				sb.Append("1 day ");
			}

			sb.AppendFormat("{0:00}:{1:00}:{2:00}",
			                duration.Hours, duration.Minutes, duration.Seconds);
		}

		#endregion
	}
}
