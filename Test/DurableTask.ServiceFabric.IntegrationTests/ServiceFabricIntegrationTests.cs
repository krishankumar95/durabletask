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

namespace DurableTask.ServiceFabric.IntegrationTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestApplication.Common;

    /// <summary>
    /// Integration tests that run DTFX orchestrations against a local SF cluster.
    ///
    /// Prerequisites:
    ///   1. Local SF cluster running (Start-Service FabricHostSvc)
    ///   2. TestFabricApplication deployed (via DurableTask.ServiceFabric.Test.AssemblySetup
    ///      or manually via PowerShell: Connect-ServiceFabricCluster; New-ServiceFabricApplication)
    ///
    /// These tests verify that the delta1 package works correctly with SF Reliable
    /// Collections on the current TFM (net48 or net10.0).
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class ServiceFabricIntegrationTests
    {
        private const string TestAppAddress = "fabric:/TestFabricApplication/TestStatefulService";
        private IRemoteClient serviceClient;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceClient = ServiceProxy.Create<IRemoteClient>(
                new Uri(TestAppAddress),
                new ServicePartitionKey(1));
        }

        [TestMethod]
        public async Task Orchestration_WithScheduledTasks_CompletesSuccessfully()
        {
            var result = await this.serviceClient.RunOrchestrationAsync(
                "SimpleOrchestrationWithTasks",
                null,
                TimeSpan.FromMinutes(2));

            Assert.AreEqual(OrchestrationStatus.Completed, result.OrchestrationStatus);
            Assert.AreEqual("\"Hello Gabbar\"", result.Output);
        }

        [TestMethod]
        public async Task Orchestration_WithTimer_CompletesAfterWaitTime()
        {
            int waitSeconds = 10;
            var result = await this.serviceClient.RunOrchestrationAsync(
                "SimpleOrchestrationWithTimer",
                waitSeconds,
                TimeSpan.FromMinutes(2));

            Assert.AreEqual(OrchestrationStatus.Completed, result.OrchestrationStatus);
            Assert.AreEqual("\"Hello Gabbar\"", result.Output);

            var elapsed = result.CompletedTime - result.CreatedTime;
            Assert.IsTrue(elapsed > TimeSpan.FromSeconds(waitSeconds),
                $"Orchestration should take at least {waitSeconds}s, took {elapsed.TotalSeconds:F1}s");
        }

        [TestMethod]
        public async Task Orchestration_WithSubOrchestration_CompletesSuccessfully()
        {
            var result = await this.serviceClient.RunOrchestrationAsync(
                "SimpleOrchestrationWithSubOrchestration",
                null,
                TimeSpan.FromMinutes(2));

            Assert.AreEqual(OrchestrationStatus.Completed, result.OrchestrationStatus);
        }

        [TestMethod]
        public async Task Orchestration_CanQueryRunningInstances()
        {
            var running = await this.serviceClient.GetRunningOrchestrations();
            Assert.IsNotNull(running, "GetRunningOrchestrations should return a non-null collection");
        }

        [TestMethod]
        public async Task Orchestration_CanPurgeHistory()
        {
            // Run an orchestration first
            await this.serviceClient.RunOrchestrationAsync(
                "SimpleOrchestrationWithTasks",
                null,
                TimeSpan.FromMinutes(2));

            // Purge should not throw
            await this.serviceClient.PurgeOrchestrationHistoryEventsAsync();
        }

        [TestMethod]
        public async Task Orchestration_StartAndWait_RoundTrip()
        {
            // Start an orchestration and get the instance back
            var input = new TestOrchestrationData
            {
                NumberOfParallelTasks = 2,
                NumberOfSerialTasks = 1,
                MaxDelay = 0,
                MinDelay = 0,
                DelayUnit = TimeSpan.FromMilliseconds(1)
            };

            var instance = await this.serviceClient.StartTestOrchestrationAsync(input);
            Assert.IsNotNull(instance, "StartTestOrchestrationAsync should return an instance");
            Assert.IsFalse(string.IsNullOrEmpty(instance.InstanceId), "InstanceId should not be empty");

            // Wait for completion
            var state = await this.serviceClient.WaitForOrchestration(instance, TimeSpan.FromMinutes(2));
            Assert.IsNotNull(state, "WaitForOrchestration should return state");
            Assert.AreEqual(OrchestrationStatus.Completed, state.OrchestrationStatus);
        }

        [TestMethod]
        public async Task Orchestration_GetState_ReturnsValidState()
        {
            var input = new TestOrchestrationData
            {
                NumberOfParallelTasks = 1,
                NumberOfSerialTasks = 1,
                MaxDelay = 0,
                MinDelay = 0,
                DelayUnit = TimeSpan.FromMilliseconds(1)
            };

            var instance = await this.serviceClient.StartTestOrchestrationAsync(input);

            // Wait for it to complete
            await this.serviceClient.WaitForOrchestration(instance, TimeSpan.FromMinutes(2));

            // Query state
            var state = await this.serviceClient.GetOrchestrationState(instance);
            Assert.IsNotNull(state, "GetOrchestrationState should return state");
            Assert.AreEqual(instance.InstanceId, state.OrchestrationInstance.InstanceId);
        }

        [TestMethod]
        public async Task Orchestration_Terminate_StopsExecution()
        {
            var input = new TestOrchestrationData
            {
                NumberOfParallelTasks = 1,
                NumberOfSerialTasks = 1,
                MaxDelay = 60,
                MinDelay = 60,
                DelayUnit = TimeSpan.FromSeconds(1)
            };

            var instance = await this.serviceClient.StartTestOrchestrationAsync(input);

            // Give it a moment to start
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Terminate it
            await this.serviceClient.TerminateOrchestration(instance.InstanceId, "Integration test termination");

            // Wait and verify terminated
            var state = await this.serviceClient.WaitForOrchestration(instance, TimeSpan.FromMinutes(1));
            Assert.AreEqual(OrchestrationStatus.Terminated, state.OrchestrationStatus);
        }
    }
}
