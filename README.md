# RdmpExtensions
A collection of non-core RDMP functionality.
This includes:

* Automation Plugins

    Allows for Automation of extractions
* Interactive Plugins

    Contains the DeAnonymise functionality to deanonymise against a cohort
* Python

    Allows for python scripts to be ran as data providers
* Release Plugin

    Allows for releases from remote RDMP instances
* StatsScriptsExecution Plugin
    
    Allows for R scripts to be ran from RDMP

## Building
Before Building, ensure the version number is correct within the rdmpextension.nuspec and sharedAssembly.info file
You will also need 7zip or an equivalent installed.

You can build this plugin ready for upload to an RDMP instance using:

```bash
dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/windows/windows.csproj -c Release -o p/windows
dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/main/main.csproj -c Release -o p/main
7z a -tzip Rdmp.Extensions.Plugin.6.2.0.nupkg rdmpextension.nuspec p
dotnet run --project RDMP/Tools/rdmp/rdmp.csproj -c Release -- pack -p --file Rdmp.Extensions.Plugin.6.2.0.nupkg --dir yaml
```
Once built you will have a file called ` Rdmp.Extensions.Plugin.6.2.0.nupkg` 

Upload it to RDMP using

```bash
./rdmp pack -f Z:\Repos\RdmpExtensions\Rdmp.Extensions.Plugin.6.2.0.nupkg
```
_Upload into RDMP. Or use the gui client 'Plugins' node under the Tables(Advanced) toolbar button_
