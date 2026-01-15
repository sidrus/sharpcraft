using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharpCraft.Engine.Lifecycle;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Engine.Tests.Lifecycle;

public class ModLoaderTests
{
    public class TestMod(ISharpCraftSdk sdk) : IMod
    {
        public ModManifest Manifest => new(
            Id: "testmod",
            Name: "Test Mod",
            Author: "Test",
            Version: "1.0.0",
            Dependencies: [],
            Capabilities: [],
            Entrypoints: ["testmod.dll"]
        );

        public void OnEnable() { }
        public void OnDisable() { }
    }

    [Fact]
    public void LoadMods_ShouldDiscoverAndLoadMod()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var modDir = Path.Combine(tempDir, "testmod");
        Directory.CreateDirectory(modDir);

        var currentAssembly = Assembly.GetExecutingAssembly().Location;
        
        // Copy current assembly to mod dir as testmod.dll
        File.Copy(currentAssembly, Path.Combine(modDir, "testmod.dll"));

        var manifest = new ModManifest(
            Id: "testmod",
            Name: "Test Mod",
            Author: "Test",
            Version: "1.0.0",
            Dependencies: [],
            Capabilities: [],
            Entrypoints: ["testmod.dll"]
        );
        
        File.WriteAllText(Path.Combine(modDir, "mod.json"), JsonSerializer.Serialize(manifest));

        var sdkMock = new Mock<ISharpCraftSdk>();
        var loader = new ModLoader(NullLogger<ModLoader>.Instance, sdkMock.Object);

        try
        {
            // Act
            loader.LoadMods(tempDir);

            // Assert
            loader.LoadedMods.Should().Contain(m => m.Manifest.Id == "testmod");
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }
}
