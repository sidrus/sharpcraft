using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SharpCraft.Engine.Lifecycle;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Lifecycle;
using System.Reflection;
using System.Text.Json;

namespace SharpCraft.Engine.Tests.Lifecycle;

public class ModLoaderTests
{
    public class TestMod : IMod
    {
        // Reflection-loaded via Activator.CreateInstance(type, sdk); the SDK is unused here.
        public TestMod(ISharpCraftSdk sdk) => _ = sdk;

        public ModManifest Manifest => new(
            Id: "testmod",
            Name: "Test Mod",
            Author: "Test",
            Version: "1.0.0",
            Dependencies: [],
            Capabilities: [],
            Entrypoints: ["testmod.dll"]
        );

        public string BaseDirectory { get; set; } = string.Empty;

        public void OnEnable()
        {
        }
        public void OnDisable()
        {
        }
    }

    [Fact]
    public void DiscoverMods_MissingDirectory_ShouldReturnEmpty()
    {
        var loader = new ModLoader(NullLogger<ModLoader>.Instance, Substitute.For<ISharpCraftSdk>());

        var discovered = loader.DiscoverMods(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        discovered.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverMods_MalformedManifest_ShouldSkipWithoutThrowing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var modDir = Path.Combine(tempDir, "brokenmod");
        Directory.CreateDirectory(modDir);
        File.WriteAllText(Path.Combine(modDir, "mod.json"), "{ this is not valid json");

        var loader = new ModLoader(NullLogger<ModLoader>.Instance, Substitute.For<ISharpCraftSdk>());

        try
        {
            var discovered = loader.DiscoverMods(tempDir);

            discovered.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DiscoverMods_ValidMod_ShouldReturnInstantiatedMod()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var modDir = Path.Combine(tempDir, "testmod");
        Directory.CreateDirectory(modDir);
        File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(modDir, "testmod.dll"));

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

        var loader = new ModLoader(NullLogger<ModLoader>.Instance, Substitute.For<ISharpCraftSdk>());

        try
        {
            var discovered = loader.DiscoverMods(tempDir);

            discovered.Should().Contain(m => m.Manifest.Id == "testmod");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
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

        var sdkMock = Substitute.For<ISharpCraftSdk>();
        var loader = new ModLoader(NullLogger<ModLoader>.Instance, sdkMock);

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