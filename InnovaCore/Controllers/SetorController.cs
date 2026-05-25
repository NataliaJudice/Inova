using InnovaCore.Domain.Entities;
using InnovaCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovaCore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SetorController : Controller
    {
        private readonly ISetorService _setorService;

        public SetorController(ISetorService setorService)
        {
            _setorService = setorService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var listaSetores = await _setorService.ObterSetoresAtivos();
            return View(listaSetores);
        }

        [HttpGet]
        public async Task<IActionResult> CriarSetor()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CriarSetor([FromBody] Setor setor)
        {
            if (setor == null)
                return Json(new { success = false, message = "Os dados enviados estão corrompidos." });

            ModelState.Remove("DataCadastro");
            ModelState.Remove("Tarefas");
            ModelState.Remove("Solicitacoes");

            if (!ModelState.IsValid)
            {
                var erros = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Dados inválidos: {erros}" });
            }

            try
            {
                setor.Status = true; 
                await _setorService.CriarSetor(setor);

                return Json(new
                {
                    success = true,
                    message = "Novo terminal de setor inicializado!",
                    redirectUrl = Url.Action("Index")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Falha interna na criação: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditarSetor(int id, [FromForm] Setor setor)
        {
            ModelState.Remove("DataCadastro");
            ModelState.Remove("Tarefas");
            ModelState.Remove("Solicitacoes");

            if (!ModelState.IsValid)
            {
                var erros = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Erro de validação: {erros}" });
            }

            try
            {
                await _setorService.EditarSetor(id, setor);
                return Json(new { success = true, message = "Configurações do terminal atualizadas com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Falha ao editar setor: " + ex.Message });
            }
        }
    }
}