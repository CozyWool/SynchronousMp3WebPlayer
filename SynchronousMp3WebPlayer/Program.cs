using Microsoft.Extensions.Hosting.WindowsServices;
using SynchronousMp3WebPlayer;

try
{
    CreateHostBuilder(args).Build().Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

return;

static IHostBuilder CreateHostBuilder(string[] args)
{
    var contentRoot = WindowsServiceHelpers.IsWindowsService()
                          ? AppContext.BaseDirectory
                          : Directory.GetCurrentDirectory();

    Directory.SetCurrentDirectory(contentRoot);

    DotNetEnv.Env.Load(Path.Combine(contentRoot, ".env"));

    return Host.CreateDefaultBuilder(args)
               .UseContentRoot(contentRoot)
               .UseWindowsService(options => { options.ServiceName = "SynchronousMp3WebPlayer"; })
               .ConfigureAppConfiguration((context, builder) =>
                                              builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                                                                  optional: true,
                                                                  reloadOnChange: true))
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
