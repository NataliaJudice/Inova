using InnovaCore.Domain.ViewModels;
using InnovaCore.Services.Interfaces;
using InnovaCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InnovaCore.Controllers
{
    public class TarefaController : Controller
    {
        private readonly ITarefaService _tarefaService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailServices _emailService;
        private readonly ISetorService _setorService;

        public TarefaController(ITarefaService tarefaService, UserManager<IdentityUser> userManager, IEmailServices emailServices, ISetorService setorService)
        {
            _tarefaService = tarefaService;
            _userManager = userManager;
            _emailService = emailServices;
            _setorService = setorService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCardsKanban(string search, int? setorId, int statusId, int skip = 0, int take = 12)
        {
            try
            {
                var (tarefas, totalReal) = await _tarefaService.GetFiltradasPaged(statusId, search, setorId, skip, take);

                var payload = tarefas.Select(t => new
                {
                    id = t.Id,
                    titulo = t.Solicitacao?.Titulo ?? "Sem Título",
                    descricao = t.Solicitacao?.Descricao ?? "",
                    setor = t.Solicitacao?.Setor?.Nome ?? "N/A",
                    data = t.Datacadastro?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    dataCurta = t.Datacadastro?.ToString("dd/MM") ?? "",
                    responsavel = t.NomeResponsavel ?? ""
                });
                return Json(new { success = true, data = payload, totalReal = totalReal });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TodasAsTarefas()
        {

            ViewBag.Setores = await _setorService.ObterSetoresAtivos(); 
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AtribuirResponsavel([FromBody] ViewModelAtribuirResponsavel request)
        {
            if (request == null || request.idTarefa <= 0 || string.IsNullOrEmpty(request.nomeResponsavel))
                return Json(new { success = false, message = "Dados insuficientes para atribuir um responsável." });

            try
            {
                await _tarefaService.AtribuirResponsavel(request.idTarefa, request.nomeResponsavel);
                return Json(new { success = true, message = $"Responsável {request.nomeResponsavel} atribuído com sucesso." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro interno ao atribuir responsável: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletarResponsavel([FromBody] ViewModelAtribuirResponsavel request)
        {
            if (request == null || request.idTarefa <= 0)
                return Json(new { success = false, message = "Identificador de tarefa inválido." });

            try
            {
                await _tarefaService.DeletarResponsavel(request.idTarefa);
                return Json(new { success = true, message = "Responsável removido da tarefa com sucesso." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro interno ao remover responsável: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken] // Mantido conforme seu código original
        public async Task<IActionResult> MudarStatus([FromBody] ViewModelTarefaEStatus model)
        {
            if (model == null || model.idTarefa <= 0 || model.novoStatus <= 0)
                return Json(new { success = false, message = "Dados de transição de status inválidos." });

            try
            {
                await _tarefaService.MudarStatus(model.novoStatus, model.idTarefa);
                return Json(new { success = true, message = "Status atualizado na matriz de tarefas." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Falha crítica na transição do pipeline: " + ex.Message });
            }
        }
    }
}