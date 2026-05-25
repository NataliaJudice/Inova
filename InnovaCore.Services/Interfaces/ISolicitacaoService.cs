using InnovaCore.Domain.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnovaCore.Services.Interfaces
{
    public interface ISolicitacaoService
    {
        
        Task CriarProposta(Solicitacao solicitacao, string userId, string email);
        Task<IEnumerable<Solicitacao>> ListarPendentes();
        Task<IEnumerable<Solicitacao>> ListarSolicitacosUsuario(string IdUser);
        Task AprovarSolicitacao(int id);

        Task EnviarEmails(int? id, string userEmail, string tituloTarefa, string descricaoTarefa, string status);
        Task RejeitarSolicitacao(int idSolicitacao, string justificativa);
        Task<IEnumerable<Solicitacao>> ListarPendentesPaginado(string? buscaTexto, int? idSetor, int? idStatus, int skip, int take);
        Task<IEnumerable<Solicitacao>> ListarSolicitacoesUsuarioPaginado(string idUser, string? buscaTexto, int? idSetor, int? idStatus, int skip, int take);


    }
}
