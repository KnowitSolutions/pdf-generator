using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Serilog;

[SetUpFixture]
public class OneTimeTestServerSetup
{
	private static TestServer _testServer;
	internal static HttpClient Client;

	[OneTimeSetUp]
	public async Task Before()
	{
		_testServer = new TestServer(TestServerBuilder);
		await _testServer.Host.StartAsync();
		Client = _testServer.CreateClient();

	}

	[OneTimeTearDown]
	public async Task After()
	{
		await _testServer?.Host?.StopAsync();
		_testServer?.Dispose();
		Client?.Dispose();
	}

	private static IWebHostBuilder TestServerBuilder => new WebHostBuilder()
		.UseTestServer()
		.UseConfiguration(new ConfigurationBuilder()
			.AddInMemoryCollection(ConfigurationValues)
			.Build()
		)
		.UseSerilog()
		.UseStartup<Host.Startup>()
		.ConfigureTestServices(services =>
		{
			//
		});

	private static Dictionary<string, string> ConfigurationValues => new Dictionary<string, string>
	{
	};
}
