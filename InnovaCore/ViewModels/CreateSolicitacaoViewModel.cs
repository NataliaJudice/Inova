using InnovaCore.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace InnovaCore.ViewModels
{
    public class CreateSolicitacaoViewModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "O setor é obrigatório.")]
        public int IdSetor { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; }

    }
}
