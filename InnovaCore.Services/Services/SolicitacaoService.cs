using InnovaCore.Data.Context;
using InnovaCore.Domain.Entities;
using InnovaCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnovaCore.Services.Services
{
    public class SolicitacaoService : ISolicitacaoService
    {
        private readonly InnovationCoreDbContext _context;
        private readonly IEmailServices _emailServices;
        private readonly ISetorService _setorServices;

        public SolicitacaoService(InnovationCoreDbContext context, IEmailServices emailServices, ISetorService setorServices)
        {
            _context = context;
            _emailServices = emailServices;
            _setorServices = setorServices;
        }

        public async Task CriarProposta(Solicitacao solicitacao, string userId, string email)
        {
            if (solicitacao == null) throw new ArgumentNullException(nameof(solicitacao));

            try
            {
                solicitacao.Datacadastro = DateTime.Now;
                solicitacao.Status = true;
                solicitacao.IdSolicitacaoStatus = 1;
                solicitacao.IdUsuario = userId;

                _context.Solicitacoes.Add(solicitacao);
                await _context.SaveChangesAsync();

                await EnviarEmails(solicitacao.IdSetor, email, solicitacao.Titulo, solicitacao.Descricao, "Enviada");
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao criar proposta: " + ex.Message);
            }
        }

        public async Task<IEnumerable<Solicitacao>> ListarPendentesPaginado(string? buscaTexto, int? idSetor, int? idStatus, int skip, int take)
        {
            var query = _context.Solicitacoes
                .Include(x => x.Setor).Where(x => x.IdSolicitacaoStatus == 1).AsQueryable();

            if (!string.IsNullOrEmpty(buscaTexto))
                query = query.Where(s => s.Titulo.Contains(buscaTexto) || s.Descricao.Contains(buscaTexto));

            if (idSetor.HasValue)
                query = query.Where(s => s.IdSetor == idSetor.Value);

            if (idStatus.HasValue)
                query = query.Where(s => s.IdSolicitacaoStatus == idStatus.Value); 

            return await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
      
        public async Task<IEnumerable<Solicitacao>> ListarPendentes()
        {
            return await ListarPendentesPaginado(null, null, 0, 15,0);
        }

        public async Task AprovarSolicitacao(int id)
        {
            try
            {
                var solicitacao = await _context.Solicitacoes
                    .Include(s => s.Usuario)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (solicitacao == null)
                    throw new KeyNotFoundException("Solicitação não encontrada.");

                solicitacao.IdSolicitacaoStatus = 2;

                Tarefa novatarefa = new Tarefa()
                {
                    Datacadastro = DateTime.Now,
                    DataPrevisaoEntrega = DateTime.MinValue,
                    IdSolicitacao = id,
                    IdTarefaStatus = 1,
                    Status = true
                };

                _context.Tarefas.Add(novatarefa);
                await _context.SaveChangesAsync();

                await EnviarEmails(solicitacao.IdSetor, solicitacao.Usuario?.Email, solicitacao.Titulo, solicitacao.Descricao, "Aprovada");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao aprovar solicitação: " + ex.Message);
            }
        }

        public async Task RejeitarSolicitacao(int idSolicitacao, string justificativa)
        {
            try
            {
                var solicitacao = await _context.Solicitacoes
                    .Include(s => s.Usuario)
                    .FirstOrDefaultAsync(s => s.Id == idSolicitacao);

                if (solicitacao == null)
                    throw new KeyNotFoundException("Solicitação não encontrada.");

                solicitacao.IdSolicitacaoStatus = 3;
                solicitacao.JustificativaRejeicao = justificativa;

                _context.Solicitacoes.Update(solicitacao);
                await _context.SaveChangesAsync();

                await EnviarEmails(solicitacao.IdSetor, solicitacao.Usuario?.Email, solicitacao.Titulo, solicitacao.Descricao, "Inviabilizada");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro aoBox rejeitar solicitação: " + ex.Message);
            }
        }

        public async Task<IEnumerable<Solicitacao>> ListarSolicitacoesUsuarioPaginado(string idUser, string? buscaTexto, int? idSetor, int? idStatus, int skip, int take)
        {
            if (string.IsNullOrEmpty(idUser)) 
                return Enumerable.Empty<Solicitacao>();

            try
            {
                var query = _context.Solicitacoes
                    .Include(s => s.SolicitacaoStatus)
                    .Include(s => s.Setor)
                    .Include(s => s.Tarefa)
                        .ThenInclude(t => t.TarefaStatus)
                    .Where(s => s.IdUsuario == idUser);

                if (!string.IsNullOrEmpty(buscaTexto))
                    query = query.Where(s => s.Titulo.Contains(buscaTexto) || s.Descricao.Contains(buscaTexto));
                
                if (idSetor.HasValue && idSetor.Value > 0)
                    query = query.Where(s => s.IdSetor == idSetor.Value); 

                if (idStatus.HasValue && idStatus.Value > 0)
                    query = query.Where(s => s.IdSolicitacaoStatus == idStatus.Value);

                return await query
                    .AsNoTracking()
                    .OrderByDescending(s => s.Datacadastro)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw new Exception("Erro ao buscar histórico filtrado do usuário.");
            }
        }

        public async Task<IEnumerable<Solicitacao>> ListarSolicitacosUsuario(string idUser)
        {
            return await ListarSolicitacoesUsuarioPaginado(idUser, null, null, null, 0, 15);
        }

        public async Task EnviarEmails(int? id, string userEmail, string tituloTarefa, string descricaoTarefa, string status)
        {
            try
            {
                await _emailServices.SendStatusUpdateEmailAsync(userEmail, tituloTarefa, status);
                await _setorServices.EnviarEmailSetor(id, tituloTarefa, descricaoTarefa, status);
            }
            catch (Exception ex) {
                throw new Exception("Falha no serviço de e-mail: " + ex.Message);
            }
        }
    }
}