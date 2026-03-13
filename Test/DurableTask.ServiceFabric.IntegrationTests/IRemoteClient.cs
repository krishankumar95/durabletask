// ----------------------------------------------------------------------------------
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace TestApplication.Common
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using DurableTask;
    using Microsoft.ServiceFabric.Services.Remoting;

    // Replicated from DurableTask.Test.Orchestrations.Perf to avoid legacy project dependency.
    // Must match exactly for SF Remoting DataContract serialization.
    [DataContract]
    [KnownType(typeof(TestOrchestrationData))]
    public class TestOrchestrationData
    {
        [DataMember] public int NumberOfParallelTasks { get; set; }
        [DataMember] public int NumberOfSerialTasks { get; set; }
        [DataMember] public int MaxDelay { get; set; }
        [DataMember] public int MinDelay { get; set; }
        [DataMember] public TimeSpan DelayUnit { get; set; }
        [DataMember] public bool UseTimeoutTask { get; set; }
        [DataMember] public TimeSpan ExecutionTimeout { get; set; }
    }

    [DataContract]
    [KnownType(typeof(DriverOrchestrationData))]
    public class DriverOrchestrationData
    {
        [DataMember] public int NumberOfParallelOrchestrations { get; set; }
        [DataMember] public TestOrchestrationData SubOrchestrationData { get; set; }
    }

    /// <summary>
    /// Service Remoting interface matching the deployed TestStatefulService.
    /// Must match TestApplication.Common.IRemoteClient exactly (same namespace,
    /// same method signatures) for SF Remoting dispatch.
    /// </summary>
    public interface IRemoteClient : IService
    {
        Task<IEnumerable<OrchestrationInstance>> GetRunningOrchestrations();
        Task<string> GetOrchestrationRuntimeState(string instanceId);
        Task<OrchestrationState> RunOrchestrationAsync(string orchestrationTypeName, object input, TimeSpan waitTimeout);
        Task<OrchestrationState> RunDriverOrchestrationAsync(DriverOrchestrationData input, TimeSpan waitTimeout);
        Task<OrchestrationInstance> StartTestOrchestrationAsync(TestOrchestrationData input);
        Task<OrchestrationState> GetOrchestrationState(OrchestrationInstance instance);
        Task<OrchestrationInstance> StartTestOrchestrationWithInstanceIdAsync(string instanceId, TestOrchestrationData input);
        Task<OrchestrationState> GetOrchestrationStateWithInstanceId(string instanceId);
        Task<OrchestrationState> WaitForOrchestration(OrchestrationInstance instance, TimeSpan waitTimeout);
        Task PurgeOrchestrationHistoryEventsAsync();
        Task TerminateOrchestration(string instanceId, string reason);
    }
}
