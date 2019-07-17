using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Orleans;
using Orleans.Hosting;
using System.Threading.Tasks;

namespace TestHost
{
    class Program
    {
        static Task Main(string[] args)
        {
            Console.Title = nameof(TestHost);

            return new HostBuilder()
                .UseOrleans(builder =>
                {
                    builder
                        .UseLocalhostClustering();
                        //.ConfigureApplicationParts(manager =>
                        //{
                        //    manager.AddApplicationPart(typeof(GameGrain).Assembly).WithReferences();
                        //});
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                })
                .RunConsoleAsync();
        }
    }
}
