using InnovaCore.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnovaCore.Data.Context
{
    public class InnovationCoreDbContext : IdentityDbContext
    {
        public InnovationCoreDbContext(DbContextOptions<InnovationCoreDbContext> options)
        : base(options)
        {
        }

        public DbSet<Solicitacao> Solicitacoes { get; set; }

        public DbSet<SolicitacaoStatus> SolicitacaoStatus { get; set; }

        public DbSet<Tarefa> Tarefas { get; set; }

        public DbSet<Temas> Temas { get; set; }

        public DbSet<Setor> Setor { get; set; }

        public DbSet<TarefaStatus> TarefaStatus { get; set; }

        public DbSet<VwDashboardQtde> VwDashboardQtdes { get; set; }

        public DbSet<VwQtdePorSetor> VwQtdePorSetor { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        //DEFINIÇÃO DE RELACIONAMENTOS E ATRIBUTOS

          //SETOR
            modelBuilder.Entity<Setor>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);


          //SOLICITAÇÕES
            modelBuilder.Entity<Solicitacao>(entity =>
            {
                entity.HasOne(s => s.SolicitacaoStatus)
                .WithMany(t => t.Solicitacao)
                .HasForeignKey(s => s.IdSolicitacaoStatus)
                .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Solicitacao>(entity =>
            {
                entity.HasOne(t => t.Tema)
                .WithMany(s => s.Solicitacaos)
                .HasForeignKey(t => t.IdTema)
                ;
            });

            modelBuilder.Entity<Solicitacao>()
               .HasOne(s => s.Usuario)
               .WithMany()
               .HasForeignKey(s => s.IdUsuario)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitacao>()
                .HasOne(s => s.UsuarioAlteracaoStatuss)
                .WithMany()
                .HasForeignKey(s => s.UsuarioAlteracaoStatus)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitacao>()
                .HasOne(s => s.Setor)
                .WithMany(e => e.Solicitacoes)
                .HasForeignKey(s => s.IdSetor)
                .OnDelete(DeleteBehavior.SetNull);

          //TAREFAS
            modelBuilder.Entity<Tarefa>(entity =>
            {
                entity.HasOne(t => t.Solicitacao)
                .WithOne(s => s.Tarefa)
                .HasForeignKey<Tarefa>(t => t.IdSolicitacao)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Tarefa>(entity =>
            {
                entity.HasOne(t => t.TarefaStatus)
                .WithMany(s => s.Tarefas)
                .HasForeignKey(t => t.IdTarefaStatus)
                ;
            });

            //VIEWS
            modelBuilder.Entity<VwDashboardQtde>().HasNoKey();
            modelBuilder.Entity<VwDashboardQtde>().ToView("VwDashboardQtde");
            modelBuilder.Entity<VwQtdePorSetor>().HasNoKey();
            modelBuilder.Entity<VwQtdePorSetor>().ToView("VwQtdePorSetor");

           
        }
    }
}
