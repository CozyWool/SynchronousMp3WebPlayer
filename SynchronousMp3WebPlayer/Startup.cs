

using SynchronousMp3WebPlayer.Hubs;

namespace SynchronousMp3WebPlayer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR(options =>
                            {
                                options.MaximumReceiveMessageSize = 1024 * 1024 * 20;
                            });
        services.AddHttpContextAccessor();
        services.AddMvc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
                         {
                             endpoints.MapControllerRoute("default",
                                                          "{controller=Music}/{action=Index}");
                             endpoints.MapHub<MusicHub>("/musicHub");
                         });
    }
}