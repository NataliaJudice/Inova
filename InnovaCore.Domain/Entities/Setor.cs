using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnovaCore.Domain.Entities
{
    public class Setor
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? EmailResponsavel { get; set; }
        public DateTime DataCadastro { get; set; }
        public bool Status {  get; set; }
        public string? IdUsuario { get; set; }
        public IdentityUser? User { get; set; }
        public ICollection<Solicitacao> Solicitacoes { get; set; }

    }
}
