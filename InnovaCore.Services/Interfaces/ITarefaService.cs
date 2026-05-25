using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnovaCore.Domain.Entities;

namespace InnovaCore.Services.Interfaces
{
    public interface ITarefaService
    {
        Task DeletarResponsavel(int idTarefa);
        Task<IEnumerable<Tarefa>> GetAll();
        Task MudarStatus(int novoStatus, int idTarefa);
        Task AtribuirResponsavel(int idTarefa, string nomeResponsavel);
        Task<(IEnumerable<Tarefa> deTarefas, int totalReal)> GetFiltradasPaged(int statusId, string search, int? setorId, int skip, int take);
    }
}
