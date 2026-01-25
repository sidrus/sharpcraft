using AwesomeAssertions;
using SharpCraft.Client.Rendering.Shaders;
using Xunit;

namespace SharpCraft.Client.Tests.Rendering;

public class ShaderPreprocessorTests
{
    [Fact]
    public void Process_WithNoDirectives_ReturnsSourceUnchanged()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "void main() { }";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Be(source);
    }

    [Fact]
    public void Process_WithDefine_AddsSymbolToDefines()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#define MY_SYMBOL\n#ifdef MY_SYMBOL\nincluded\n#endif";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Trim().Should().Be("included");
    }

    [Fact]
    public void Process_WithDef_AddsSymbolToDefines()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#def MY_SYMBOL\n#ifdef MY_SYMBOL\nincluded\n#endif";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Trim().Should().Be("included");
    }

    [Fact]
    public void Process_WithIfndef_IncludesCodeWhenSymbolNotDefined()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#ifndef UNDEFINED_SYMBOL\nincluded\n#endif";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Trim().Should().Be("included");
    }

    [Fact]
    public void Process_WithIfndef_ExcludesCodeWhenSymbolDefined()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#define MY_SYMBOL\n#ifndef MY_SYMBOL\nexcluded\n#endif\nkept";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Trim().Should().Be("kept");
    }

    [Fact]
    public void Process_WithIfdef_ExcludesCodeWhenSymbolNotDefined()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#ifdef UNDEFINED_SYMBOL\nexcluded\n#endif\nkept";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Trim().Should().Be("kept");
    }

    [Fact]
    public void Process_WithIncludeGuard_IncludesContentOnce()
    {
        // Arrange
        var includeContent = "#ifndef MATH_GLSL\n#define MATH_GLSL\nconst float PI = 3.14;\n#endif";
        var files = new Dictionary<string, string>
        {
            { "C:\\shaders\\math.glsl", includeContent }
        };
        var preprocessor = new ShaderPreprocessor(path => files.GetValueOrDefault(path));
        var source = "#include \"math.glsl\"\n#include \"math.glsl\"\nvoid main() { }";

        // Act
        var result = preprocessor.Process(source, "C:\\shaders");

        // Assert
        var piCount = result.Split("const float PI").Length - 1;
        piCount.Should().Be(1);
        result.Should().Contain("void main()");
    }

    [Fact]
    public void Process_WithNestedIncludes_ResolvesCorrectly()
    {
        // Arrange
        var mathContent = "#ifndef MATH_GLSL\n#define MATH_GLSL\nconst float PI = 3.14;\n#endif";
        var brdfContent = "#ifndef BRDF_GLSL\n#define BRDF_GLSL\n#include \"math.glsl\"\nfloat brdf() { return PI; }\n#endif";
        var files = new Dictionary<string, string>
        {
            { "C:\\shaders\\Common\\math.glsl", mathContent },
            { "C:\\shaders\\Common\\brdf.glsl", brdfContent }
        };
        var preprocessor = new ShaderPreprocessor(path => files.GetValueOrDefault(path));
        var source = "#include \"Common\\brdf.glsl\"\nvoid main() { }";

        // Act
        var result = preprocessor.Process(source, "C:\\shaders");

        // Assert
        result.Should().Contain("const float PI");
        result.Should().Contain("float brdf()");
        result.Should().Contain("void main()");
    }

    [Fact]
    public void Process_WithNestedConditionals_HandlesCorrectly()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#define OUTER\n#ifdef OUTER\nouter_start\n#ifdef INNER\ninner\n#endif\nouter_end\n#endif";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Contain("outer_start");
        result.Should().Contain("outer_end");
        result.Should().NotContain("inner");
    }

    [Fact]
    public void Process_WithMissingIncludeFile_SkipsInclude()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#include \"missing.glsl\"\nvoid main() { }";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Trim().Should().Be("void main() { }");
    }

    [Fact]
    public void Process_PreservesLineContent()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "  indented line\n\ttabbed line";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Contain("  indented line");
        result.Should().Contain("\ttabbed line");
    }

    [Fact]
    public void Process_DefineInsideIfndef_WorksCorrectly()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "#ifndef GUARD\n#define GUARD\nprotected content\n#endif\n#ifndef GUARD\nduplicate\n#endif";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Contain("protected content");
        result.Should().NotContain("duplicate");
    }

    [Fact]
    public void Process_StripsSingleLineComments()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "void main() { // this is a comment\n    return;\n}";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Contain("void main() { ");
        result.Should().NotContain("this is a comment");
        result.Should().Contain("return;");
    }

    [Fact]
    public void Process_StripsMultiLineComments()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "void main() { /* multi\nline\ncomment */ return; }";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Contain("void main() { ");
        result.Should().NotContain("multi");
        result.Should().NotContain("comment");
        result.Should().Contain("return; }");
    }

    [Fact]
    public void Process_StripsCommentsWithNonAsciiCharacters()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "float angle = 0.0; // sun 0° to -6° below horizon\nvoid main() { }";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        result.Should().Contain("float angle = 0.0;");
        result.Should().NotContain("°");
        result.Should().NotContain("sun");
        result.Should().Contain("void main()");
    }

    [Fact]
    public void Process_PreservesLineNumbersInMultiLineComments()
    {
        // Arrange
        var preprocessor = new ShaderPreprocessor(_ => null);
        var source = "line1\n/* comment\nspanning\nlines */\nline5";

        // Act
        var result = preprocessor.Process(source, "C:\\test");

        // Assert
        var lines = result.Split('\n');
        lines.Should().HaveCount(5);
        lines[0].Should().Be("line1");
        lines[4].Should().Be("line5");
    }
}
