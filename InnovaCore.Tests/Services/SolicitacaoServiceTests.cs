using System;
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
    public class SolicitacaoServiceExceptionTests
    {
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<ISetorService> _setorServicesMock;

        public SolicitacaoServiceExceptionTests()
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
        public async Task CriarProposta_DeveLancarArgumentNullException_QuandoSolicitacaoForNula()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SolicitacaoService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CriarProposta(null, "user1", "email@test.com"));
        }

        [Fact]
        public async Task CriarProposta_DeveEnveloparExcecao_QuandoErroNoBanco()
        {
            // Arrange
            var service = new SolicitacaoService(null, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.CriarProposta(new Solicitacao(), "user1", "email@test.com"));
            Assert.Contains("Falha ao criar proposta", excecao.Message);
        }

        [Fact]
        public async Task AprovarSolicitacao_DeveLancarExceptionGenericaEnvelopada_QuandoSolicitacaoNaoExistir()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SolicitacaoService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert - Espera a Exception genérica que o seu catch joga
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.AprovarSolicitacao(999));

            // Valida se as mensagens interna e externa foram mescladas com sucesso
            Assert.Contains("Erro ao aprovar solicitação:", excecao.Message);
            Assert.Contains("Solicitação não encontrada.", excecao.Message);
        }

        [Fact]
        public async Task RejeitarSolicitacao_DeveLancarExceptionGenericaEnvelopada_QuandoSolicitacaoNaoExistir()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var service = new SolicitacaoService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act & Assert - Espera a Exception genérica que o seu catch joga
            var excecao = await Assert.ThrowsAsync<Exception>(() => service.RejeitarSolicitacao(999, "Justificativa"));

            // Valida se as mensagens interna e externa foram mescladas com sucesso
            Assert.Contains("Erro ao rejeitar solicitação:", excecao.Message);
            Assert.Contains("Solicitação não encontrada.", excecao.Message);
        }

        [Fact]
        public async Task EnviarEmails_DeveEngolirExcecaoSilenciosamente_QuandoServicosDeEmailFalharem()
        {
            // Arrange
            using var context = GetDatabaseContext();

            // Força ambos os mocks de email a explodirem exceções brutas
            _emailServicesMock.Setup(x => x.SendStatusUpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                              .ThrowsAsync(new Exception("Crash total no microserviço de email"));

            _setorServicesMock.Setup(x => x.EnviarEmailSetor(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                              .ThrowsAsync(new Exception("Crash no serviço de setor"));

            var service = new SolicitacaoService(context, _emailServicesMock.Object, _setorServicesMock.Object);

            // Act
            // O seu método EnviarEmails tem um bloco try {} catch (Exception) { } vazio (engole o erro)
            var excecao = await Record.ExceptionAsync(() => service.EnviarEmails(1, "teste@teste.com", "Task", "Desc", "Status"));

            // Assert
            // O teste passa se nenhuma exceção bolhasse para fora do método, garantindo que o fluxo não quebra por causa do envio do e-mail
            Assert.Null(excecao);
        }
    }
}