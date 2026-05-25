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
    public class SetorService : ISetorService
    {
        private readonly InnovationCoreDbContext _context;
        private readonly IEmailServices _emailService;

        public SetorService(InnovationCoreDbContext context, IEmailServices emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IEnumerable<Setor>> ObterSetoresAtivos()
        {
            try
            {
                return await _context.Setor
                    .AsNoTracking()
                    .Where(t => t.Status == true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao recuperar setores ativos: " + ex.Message);
            }
        }

        public async Task CriarSetor(Setor setor)
        {
            if (setor == null)
                throw new ArgumentNullException(nameof(setor), "O setor não pode ser nulo.");

            try
            {
                setor.DataCadastro = DateTime.Now;
                setor.Status = true;

                await _context.Setor.AddAsync(setor);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Erro ao salvar o setor no banco de dados. Verifique se os dados são válidos.");
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro inesperado ao criar o setor.");
            }
        }

        public async Task EditarSetor(int id, Setor setorNovo)
        {
            if (setorNovo == null)
                throw new ArgumentNullException(nameof(setorNovo));

            var setor = await _context.Setor.FirstOrDefaultAsync(s => s.Id == id);

            if (setor == null)
            {
                throw new KeyNotFoundException($"Setor com ID {id} não foi encontrado.");
            }

            try
            {
                setor.Nome = setorNovo.Nome;

                _context.Setor.Update(setor);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao atualizar o setor: " + ex.Message);
            }
        }

        public async Task EnviarEmailSetor(int? id, string taskTitle, string descricaoTarefa, string status)
        {
            if (!id.HasValue) return;

            try
            {
                var setor = await _context.Setor
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (setor == null)
                {
                    throw new Exception("Não foi possível enviar o e-mail: Setor não encontrado.");
                }
                //Pode mudar o email aqui se precisar
                string emailDestino = "natyang873@gmail.com";

                await _emailService.SendStatusUpdateEmailAsyncSetores(emailDestino, taskTitle, descricaoTarefa, status);
            }
            catch (Exception ex)
            {
                throw new Exception("Falha no serviço de e-mail: " + ex.Message);
            }
        }
    }
}