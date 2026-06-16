using InnovaCore.Data.Context;
using InnovaCore.Services.Interfaces;
using InnovaCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace InnovaCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly InnovationCoreDbContext _context;

        public HomeController(IDashboardService dashboardService, InnovationCoreDbContext context)
        {
            _dashboardService = dashboardService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Hub()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult HubAdmin()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Comando(string periodo = "6meses")
        {
            try
            {
                // 1. Define a data limite baseada no filtro selecionado
                DateTime dataLimite = ObterDataLimite(periodo);

                // 2. Busca as quantidades gerais lendo diretamente da tabela de Tarefas
                var qtdes = await ObterMetricasGeraisAsync(dataLimite);

                // 3. Busca a quantidade agrupada por setor fazendo o JOIN correto
                var qtdeporSetor = await ObterPropostasPorSetorAsync(dataLimite);

                var vm = new ViewModelDashboard
                {
                    VwDashboardQtde = qtdes,
                    VwQtdePorSetor = qtdeporSetor
                };

                ViewBag.PeriodoSelecionado = periodo;
                return View(vm);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não foi possível carregar os dados do dashboard no momento.";
                return View(new ViewModelDashboard());
            }
        }

        // Zera as horas para garantir que o filtro de data traga os dados corretamente
        private DateTime ObterDataLimite(string periodo)
        {
            return periodo switch
            {
                "mes" => DateTime.Now.AddMonths(-1).Date,
                "ano" => DateTime.Now.AddYears(-1).Date,
                "6meses" or _ => DateTime.Now.AddMonths(-6).Date
            };
        }

        // Método que realiza o JOIN entre Solicitações e Setores para o gráfico de barras
        private async Task<List<InnovaCore.Domain.Entities.VwQtdePorSetor>> ObterPropostasPorSetorAsync(DateTime dataLimite)
        {
            return await _context.Solicitacoes
                .Where(s => s.Datacadastro >= dataLimite)
                .Join(_context.Setor,
                    solicitacao => solicitacao.IdSetor,
                    setor => setor.Id,
                    (solicitacao, setor) => new { solicitacao, setor })
                .GroupBy(x => x.setor.Nome)
                .Select(g => new InnovaCore.Domain.Entities.VwQtdePorSetor
                {
                    Nome = g.Key ?? "Sem Setor",
                    QTDE_POR_SETOR = g.Count()
                })
                .ToListAsync();
        }

        // CONSERTO REALIZADO: Método agora consulta unicamente a tabela de Tarefas mapeada no banco
        private async Task<InnovaCore.Domain.Entities.VwDashboardQtde> ObterMetricasGeraisAsync(DateTime dataLimite)
        {
            // 1. Aponta para a tabela correta do DbSet: _context.Tarefas
            // Usando a coluna 'Datacadastro' em caixa baixa conforme o print da estrutura
            var queryFiltrada = _context.Tarefas.Where(t => t.Datacadastro >= dataLimite);
            var querySolicitacoesFiltrada = _context.Solicitacoes.Where(t => t.Datacadastro >= dataLimite);

            var total = await queryFiltrada.CountAsync();

            // 2. Filtra usando o campo 'IdTarefaStatus' mapeado com os IDs reais do seu reset (7, 8, 9)
            var pendentes = await querySolicitacoesFiltrada.Where(t => t.IdSolicitacaoStatus == 1).CountAsync();
            var aComecar = await queryFiltrada.Where(t => t.IdTarefaStatus == 1).CountAsync();
            var emAndamento = await queryFiltrada.Where(t => t.IdTarefaStatus == 2).CountAsync();
            var concluidas = await queryFiltrada.Where(t => t.IdTarefaStatus == 3).CountAsync();

            return new InnovaCore.Domain.Entities.VwDashboardQtde
            {
                TotalDeTarefas = total,
                SolicitacoesPendentes = pendentes,   // Corresponde ao Card 'Aguardando Resposta'
                TarefasEmAndamento = emAndamento,    // Corresponde ao Card 'Em Andamento'
                TarefasConcluidas = concluidas,      // Corresponde ao Card 'Concluídas' / Gráfico Donut
                TarefasAComecar = aComecar// Card 'A Fazer'
            };
        }
    }
}