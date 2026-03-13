# Service Fabric Integration Tests

These tests run DTFX orchestrations against a **real local Service Fabric cluster** 
to verify end-to-end compatibility of the delta1 package.

## Prerequisites

1. **Local SF cluster running**:
   ```powershell
   Start-Service FabricHostSvc
   # Or use SF Local Cluster Manager from system tray → Start Local Cluster (5 Node)
   ```

2. **TestFabricApplication deployed** — open `DurableTask.ServiceFabric.sln` in 
   Visual Studio, right-click `TestFabricApplication` → **Publish** to local cluster.
   
   Alternatively, if the app is already packaged:
   ```powershell
   Connect-ServiceFabricCluster
   Copy-ServiceFabricApplicationPackage -ApplicationPackagePath "Test\TestFabricApplication\TestFabricApplication\pkg\Debug" -ImageStoreConnectionString "fabric:ImageStore"
   Register-ServiceFabricApplicationType -ApplicationPathInImageStore "Debug"
   New-ServiceFabricApplication -ApplicationName "fabric:/TestFabricApplication" -ApplicationTypeName "TestFabricApplicationType" -ApplicationTypeVersion "1.0.0"
   ```

3. **Wait for the service to become healthy**:
   ```powershell
   Get-ServiceFabricApplicationHealth -ApplicationName "fabric:/TestFabricApplication"
   # Or check http://localhost:19080/Explorer
   ```

## Running

```powershell
# Run on .NET Framework 4.8
dotnet test Test\DurableTask.ServiceFabric.IntegrationTests -f net48 --filter TestCategory=Integration

# Run on .NET 10
dotnet test Test\DurableTask.ServiceFabric.IntegrationTests -f net10.0 --filter TestCategory=Integration
```

## What These Tests Prove

- The delta1 package can **start, run, and complete** DTFX orchestrations
  against SF Reliable Collections on both net48 and net10.0
- Orchestration state (stored in SF Reliable Dictionaries via
  `DataContractSerializer`) is correctly persisted and retrieved
- Timer, sub-orchestration, and termination scenarios work correctly
- The SF Remoting client can communicate with the deployed service from
  both TFMs

## Note on TestFabricApplication Build

The `TestFabricApplication` is a legacy SF application (.sfproj) that must be 
built through Visual Studio. It references our updated SDK-style 
`DurableTask.Framework` and `DurableTask.ServiceFabric` projects, and will 
automatically pick up the net48 output when built.
