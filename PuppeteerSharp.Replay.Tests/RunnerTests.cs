using Moq;
using PuppeteerSharp.Replay.Contracts;

namespace PuppeteerSharp.Replay.Tests
{
    public class RunnerTests
    {
        Mock<IRunnerExtension> _ExtensionMock;
        UserFlow _Flow;
        Runner _Sut;

        public RunnerTests()
        {
            _ExtensionMock = new Mock<IRunnerExtension>();
            _Flow = UserFlow.Parse(File.ReadAllText(@"Data\UserFlowExample.json"));
            _Sut = new Runner(_Flow, _ExtensionMock.Object);
        }

        [Fact]
        public void Constructor_SavesArgumentsToProperties()
        {
            Assert.Equal(_Flow, _Sut.Flow);
            Assert.Equal(_ExtensionMock.Object, _Sut.Extension);
        }

        [Fact]
        public async Task Run_CallsBeforeAllSteps_Once()
        {
            await _Sut.Run();

            _ExtensionMock.Verify(x => x.BeforeAllSteps(It.IsAny<UserFlow>()), Times.Once());
        }

        [Fact]
        public async Task Run_CallsAfterAllSteps_Once()
        {
            await _Sut.Run();

            _ExtensionMock.Verify(x => x.AfterAllSteps(It.IsAny<UserFlow>()), Times.Once());
        }

        [Fact]
        public async Task Run_CallsBeforeEachStep_ForEachStep()
        {
            await _Sut.Run();
            var timesExpected = _Flow.Steps.Length;

            _ExtensionMock.Verify(x => x.BeforeEachStep(It.IsAny<Step>(), It.IsAny<UserFlow>()), Times.Exactly(timesExpected));
        }

        [Fact]
        public async Task Run_CallsAfterEachStep_ForEachStep()
        {
            await _Sut.Run();
            var timesExpected = _Flow.Steps.Length;

            _ExtensionMock.Verify(x => x.AfterEachStep(It.IsAny<Step>(), It.IsAny<UserFlow>()), Times.Exactly(timesExpected));
        }

        [Fact]
        public async Task Run_CallsRunStep_ForEachStep()
        {
            await _Sut.Run();
            var timesExpected = _Flow.Steps.Length;

            _ExtensionMock.Verify(x => x.RunStep(It.IsAny<Step>(), It.IsAny<UserFlow>()), Times.Exactly(timesExpected));
        }

        [Fact]
        public async Task CanRunFlow()
        {
            var runner = await Runner.CreateRunner(_Flow);
            await runner.Run();
        }
    }
}
