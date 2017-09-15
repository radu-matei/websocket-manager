using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace ChatApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
			        .UseStartup<Startup>()
			        .Build();
        }
    }
}
