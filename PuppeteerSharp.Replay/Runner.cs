using System;
using System.Threading.Tasks;
using PuppeteerSharp.Replay.Contracts;

namespace PuppeteerSharp.Replay
{
    public class Runner
    {
        public UserFlow Flow { get; }
        public IRunnerExtension Extension { get; }

        private bool _Aborted;

        public Runner(UserFlow flow, IRunnerExtension extension)
        {
            Flow = flow;
            Extension = extension;
        }

        public async Task<bool> Run()
        {
            _Aborted = false;
            await Extension.BeforeAllSteps(Flow);

            foreach (var step in Flow.Steps)
            {
                await Extension.BeforeEachStep(step, Flow);
                await Extension.RunStep(step, Flow);
                await Extension.AfterEachStep(step, Flow);
            }

            await Extension.AfterAllSteps(Flow);
            return false; 
        }
    }
}
