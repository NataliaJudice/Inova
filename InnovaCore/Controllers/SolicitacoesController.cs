using InnovaCore.Domain.Entities;
using InnovaCore.Domain.ViewModels;
using InnovaCore.Services.Interfaces;
using InnovaCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnovaCore.Controllers
{
    public class SolicitacoesController : Controller
    {
        private readonly ISolicitacaoService _solicitacaoService;
        private readonly ISetorService _setorService;

        public SolicitacoesController(ISolicitacaoService solicitacaoService, ISetorService setorService)
        {
            _solicitacaoService = solicitacaoService;
            _setorService = setorService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            IEnumerable<Setor> TotalSetores = await _setorService.ObterSetoresAtivos();
            return View(TotalSetores);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateSolicitacaoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var erroValidacao = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Dados inválidos: {erroValidacao}" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email);

                var solicitacao = new Solicitacao
                {
                    Titulo = model.Titulo,
                    IdSetor = model.IdSetor,
                    Descricao = model.Descricao
                };

                await _solicitacaoService.CriarProposta(solicitacao, userId, email);

                return Json(new
                {
                    success = true,
                    message = "Proposta enviada com sucesso!",
                    redirectUrl = Url.Action(nameof(ListarSolicitacoesUsuario))
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro interno ao salvar dados: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ListarPendentes(string? buscaTexto, int? idSetor, int? idStatus, int pagina = 1)
        {
            try
            {
                int itensPorPagina = 15;
                int skip = (pagina - 1) * itensPorPagina;

                var solicitacoes = await _solicitacaoService.ListarPendentesPaginado(buscaTexto, idSetor, idStatus, skip, itensPorPagina);

                ViewBag.Setores = await _setorService.ObterSetoresAtivos();
                ViewBag.BuscaTexto = buscaTexto;
                ViewBag.IdSetorSelecionado = idSetor;
                ViewBag.IdStatusSelecionado = idStatus;

                return View(solicitacoes);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro crítico ao carregar solicitações pendentes.";
                return RedirectToAction("HubAdmin", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendentesJson(string? buscaTexto, int? idSetor, int? idStatus, int skip = 0, int take = 15)
        {
            try
            {
                var dados = await _solicitacaoService.ListarPendentesPaginado(buscaTexto, idSetor, idStatus, skip, take);

                var dadosMapeados = dados.Select(s => new {
                    s.Id,
                    s.Titulo,
                    s.Descricao,
                    SetorNome = s.Setor?.Nome ?? "N/A",
                    Datacadastro = s.Datacadastro?.ToString("dd/MM/yyyy HH:mm")
                });

                return Json(dadosMapeados);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprovar(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "Identificador de solicitação inválido." });

            try
            {
                await _solicitacaoService.AprovarSolicitacao(id);
                return Json(new
                {
                    success = true,
                    message = "Solicitação aprovada com sucesso!",
                    redirectUrl = Url.Action("TodasAsTarefas", "Tarefa")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Falha ao processar aprovação: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejeitar([FromBody] ViewModelRejeicao vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "A justificativa é obrigatória para rejeitar." });
            }

            try
            {
                await _solicitacaoService.RejeitarSolicitacao(vm.IdSolicitacao, vm.Justificativa);
                return Json(new { success = true, message = "Solicitação rejeitada e usuário notificado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro ao rejeitar solicitação: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ListarSolicitacoesUsuario(string? buscaTexto, int? idSetor, int? idStatus)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                ViewBag.Setores = await _setorService.ObterSetoresAtivos();
                ViewBag.BuscaTexto = buscaTexto;
                ViewBag.IdSetor = idSetor;
                ViewBag.IdStatus = idStatus;

                var solicitacoes = await _solicitacaoService.ListarSolicitacoesUsuarioPaginado(userId, buscaTexto, idSetor, idStatus, 0, 15);
                return View(solicitacoes);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não foi possível carregar seu histórico de solicitações.";
                return RedirectToAction("Hub", "Home");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSolicitacoesUsuarioJson(string? buscaTexto, int? idSetor, int? idStatus, int skip = 0, int take = 15)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var dados = await _solicitacaoService.ListarSolicitacoesUsuarioPaginado(userId, buscaTexto, idSetor, idStatus, skip, take);

                var dadosMapeados = dados.Select(s => new {
                    s.Id,
                    s.Titulo,
                    s.Descricao,
                    s.IdSolicitacaoStatus,
                    SetorNome = s.Setor?.Nome ?? "N/A",
                    StatusNome = s.Tarefa?.TarefaStatus?.Nome ?? s.SolicitacaoStatus?.NomeStatus,
                    IsTarefa = s.Tarefa != null,
                    JustificativaRejeicao = s.JustificativaRejeicao ?? ""
                });

                return Json(dadosMapeados);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}