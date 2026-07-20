using AwesomeAssertions;
using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Rendering;
using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Tests.Assets;

public class TextureLoaderTests
{
    [Fact]
    public void LoadTexturesFromAtlas_WhenFileDoesNotExist_ShouldReturnFallbackTextures()
    {
        // Arrange
        var mapping = new Dictionary<string, int>
        {
            { "test1", 0 },
            { "test2", 1 }
        };
        var material = new Material("non_existent.png");

        // Act
        var result = TextureLoader.LoadTexturesFromAtlas(material, mapping).ToList();

        // Assert
        result.Count.Should().Be(2);
        result[0].name.Should().Be("test1");
        result[0].data.Width.Should().Be(16);
        result[0].data.Height.Should().Be(16);
        result[0].data.Data.Length.Should().Be(16 * 16 * 4);

        // Check for purple fallback (255, 0, 255, 255)
        result[0].data.Data[0].Should().Be(255);
        result[0].data.Data[1].Should().Be(0);
        result[0].data.Data[2].Should().Be(255);
        result[0].data.Data[3].Should().Be(255);

        result[1].name.Should().Be("test2");
    }

    [Fact]
    public void LoadTexturesFromAtlas_WithMetallicAndRoughness_ShouldHandleThem()
    {
        // Arrange
        var mapping = new Dictionary<string, int> { { "test", 0 } };
        var material = new Material("non_existent.png")
        {
            MetallicPath = "non_existent_metallic.png",
            RoughnessPath = "non_existent_roughness.png"
        };

        // Act
        var result = TextureLoader.LoadTexturesFromAtlas(material, mapping).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].data.MetallicData.Should().BeNull();
        result[0].data.RoughnessData.Should().BeNull();
        result[0].data.SpecularData.Should().BeNull();
    }

    [Fact]
    public void TextureData_ShouldAssignPropertiesCorrectly()
    {
        // Arrange
        var data = new byte[] { 1 };
        var normal = new byte[] { 2 };
        var ao = new byte[] { 3 };
        var metallic = new byte[] { 4 };
        var roughness = new byte[] { 5 };

        // Act
        var textureData = new TextureData(1, 1, data, normal, ao, null, metallic, roughness);

        // Assert
        textureData.Data.Should().BeSameAs(data);
        textureData.NormalData.Should().BeSameAs(normal);
        textureData.AoData.Should().BeSameAs(ao);
        textureData.SpecularData.Should().BeNull();
        textureData.MetallicData.Should().BeSameAs(metallic);
        textureData.RoughnessData.Should().BeSameAs(roughness);
    }
}