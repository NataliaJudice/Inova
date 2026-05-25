using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaCore.Data.Context;
using InnovaCore.Domain.Entities;
using InnovaCore.Services.Interfaces;
using InnovaCore.Services.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InnovaCore.Tests.Services
{
    public class TarefaServiceExceptionTests
    {
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<ISetorService> _setorServicesMock;

        public TarefaServiceExceptionTests()
        {
            _emailServicesMock = new Mock<IEmailServices>();
            _setorServicesMock = new Mock<ISetorService>();
        }

        private InnovationCoreDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<InnovationCoreDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new InnovationCoreDbContext(options);
        }

        [Fact]
        public async Task AtribuirResponsavel_DeveLancarExceptionGenericaEnvelopada_QuandoIdNaoExistir()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new TarefaService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.AtribuirResponsavel(999, "Operador"));

            Assert.Contains("Erro ao atribuir responsável:", excecao.Message);
            Assert.Contains("Tarefa com ID 999 não encontrada.", excecao.Message);
        }

        [Fact]
        public async Task DeletarResponsavel_DeveLancarExceptionGenericaEnvelopada_QuandoIdNaoExistir()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new TarefaService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.DeletarResponsavel(999));

            Assert.Contains("Erro ao remover responsável:", excecao.Message);
            Assert.Contains("Tarefa com ID 999 não encontrada.", excecao.Message);
        }

        [Fact]
        public async Task MudarStatus_DeveLancarExceptionGenericaEnvelopada_QuandoTarefaNaoForEncontrada()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new TarefaService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.MudarStatus(2, 999));

            Assert.Contains("Falha ao mudar status da tarefa:", excecao.Message);
            Assert.Contains("Tarefa ou Solicitação vinculada não encontrada.", excecao.Message);
        }

       
        [Fact]
        public async Task GetAll_DeveLancarException_QuandoOcorrerErroInesperado()
        {
            // Arrange
            var service = new TarefaService(null, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.GetAll());
            Assert.Equal("Erro ao listar todas as tarefas.", excecao.Message);
        }
    }
}