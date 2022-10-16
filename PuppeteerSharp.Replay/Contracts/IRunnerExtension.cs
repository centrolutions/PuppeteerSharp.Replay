using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Contracts
{
    public interface IRunnerExtension
    {
        Task BeforeAllSteps(UserFlow flow);
        Task AfterAllSteps(UserFlow flow);
        Task BeforeEachStep(Step step, UserFlow flow);
        Task RunStep(Step step, UserFlow flow);
        Task AfterEachStep(Step step, UserFlow flow);
    }
}
