using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Serilog;

[SetUpFixture]
public class OneTimeTestServerSetup
{
	private static IHost _testServer;
	internal static HttpClient Client;

	[OneTimeSetUp]
	public async Task Before()
	{
		_testServer = await TestServerBuilder.StartAsync();
		Client = _testServer.GetTestClient();
	}

	[OneTimeTearDown]
	public async Task After()
	{
		await _testServer?.StopAsync();
		_testServer?.Dispose();
		Client?.Dispose();
	}

	private static IHostBuilder TestServerBuilder => new HostBuilder()
		.UseSerilog()
		.ConfigureWebHost(webBuilder => webBuilder
			.UseTestServer()
			.UseConfiguration(new ConfigurationBuilder()
				.AddInMemoryCollection(ConfigurationValues)
				.Build()
			)
			.UseStartup<Host.Startup>()
			.ConfigureTestServices(services =>
			{
				//
			}));

	private static Dictionary<string, string> ConfigurationValues => new Dictionary<string, string>
	{
	};
}
