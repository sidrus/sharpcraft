using System.Text;

namespace SharpCraft.Engine.Rendering.Shaders;

/// <summary>
/// Preprocesses shader source code, handling #include, #define, #ifndef, #ifdef, and #endif directives.
/// Optimized for minimal allocations using Span-based parsing.
/// </summary>
public sealed class ShaderPreprocessor
{
    private readonly HashSet<string> _processedIncludes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _defines = new(StringComparer.Ordinal);
    private readonly Func<string, string?> _fileReader;
    private readonly StringBuilder _resultBuilder = new(4096);

    /// <summary>
    /// Creates a new shader preprocessor with the specified file reader function.
    /// </summary>
    /// <param name="fileReader">Function that reads file content given a path, returns null if file doesn't exist.</param>
    public ShaderPreprocessor(Func<string, string?> fileReader)
    {
        _fileReader = fileReader;
    }

    /// <summary>
    /// Preprocesses the shader source code, resolving includes and conditional compilation.
    /// </summary>
    /// <param name="source">The shader source code.</param>
    /// <param name="currentDir">The directory containing the shader file (for resolving relative includes).</param>
    /// <returns>The preprocessed shader source.</returns>
    public string Process(string source, string currentDir)
    {
        _processedIncludes.Clear();
        _defines.Clear();
        _resultBuilder.Clear();

        ProcessInternal(source.AsSpan(), currentDir);
        StripComments(_resultBuilder);

        return _resultBuilder.ToString();
    }

    /// <summary>
    /// Strips single-line (//) and multi-line (/* */) comments from shader source in-place.
    /// This prevents non-ASCII characters in comments from causing GPU compiler errors.
    /// </summary>
    private static void StripComments(StringBuilder sb)
    {
        var i = 0;
        while (i < sb.Length)
        {
            // Check for multi-line comment start
            if (i < sb.Length - 1 && sb[i] == '/' && sb[i + 1] == '*')
            {
                var start = i;
                i += 2;
                var newlineCount = 0;

                // Find end of comment, counting newlines
                while (i < sb.Length - 1 && !(sb[i] == '*' && sb[i + 1] == '/'))
                {
                    if (sb[i] == '\n')
                    {
                        newlineCount++;
                    }
                    i++;
                }

                var end = i < sb.Length - 1 ? i + 2 : sb.Length;

                // Replace comment with newlines to preserve line numbers
                sb.Remove(start, end - start);
                for (var j = 0; j < newlineCount; j++)
                {
                    sb.Insert(start + j, '\n');
                }
                i = start + newlineCount;
                continue;
            }

            // Check for single-line comment start
            if (i < sb.Length - 1 && sb[i] == '/' && sb[i + 1] == '/')
            {
                var start = i;
                i += 2;

                // Find end of line
                while (i < sb.Length && sb[i] != '\n')
                {
                    i++;
                }

                // Remove comment but keep newline
                sb.Remove(start, i - start);
                i = start;
                continue;
            }

            i++;
        }
    }

    private void ProcessInternal(ReadOnlySpan<char> source, string currentDir)
    {
        var conditionalStack = new Stack<bool>(8);
        var lineStart = 0;

        while (lineStart < source.Length)
        {
            // Find end of line
            var lineEnd = lineStart;
            while (lineEnd < source.Length && source[lineEnd] != '\n')
            {
                lineEnd++;
            }

            var line = source.Slice(lineStart, lineEnd - lineStart);
            var trimmedLine = line.Trim();

            var handled = ProcessDirective(trimmedLine, line, currentDir, conditionalStack);

            if (!handled && IsCodeActive(conditionalStack))
            {
                _resultBuilder.Append(line);
                if (lineEnd < source.Length)
                {
                    _resultBuilder.Append('\n');
                }
            }

            lineStart = lineEnd + 1;
        }
    }

    private bool ProcessDirective(
        ReadOnlySpan<char> trimmedLine,
        ReadOnlySpan<char> originalLine,
        string currentDir,
        Stack<bool> conditionalStack)
    {
        // Handle #def (custom directive for preprocessor-only symbols)
        if (trimmedLine.StartsWith("#def "))
        {
            if (IsCodeActive(conditionalStack))
            {
                var symbol = ExtractFirstToken(trimmedLine[5..]);
                if (symbol.Length > 0)
                {
                    _defines.Add(symbol.ToString());
                }
            }
            return true;
        }

        // Handle #define
        if (trimmedLine.StartsWith("#define "))
        {
            if (IsCodeActive(conditionalStack))
            {
                var afterDefine = trimmedLine[8..].Trim();
                var symbol = ExtractFirstToken(afterDefine);

                if (symbol.Length > 0)
                {
                    _defines.Add(symbol.ToString());

                    // If it has a value (more content after symbol), pass through to GLSL compiler
                    var afterSymbol = afterDefine[symbol.Length..].Trim();
                    if (afterSymbol.Length > 0)
                    {
                        _resultBuilder.Append(originalLine);
                        _resultBuilder.Append('\n');
                    }
                }
            }
            return true;
        }

        // Handle #ifndef
        if (trimmedLine.StartsWith("#ifndef "))
        {
            var symbol = trimmedLine[8..].Trim();
            var isDefined = _defines.Contains(symbol.ToString());
            conditionalStack.Push(!isDefined && IsCodeActive(conditionalStack));
            return true;
        }

        // Handle #ifdef
        if (trimmedLine.StartsWith("#ifdef "))
        {
            var symbol = trimmedLine[7..].Trim();
            var isDefined = _defines.Contains(symbol.ToString());
            conditionalStack.Push(isDefined && IsCodeActive(conditionalStack));
            return true;
        }

        // Handle #endif
        if (trimmedLine.SequenceEqual("#endif") ||
            trimmedLine.StartsWith("#endif ") ||
            trimmedLine.StartsWith("#endif//"))
        {
            if (conditionalStack.Count > 0)
            {
                conditionalStack.Pop();
            }
            return true;
        }

        // Handle #include
        if (trimmedLine.StartsWith("#include \""))
        {
            if (IsCodeActive(conditionalStack))
            {
                var closeQuote = trimmedLine[10..].IndexOf('"');
                if (closeQuote > 0)
                {
                    var includePath = trimmedLine.Slice(10, closeQuote).ToString();
                    var fullIncludePath = Path.GetFullPath(Path.Combine(currentDir, includePath));

                    if (!_processedIncludes.Contains(fullIncludePath))
                    {
                        var includeContent = _fileReader(fullIncludePath);
                        if (includeContent != null)
                        {
                            _processedIncludes.Add(fullIncludePath);
                            ProcessInternal(
                                includeContent.AsSpan(),
                                Path.GetDirectoryName(fullIncludePath) ?? currentDir);
                        }
                    }
                }
            }
            return true;
        }

        return false;
    }

    private static ReadOnlySpan<char> ExtractFirstToken(ReadOnlySpan<char> span)
    {
        span = span.Trim();
        var end = 0;
        while (end < span.Length && span[end] != ' ' && span[end] != '\t')
        {
            end++;
        }
        return span[..end];
    }

    private static bool IsCodeActive(Stack<bool> conditionalStack)
    {
        return conditionalStack.Count == 0 || conditionalStack.Peek();
    }
}