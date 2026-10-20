using System;
using System.Net;
using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using NUnit.Framework;
using ProSuite.Microservices.Client.GrpcNet;

namespace ProSuite.Microservices.Client.AGP.Test.GrpcNet
{
	[TestFixture]
	public class GrpcUtilsTest
	{
		private static readonly Uri _systemProxy = new Uri("http://proxy.example.com:8080/");
		private static readonly Uri _externalAddress = new Uri("https://www.esri.com/");

		private IWebProxy _originalDefaultProxy;

		[SetUp]
		public void SetUp()
		{
			_originalDefaultProxy = HttpClient.DefaultProxy;

			// Stands in for the proxy that Windows (WPAD, PAC file or manual settings) provides:
			HttpClient.DefaultProxy = new WebProxy(_systemProxy);
		}

		[TearDown]
		public void TearDown()
		{
			HttpClient.DefaultProxy = _originalDefaultProxy;
		}

		[Test]
		public void CanCreateLocalhostChannelWithoutChangingProcessProxy()
		{
			using (GrpcChannel channel = GrpcUtils.CreateChannel(
				       "localhost", 5151, ChannelCredentials.Insecure, 1024))
			{
				Assert.NotNull(channel);
			}

			Assert.AreEqual(_systemProxy, HttpClient.DefaultProxy.GetProxy(_externalAddress),
			                "Other connections in the process no longer use the system proxy");
		}

		[Test]
		public void CanCreateChannelOptionsThatBypassProxyForThisChannelOnly()
		{
			GrpcChannelOptions options = GrpcUtils.CreateChannelOptions(1024, disableProxy: true);

			var handler = options.HttpHandler as SocketsHttpHandler;

			Assert.NotNull(handler);
			Assert.IsFalse(handler.UseProxy);
			Assert.IsTrue(options.DisposeHttpClient);

			Assert.AreEqual(_systemProxy, HttpClient.DefaultProxy.GetProxy(_externalAddress),
			                "Other connections in the process no longer use the system proxy");
		}

		[Test]
		public void CanCreateChannelOptionsThatUseSystemProxy()
		{
			GrpcChannelOptions options = GrpcUtils.CreateChannelOptions(1024);

			Assert.IsNull(options.HttpHandler);
		}

		[Test]
		public void CanCreateLoopbackAddressChannelWithoutChangingProcessProxy()
		{
			using (GrpcChannel channel = GrpcUtils.CreateChannel(
				       "127.0.0.1", 5151, ChannelCredentials.Insecure, 1024))
			{
				Assert.NotNull(channel);
			}

			Assert.AreEqual(_systemProxy, HttpClient.DefaultProxy.GetProxy(_externalAddress),
			                "Other connections in the process no longer use the system proxy");
		}
	}
}
