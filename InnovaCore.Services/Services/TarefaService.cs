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
    public class TarefaService : ITarefaService
    {
        private readonly InnovationCoreDbContext _context;
        private readonly IEmailServices _emailServices;
        private readonly ISetorService _setorServices;

        public TarefaService(InnovationCoreDbContext context, IEmailServices emailServices, ISetorService setorServices)
        {
            _context = context;
            _emailServices = emailServices;
            _setorServices = setorServices;
        }

        public async Task<(IEnumerable<Tarefa> deTarefas, int totalReal)> GetFiltradasPaged(int statusId, string search, int? setorId, int skip, int take)
        {
            try
            {
                var query = _context.Tarefas
                    .Include(s => s.Solicitacao)
                        .ThenInclude(s => s.Setor)
                    .Where(s => s.Status && s.IdTarefaStatus == statusId);

                if (!string.IsNullOrEmpty(search))
                {
                    string searchLower = search.ToLower();
                    query = query.Where(t => t.Solicitacao.Titulo.ToLower().Contains(searchLower) ||
                                             (t.Solicitacao.Descricao != null && t.Solicitacao.Descricao.ToLower().Contains(searchLower)));
                }

                if (setorId.HasValue && setorId.Value > 0)
                {
                    query = query.Where(t => t.Solicitacao.IdSetor == setorId.Value);
                }

                int totalReal = await query.CountAsync();

                var tarefas = await query
                    .AsNoTracking()
                    .OrderByDescending(t => t.Datacadastro)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                return (tarefas, totalReal);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar tarefas filtradas de forma paginada: " + ex.Message);
            }
        }

        public async Task AtribuirResponsavel(int idTarefa, string nomeResponsavel)
        {
            try
            {
                var tarefa = await _context.Tarefas.FirstOrDefaultAsync(x => x.Id == idTarefa);

                if (tarefa == null)
                    throw new KeyNotFoundException($"Tarefa com ID {idTarefa} não encontrada.");

                tarefa.NomeResponsavel = nomeResponsavel;

                _context.Tarefas.Update(tarefa);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao atribuir responsável: " + ex.Message);
            }
        }

        public async Task DeletarResponsavel(int idTarefa)
        {
            try
            {
                var tarefa = await _context.Tarefas.FirstOrDefaultAsync(x => x.Id == idTarefa);

                if (tarefa == null)
                    throw new KeyNotFoundException($"Tarefa com ID {idTarefa} não encontrada.");

                tarefa.NomeResponsavel = null;

                _context.Tarefas.Update(tarefa);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao remover responsável: " + ex.Message);
            }
        }

        public async Task MudarStatus(int novoStatus, int idTarefa)
        {
            try
            {
                var tarefa = await _context.Tarefas
                    .Include(t => t.Solicitacao)
                        .ThenInclude(s => s.Usuario)
                    .FirstOrDefaultAsync(t => t.Id == idTarefa);

                if (tarefa == null || tarefa.Solicitacao == null)
                    throw new KeyNotFoundException("Tarefa ou Solicitação vinculada não encontrada.");

                tarefa.IdTarefaStatus = novoStatus;

                var statusInfo = await _context.TarefaStatus
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == novoStatus);

                string nomeStatus = statusInfo?.Nome ?? "Status Atualizado";

                await _context.SaveChangesAsync();

                if (tarefa.Solicitacao.Usuario != null && !string.IsNullOrEmpty(tarefa.Solicitacao.Usuario.Email))
                    await _emailServices.SendStatusUpdateEmailAsync(tarefa.Solicitacao.Usuario.Email, tarefa.Solicitacao.Titulo, nomeStatus);
                

                await _setorServices.EnviarEmailSetor(tarefa.Solicitacao.IdSetor, tarefa.Solicitacao.Titulo, tarefa.Solicitacao.Descricao, nomeStatus);
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao mudar status da tarefa: " + ex.Message);
            }
        }
        public async Task<IEnumerable<Tarefa>> GetAll()
        {
            try
            {
                return await _context.Tarefas
                    .AsNoTracking()
                    .Include(s => s.Solicitacao)
                        .ThenInclude(s => s.Setor)
                    .Where(s => s.Status)    
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao listar todas as tarefas.");
            }
        }
    }
}