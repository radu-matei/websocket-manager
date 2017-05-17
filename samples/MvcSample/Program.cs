using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace MvcSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}


/*
 * 
 * 
 * 
 *           .UseKestrel(c =>
          {
            c.AddServerHeader = false;
            c.NoDelay = true;
             // c.ThreadCount = 1000;
           })
          .UseUrls("http://*:9013")
          .UseContentRoot(Directory.GetCurrentDirectory())
          .UseIISIntegration()
          .UseStartup<Startup>()
*/