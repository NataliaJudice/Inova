using InnovaCore.Services.Interfaces;
using InnovaCore.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InnovaCore.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureIoC(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddScoped<ISolicitacaoService, SolicitacaoService>();
            services.AddScoped<ITarefaService, TarefaService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ITemaService, TemaService>();
            services.AddScoped<ISetorService, SetorService>();
            services.AddTransient<IEmailServices, EmailServices>();

            return services;
        }
    }
}