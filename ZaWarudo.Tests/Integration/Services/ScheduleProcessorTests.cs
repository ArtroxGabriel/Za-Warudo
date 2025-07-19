using JetBrains.Annotations;
using Moq;
using Serilog;
using ZaWarudo.Model;
using ZaWarudo.Scheduler;
using ZaWarudo.Services;

namespace ZaWarudo.Tests.Integration.Services;

[TestSubject(typeof(ScheduleProcessor))]
public class ScheduleProcessorTests
{
    static ScheduleProcessorTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }
    [Fact]
        public async Task ProcessScheduleAsync_SuccessfulProcessing_WritesCorrectOutput()
        {
            // Arrange
            var mockScheduler = new Mock<IScheduler>();

            mockScheduler.Setup(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()))
                         .ReturnsAsync(Result<Model.Unit, SchedulerError>.Success(Model.Unit.Value));

            mockScheduler.SetupSequence(s => s.CheckIfSerializableAsync())
                         .ReturnsAsync(Result<string, SchedulerError>.Success("Result for S1"))
                         .ReturnsAsync(Result<string, SchedulerError>.Success("Result for S2"));

            mockScheduler.Setup(s => s.ResetScheduler())
                         .ReturnsAsync(Result<Model.Unit, SchedulerError>.Success(Model.Unit.Value));

            var schedulePlans = new List<SchedulePlan>
            {
                new("S1", new List<Operation> { new(OperationType.Read, "T1", "A") }),
                new("S2", new List<Operation> { new(OperationType.Write, "T2", "B") })
            };

            const string outputPath = "test_output_successful.txt";
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var processor = new ScheduleProcessor(mockScheduler.Object);

            try
            {
                // Act
                var result = await processor.ProcessScheduleAsync(schedulePlans, outputPath);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.False(result.IsError);

                // Verifique se os métodos do mock foram chamados o número correto de vezes
                mockScheduler.Verify(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()), Times.Exactly(2));
                mockScheduler.Verify(s => s.CheckIfSerializableAsync(), Times.Exactly(2));
                mockScheduler.Verify(s => s.ResetScheduler(), Times.Exactly(2));

                // Verifique o conteúdo do arquivo de saída
                Assert.True(File.Exists(outputPath));
                var outputContent = await File.ReadAllTextAsync(outputPath);
                Assert.Contains("Result for S1", outputContent);
                Assert.Contains("Result for S2", outputContent);
                Assert.DoesNotContain("Result for S3", outputContent); // Verifique que não há lixo
            }
            finally
            {
                // Clean up: Delete the test output file
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }

        [Fact]
            public async Task ProcessScheduleAsync_OutputFileAlreadyExists_IsOverwritten()
            {
                // Arrange
                var mockScheduler = new Mock<IScheduler>();
                mockScheduler.Setup(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()))
                             .ReturnsAsync(Result<Model.Unit, SchedulerError>.Success(Model.Unit.Value));
                mockScheduler.Setup(s => s.CheckIfSerializableAsync())
                             .ReturnsAsync(Result<string, SchedulerError>.Success("New content"));
                mockScheduler.Setup(s => s.ResetScheduler())
                             .ReturnsAsync(Result<Model.Unit, SchedulerError>.Success(Model.Unit.Value));

                var outputPath = "test_output_overwrite.txt";
                // Crie o arquivo com conteúdo antigo para simular que ele já existe
                await File.WriteAllTextAsync(outputPath, "Old content that should be overwritten.");

                var schedulePlans = new List<SchedulePlan>
                {
                    new("S1", new List<Operation> { new(OperationType.Read, "T1", "A") })
                };

                var processor = new ScheduleProcessor(mockScheduler.Object);

                try
                {
                    // Act
                    var result = await processor.ProcessScheduleAsync(schedulePlans, outputPath);

                    // Assert
                    Assert.True(result.IsSuccess);
                    Assert.False(result.IsError);

                    var outputContent = await File.ReadAllTextAsync(outputPath);
                    Assert.Contains("New content", outputContent);
                    Assert.DoesNotContain("Old content", outputContent);
                }
                finally
                {
                    // Clean up
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
            }

            [Fact]
            public async Task ProcessScheduleAsync_SetScheduleFails_ReturnsErrorAndStopsProcessing()
            {
                // Arrange
                var mockScheduler = new Mock<IScheduler>();
                // Simula uma falha em SetScheduleAsync para o primeiro plano
                mockScheduler.Setup(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()))
                             .ReturnsAsync(Result<Model.Unit, SchedulerError>.Error(new SchedulerError("Set schedule failed")));
                // Garante que CheckIfSerializableAsync e ResetScheduler NÃO são chamados após a falha
                mockScheduler.Setup(s => s.CheckIfSerializableAsync()).Verifiable(Times.Never());
                mockScheduler.Setup(s => s.ResetScheduler()).Verifiable(Times.Never());


                var schedulePlans = new List<SchedulePlan>
                {
                    new("S1", new List<Operation>()),
                    new("S2", new List<Operation>()) // Este plano não deve ser processado
                };
                var outputPath = "test_output_set_schedule_fail.txt";
                if (File.Exists(outputPath)) File.Delete(outputPath);

                var processor = new ScheduleProcessor(mockScheduler.Object);

                try
                {
                    // Act
                    var result = await processor.ProcessScheduleAsync(schedulePlans, outputPath);

                    // Assert
                    Assert.True(result.IsError);
                    Assert.False(result.IsSuccess);
                    Assert.Contains("Failed to set schedule for plan S1", result.GetErrorOrThrow().ToString());

                    // Verifica que o arquivo de saída está vazio ou não contém resultados (não deve ter sido escrito nada relevante)
                    Assert.True(File.Exists(outputPath)); // O arquivo é criado, mas não deve ter nada relevante
                    var outputContent = await File.ReadAllTextAsync(outputPath);
                    Assert.Empty(outputContent); // Nenhuma saída deve ser escrita

                    // Verifica que CheckIfSerializableAsync e ResetScheduler nunca foram chamados
                    mockScheduler.Verify(s => s.CheckIfSerializableAsync(), Times.Never());
                    mockScheduler.Verify(s => s.ResetScheduler(), Times.Never());
                }
                finally
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
            }

            [Fact]
            public async Task ProcessScheduleAsync_CheckIfSerializableFails_ReturnsErrorAndStopsProcessing()
            {
                // Arrange
                var mockScheduler = new Mock<IScheduler>();
                mockScheduler.Setup(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()))
                             .ReturnsAsync(Result<Model.Unit, SchedulerError>.Success(Model.Unit.Value));
                // Simula uma falha em CheckIfSerializableAsync para o primeiro plano
                mockScheduler.Setup(s => s.CheckIfSerializableAsync())
                             .ReturnsAsync(Result<string, SchedulerError>.Error(new SchedulerError("Serialization check failed")));
                // Garante que ResetScheduler NÃO é chamado após a falha
                mockScheduler.Setup(s => s.ResetScheduler()).Verifiable(Times.Never());

                var schedulePlans = new List<SchedulePlan>
                {
                    new("S1", new List<Operation>()),
                    new("S2", new List<Operation>()) // Este plano não deve ser processado
                };
                var outputPath = "test_output_serializable_fail.txt";
                if (File.Exists(outputPath)) File.Delete(outputPath);

                var processor = new ScheduleProcessor(mockScheduler.Object);

                try
                {
                    // Act
                    var result = await processor.ProcessScheduleAsync(schedulePlans, outputPath);

                    // Assert
                    Assert.True(result.IsError);
                    Assert.False(result.IsSuccess);
                    Assert.Contains("Failed to check serializability for plan S1", result.GetErrorOrThrow().ToString());

                    Assert.True(File.Exists(outputPath));
                    var outputContent = await File.ReadAllTextAsync(outputPath);
                    Assert.Empty(outputContent); // Nenhuma saída deve ser escrita

                    mockScheduler.Verify(s => s.ResetScheduler(), Times.Never());
                    mockScheduler.Verify(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()), Times.Once()); // SetScheduleAsync foi chamado
                    mockScheduler.Verify(s => s.CheckIfSerializableAsync(), Times.Once()); // CheckIfSerializableAsync foi chamado uma vez
                }
                finally
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
            }

            [Fact]
            public async Task ProcessScheduleAsync_ResetSchedulerFails_ReturnsErrorAndStopsProcessing()
            {
                // Arrange
                var mockScheduler = new Mock<IScheduler>();
                mockScheduler.Setup(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()))
                             .ReturnsAsync(Result<Model.Unit, SchedulerError>.Success(Model.Unit.Value));
                mockScheduler.Setup(s => s.CheckIfSerializableAsync())
                             .ReturnsAsync(Result<string, SchedulerError>.Success("Some result"));
                // Simula uma falha em ResetScheduler para o primeiro plano
                mockScheduler.Setup(s => s.ResetScheduler())
                             .ReturnsAsync(Result<Model.Unit, SchedulerError>.Error(new SchedulerError("Reset failed")));


                var schedulePlans = new List<SchedulePlan>
                {
                    new("S1", new List<Operation>()),
                    new("S2", new List<Operation>()) // Este plano não deve ser processado
                };
                var outputPath = "test_output_reset_fail.txt";
                if (File.Exists(outputPath)) File.Delete(outputPath);

                var processor = new ScheduleProcessor(mockScheduler.Object);

                try
                {
                    // Act
                    var result = await processor.ProcessScheduleAsync(schedulePlans, outputPath);

                    // Assert
                    Assert.True(result.IsError);
                    Assert.False(result.IsSuccess);
                    Assert.Contains("Failed to reset scheduler after processing plan S1", result.GetErrorOrThrow().ToString());

                    Assert.True(File.Exists(outputPath));
                    var outputContent = await File.ReadAllTextAsync(outputPath);
                    // Deve conter a saída do primeiro plano, pois a falha de reset ocorre DEPOIS de escrever a saída.
                    Assert.Contains("Some result", outputContent);

                    mockScheduler.Verify(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>()), Times.Once());
                    mockScheduler.Verify(s => s.CheckIfSerializableAsync(), Times.Once());
                    mockScheduler.Verify(s => s.ResetScheduler(), Times.Once()); // ResetScheduler foi chamado uma vez
                }
                finally
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
            }

            [Fact]
            public async Task ProcessScheduleAsync_EmptySchedulePlans_ReturnsSuccessAndEmptyFile()
            {
                // Arrange
                var mockScheduler = new Mock<IScheduler>();
                // Garante que nenhum método do scheduler é chamado
                mockScheduler.Setup(s => s.SetScheduleAsync(It.IsAny<SchedulePlan>())).Verifiable(Times.Never());
                mockScheduler.Setup(s => s.CheckIfSerializableAsync()).Verifiable(Times.Never());
                mockScheduler.Setup(s => s.ResetScheduler()).Verifiable(Times.Never());

                var schedulePlans = new List<SchedulePlan>(); // Lista vazia

                var outputPath = "test_output_empty_plans.txt";
                if (File.Exists(outputPath)) File.Delete(outputPath);

                var processor = new ScheduleProcessor(mockScheduler.Object);

                try
                {
                    // Act
                    var result = await processor.ProcessScheduleAsync(schedulePlans, outputPath);

                    // Assert
                    Assert.True(result.IsSuccess);
                    Assert.False(result.IsError);

                    // Verifique que nenhum método do mock foi chamado
                    mockScheduler.VerifyNoOtherCalls();

                    // Verifique que o arquivo foi criado, mas está vazio
                    Assert.True(File.Exists(outputPath));
                    var outputContent = await File.ReadAllTextAsync(outputPath);
                    Assert.Empty(outputContent);
                }
                finally
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
            }
}
