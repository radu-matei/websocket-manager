using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace MvcSample
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
