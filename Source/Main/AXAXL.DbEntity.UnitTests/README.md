# Note on Unit Tests

## Export Node Map to file for debugging

`DbService` is instantiated via DI container before any test start utilizing `AssemblyInitialize` annotation
(see [reference](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.assemblyinitializeattribute?view=mstest-net-1.2.0)).  If you want to
print `NodeMap` for troubleshooting, create an environment named `DbEntity__NodeMapExport` which provides a full file path.  As 
you can see in `CommonTestContext.cs`, this variable will be used to indicate to the `DbService` to print `NodeMap` to file after `Bootstrap` is done.