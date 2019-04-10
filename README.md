# CelesteBot
An application of NEAT learning, in which an AI attempts to learn how to play Celeste
## Getting Started
These instructions should help you get started running and developing for CelesteBot.
### Installing
CelesteBot uses [Everest](https://everestapi.github.io/), you should follow the [installation instructions for Everest](https://everestapi.github.io/#installing-everest). After installation is complete:
1. Clone the CelesteBot repo to anywhere you desire.
2. Copy organismNames.txt and speciesNames.txt from `CelesteBot/CelesteBot-Everest-Interop/bin/Debug` to the Celeste directory (where the .exe is located).
3. Create a file in the Celeste directory called fitness.fit (or copy it from the `Debug` directory as well).
4. Open `CelesteBot/CelesteBot-Everest-Interop/CelesteBot-Everest-Interop.csproj` in your favorite C# IDE (VisualStudio works best).
5. Build the project to `CelesteBot/CelesteBot-Everest-Interop/bin/Debug/CelesteBot-Everest-Interop.dll` (default on VisualStudio).
6. Don't forget to copy over the DLL and metadata file from the `Debug` folder to the `Mods` folder in your Celeste directory!

After that, you should be good to open Celeste and run CelesteBot.
### References
When developing for CelesteBot, you should ensure you have the following references present:
```
Celeste:
    Celeste
    FNA
    MMHOOK_Celeste
    Mono.Cecil
    MonoMod
    MonoMod.RuntimeDetour
    MonoMod.Utils
    YamlDotNet
.NET:
    Microsoft.CSharp
    System
    System.Core
    System.Runtime.Serialization
    System.Xml
```
### Controls
| Key | Function |
| --- | --- |
| \ | Begin training |
| , | Stop training, reset level |
| ' | Load generation specified by CelesteBot Mod setting "Checkpoint to Load" |
| A | Toggle fitness append mode |
| Space | Add fitness checkpoint to fitness.fit (Only in fitness append mode) |
| N | Hide brain |
| Shift + N | Show brain |
