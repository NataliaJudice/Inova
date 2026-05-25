using System;
using System.Collections.Generic;
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
    public class SetorServiceExceptionTests
    {
        private readonly Mock<IEmailServices> _emailServiceMock;

        public SetorServiceExceptionTests()
        {
            _emailServiceMock = new Mock<IEmailServices>();
        }

        private InnovationCoreDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<InnovationCoreDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new InnovationCoreDbContext(options);
        }

        [Fact]
        public async Task CriarSetor_DeveLancarExcecaoEspecifica_QuandoOcorrerErroNoBanco()
        {
            // Arrange
            // Usando um contexto nulo ou fechado para forçar uma DbUpdateException/Exception no EF Core
            var service = new SetorService(null, _emailServiceMock.Object);
            var setor = new Setor { Nome = "TI" };

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.CriarSetor(setor));
            Assert.Contains("Ocorreu um erro inesperado ao criar o setor", excecao.Message);
        }

        [Fact]
        public async Task EditarSetor_DeveLancarArgumentNullException_QuandoSetorNovoForNulo()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SetorService(context, _emailServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.EditarSetor(1, null));
        }

        [Fact]
        public async Task EditarSetor_DeveLancarKeyNotFoundException_QuandoIdNaoExistirNoBanco()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SetorService(context, _emailServiceMock.Object);
            var setorNovo = new Setor { Nome = "Novo Nome" };

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.EditarSetor(999, setorNovo));
            Assert.Contains("Setor com ID 999 não foi encontrado", excecao.Message);
        }

        [Fact]
        public async Task EnviarEmailSetor_DeveRetornarSemErros_QuandoIdForNulo()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SetorService(context, _emailServiceMock.Object);

            // Act
            // Não deve lançar nada porque a primeira linha valida se !id.HasValue e faz um return precoce
            var excecao = await Record.ExceptionAsync(() => service.EnviarEmailSetor(null, "Titulo", "Desc", "Status"));

            // Assert
            Assert.Null(excecao);
        }

        [Fact]
        public async Task EnviarEmailSetor_DeveLancarExcecao_QuandoSetorNaoForEncontrado()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SetorService(context, _emailServiceMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.EnviarEmailSetor(999, "Titulo", "Desc", "Status"));
            Assert.Contains("Não foi possível enviar o e-mail: Setor não encontrado", excecao.Message);
        }

        [Fact]
        public async Task EnviarEmailSetor_DeveEnveloparExcecao_QuandoServicoDeEmailFalhar()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var setor = new Setor { Id = 1, Nome = "Suporte" };
            await context.Setor.AddAsync(setor);
            await context.SaveChangesAsync();

            // Simula o serviço de e-mail disparando um erro de rede/SMTP interno
            _emailServiceMock.Setup(x => x.SendStatusUpdateEmailAsyncSetores(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Timeout"));

            var service = new SetorService(context, _emailServiceMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.EnviarEmailSetor(1, "Titulo", "Desc", "Status"));
            Assert.Contains("Falha no serviço de e-mail: SMTP Timeout", excecao.Message);
        }
    }
}