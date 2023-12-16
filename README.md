# XIVRunner

This is a repo in Dalamud for auto-moving the character by inputting positions.

This repo is heavily inspired by [awgil/ffxiv_visland](https://github.com/awgil/ffxiv_visland).

## Getting Started

Add XIVRunner as a submodule to your project:

```shell
git submodule add https://github.com/adambennett55/XIVRunner
```

Add it to your plugin's CSProj file:

```xml
<ItemGroup>
	<ProjectReference Include="..\XIVRunner\XIVRunner\XIVRunner.csproj" />
</ItemGroup>
```

Then, in the entry point of your plugin:

```c#
var runner = XIVRunner.XIVRunner.Create(pluginInterface);
runner.Enable = true;
```

where pluginInterface is a **DalamudPluginInterface**.

Don't forget to **dispose** it!

## Usage

The character will act in strict accordance with the order of points. So please make sure that your positions are valid. The `NaviPts` is type`Queue<Vector3>`. You can modify it freely.

```c#
runner.NaviPts.Enqueue(Service.ClientState.LocalPlayer.Position
	+ new System.Numerics.Vector3(10, 0, 0));
```

Besides, you can change the `MountId` to the mount it is called.
