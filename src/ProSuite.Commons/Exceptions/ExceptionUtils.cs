using System;
using System.Runtime.InteropServices;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Exceptions
{
	public static class ExceptionUtils
	{
		[NotNull]
		public static string FormatMessage([NotNull] Exception exception)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			var sb = new StringBuilder();

			sb.Append(exception.Message);

			var externalException = exception as ExternalException;
			if (externalException != null)
			{
				sb.AppendLine();
				sb.AppendFormat("- Error code: {0}", externalException.ErrorCode);
			}

			if (exception.InnerException != null)
			{
				AppendException(sb, exception.InnerException);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Gets the message of the innermost exception of a given exception.
		/// </summary>
		/// <param name="exception">The exception.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetInnermostMessage([NotNull] Exception exception)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			return exception.InnerException == null
				       ? exception.Message
				       : GetInnermostMessage(exception.InnerException);
		}

		private static void AppendException([NotNull] StringBuilder sb,
		                                    [NotNull] Exception exception)
		{
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendFormat("---> {0}", exception.Message);

			var externalException = exception as ExternalException;
			if (externalException != null)
			{
				sb.AppendLine();
				sb.AppendFormat("- Error code: {0}", externalException.ErrorCode);
			}

			if (exception.InnerException != null)
			{
				AppendException(sb, exception.InnerException);
			}
		}
	}
}