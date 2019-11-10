using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;

namespace Server_CS
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                .AddBasic(options =>
                {
                    options.AllowInsecureProtocol = true;
                    options.Realm = "idunno";
                    options.Events = new BasicAuthenticationEvents
                    {
                        OnValidateCredentials = context =>
                        {
                            if (context.Username == context.Password)
                            {
                                var claims = new[]
                                {
                                    new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                    new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                };

                                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                                context.Success();
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            services.AddCodeFirstGrpc(config =>
            {
                config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MyCalculator>();
                endpoints.MapGrpcService<MyTimeService>();
            });
        }
    }
}
