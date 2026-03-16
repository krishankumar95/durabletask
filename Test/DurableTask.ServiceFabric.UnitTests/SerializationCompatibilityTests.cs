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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using DurableTask;
using DurableTask.History;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DurableTask.ServiceFabric.UnitTests
{
    /// <summary>
    /// These tests verify that data serialized by the original charlie9 package
    /// (net451) can be deserialized by the current build on both net48 and net10.0.
    /// This proves SF Reliable Dictionary data compatibility across the migration.
    ///
    /// The golden .bin files in GoldenData/ were generated using the charlie9 NuGet
    /// DLL (DurableTask.ServiceFabric 1.0.1.0) and DataContractSerializer — the same
    /// serializer that SF Reliable Collections uses internally.
    /// </summary>
    [TestClass]
    public class SerializationCompatibilityTests
    {
        private static byte[] LoadGoldenData(string fileName)
        {
            string testDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(testDir, "GoldenData", fileName);
            Assert.IsTrue(File.Exists(path), $"Golden data file not found: {path}");
            return File.ReadAllBytes(path);
        }

        [TestMethod]
        public void PersistentSession_DeserializeCharlie9Data()
        {
            byte[] charlie9Bytes = LoadGoldenData("PersistentSession_charlie9.bin");

            var serializer = new DataContractSerializer(typeof(PersistentSession));
            PersistentSession session;
            using (var ms = new MemoryStream(charlie9Bytes))
            {
                session = (PersistentSession)serializer.ReadObject(ms);
            }

            Assert.IsNotNull(session, "Deserialized session should not be null");
            Assert.AreEqual("test-compat-instance-001", session.SessionId.InstanceId);
            Assert.AreEqual("exec-compat-001", session.SessionId.ExecutionId);
            Assert.AreEqual(8, session.SessionState.Count, "Should have 8 history events (1 start + 3*2 task + 1 completed)");

            // Verify first event is ExecutionStartedEvent
            Assert.IsInstanceOfType(session.SessionState[0], typeof(ExecutionStartedEvent));

            // Verify last event is ExecutionCompletedEvent
            Assert.IsInstanceOfType(session.SessionState[7], typeof(ExecutionCompletedEvent));
        }

        [TestMethod]
        public void TaskMessageItem_DeserializeCharlie9Data()
        {
            byte[] charlie9Bytes = LoadGoldenData("TaskMessageItem_charlie9.bin");

            var serializer = new DataContractSerializer(typeof(TaskMessageItem));
            TaskMessageItem item;
            using (var ms = new MemoryStream(charlie9Bytes))
            {
                item = (TaskMessageItem)serializer.ReadObject(ms);
            }

            Assert.IsNotNull(item, "Deserialized item should not be null");
            Assert.IsNotNull(item.TaskMessage, "TaskMessage should not be null");
            Assert.AreEqual("test-compat-instance-001", item.TaskMessage.OrchestrationInstance.InstanceId);
            Assert.AreEqual("exec-compat-001", item.TaskMessage.OrchestrationInstance.ExecutionId);
            Assert.AreEqual(42, item.TaskMessage.SequenceNumber);
            Assert.IsInstanceOfType(item.TaskMessage.Event, typeof(TaskScheduledEvent));
        }

        [TestMethod]
        public void PersistentSession_ReserializeMatchesCharlie9()
        {
            // Deserialize charlie9 data, then re-serialize and verify byte-for-byte match.
            // This proves the DataContract is identical — critical for SF Reliable Collections
            // which store serialized bytes directly.
            byte[] charlie9Bytes = LoadGoldenData("PersistentSession_charlie9.bin");

            var serializer = new DataContractSerializer(typeof(PersistentSession));

            PersistentSession session;
            using (var ms = new MemoryStream(charlie9Bytes))
            {
                session = (PersistentSession)serializer.ReadObject(ms);
            }

            byte[] reserializedBytes;
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, session);
                reserializedBytes = ms.ToArray();
            }

            CollectionAssert.AreEqual(charlie9Bytes, reserializedBytes,
                "Re-serialized bytes must match charlie9 original — DataContract format must be identical");
        }

        [TestMethod]
        public void TaskMessageItem_ReserializeMatchesCharlie9()
        {
            byte[] charlie9Bytes = LoadGoldenData("TaskMessageItem_charlie9.bin");

            var serializer = new DataContractSerializer(typeof(TaskMessageItem));

            TaskMessageItem item;
            using (var ms = new MemoryStream(charlie9Bytes))
            {
                item = (TaskMessageItem)serializer.ReadObject(ms);
            }

            byte[] reserializedBytes;
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, item);
                reserializedBytes = ms.ToArray();
            }

            CollectionAssert.AreEqual(charlie9Bytes, reserializedBytes,
                "Re-serialized bytes must match charlie9 original — DataContract format must be identical");
        }
    }
}
