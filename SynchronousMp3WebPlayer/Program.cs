using Microsoft.Extensions.Hosting.WindowsServices;
using SynchronousMp3WebPlayer;
using SynchronousMp3WebPlayer.Helpers;

var contentRoot = GetContentRoot();
var logsDirectory = Path.Combine(contentRoot, "logs");

try
{
    CreateHostBuilder(args).Build().Run();
}
catch (Exception ex)
{
    Directory.CreateDirectory(logsDirectory);
    File.AppendAllText(GetLogFilePath(logsDirectory),
                       $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [Critical] Application startup failed:{Environment.NewLine}{ex}{Environment.NewLine}");
}

return;

static IHostBuilder CreateHostBuilder(string[] args)
{
    var contentRoot = GetContentRoot();
    var logsDirectory = Path.Combine(contentRoot, "logs");

    Directory.SetCurrentDirectory(contentRoot);

    DotNetEnv.Env.Load(Path.Combine(contentRoot, ".env"));

    return Host.CreateDefaultBuilder(args)
               .UseContentRoot(contentRoot)
               .UseWindowsService(options => { options.ServiceName = "SynchronousMp3WebPlayer"; })
               .ConfigureAppConfiguration((context, builder) =>
                                              builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                                                                  optional: true,
                                                                  reloadOnChange: true))
               .ConfigureLogging((context, logging) =>
                                 {
                                     logging.ClearProviders();
                                     logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                                     logging.AddProvider(new FileLoggerProvider(logsDirectory));
                                 })
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}

static string GetContentRoot()
{
    return WindowsServiceHelpers.IsWindowsService()
               ? AppContext.BaseDirectory
               : Directory.GetCurrentDirectory();
}

static string GetLogFilePath(string logsDirectory)
{
    return Path.Combine(logsDirectory, $"app-{DateTimeOffset.Now:yyyy-MM-dd}.log");
}
