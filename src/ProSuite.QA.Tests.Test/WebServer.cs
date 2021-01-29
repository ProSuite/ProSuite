using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	internal class WebServer : IDisposable
	{
		private readonly HttpListener _listener = new HttpListener();
		private readonly Func<HttpListenerRequest, string> _responder;

		public WebServer([NotNull] Func<HttpListenerRequest, string> responder,
		                 params string[] prefixes)
			: this(responder, (ICollection<string>) prefixes) { }

		private WebServer([NotNull] Func<HttpListenerRequest, string> responder,
		                  [NotNull] ICollection<string> prefixes)
		{
			Assert.ArgumentCondition(HttpListener.IsSupported,
			                         "Needs Windows XP SP2, Server 2003 or later");
			Assert.ArgumentNotNull(responder, nameof(responder));

			// URI prefixes are required, for example "http://localhost:8080/index/"
			Assert.ArgumentNotNull(prefixes, nameof(prefixes));
			Assert.ArgumentCondition(prefixes.Count > 0, "Invalid prefix count: {0}",
			                         prefixes.Count);

			foreach (string prefix in prefixes)
			{
				_listener.Prefixes.Add(prefix);
			}

			_responder = responder;
			_listener.Start();
		}

		public void Run()
		{
			ThreadPool.QueueUserWorkItem(
				o =>
				{
					Console.WriteLine(@"Webserver running...");

					while (_listener.IsListening)
					{
						try
						{
							ThreadPool.QueueUserWorkItem(
								c =>
								{
									var context = (HttpListenerContext) c;
									try
									{
										string response = _responder(context.Request);
										byte[] buffer = Encoding.UTF8.GetBytes(response);

										context.Response.ContentLength64 = buffer.Length;
										context.Response.OutputStream.Write(
											buffer, 0, buffer.Length);
									}
									catch (Exception e)
									{
										Console.WriteLine(e.Message);
									}
									finally
									{
										// always close the stream
										context.Response.OutputStream.Close();
									}
								},
								_listener.GetContext());
						}
						catch (HttpListenerException)
						{
							// occurs on final _listener.GetContext(); ignore
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
						}
					}
				});
		}

		private void Stop()
		{
			_listener.Stop();
			_listener.Close();

			Console.WriteLine(@"Webserver shut down");
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
