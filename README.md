# scalaway-object-storage-csharp
Simply a file that we use to interact with the Scaleway Object Storage API

## How to use
- Install NuGet package https://www.nuget.org/packages/Aws4RequestSigner/ 
- Add `ScwObjectStorage.cs` to your project

## Examples
``` csharp
// Get the last version
Console.WriteLine(await ScwObjectStorage.GetStringObjectByName("myBucket", "myObjectFile.txt"));
```

``` csharp
// Get all versions
Dictionary<DateTime, string> versions = await ScwObjectStorage.GetAllVersionsFromAnObject("myBucket", "myObjectFile.txt");
foreach (var item in versions)
{
    Console.WriteLine("Version " + item.Key.ToString("dd/MM/yyyy HH:mm:ss"));
    Console.WriteLine(await ScwObjectStorage.GetStringObjectByName("myBucket", "myObjectFile.txt", item.Value));
}
```
