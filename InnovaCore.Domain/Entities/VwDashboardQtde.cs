using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnovaCore.Domain.Entities
{
    public class VwDashboardQtde
    {
        public int SolicitacoesPendentes { get; set; }
        public int TotalDeTarefas { get; set; }
        public int TarefasEmAndamento { get; set; }
        public int TarefasAComecar {  get; set; }

        public int TarefasConcluidas { get; set; }
    }
}
