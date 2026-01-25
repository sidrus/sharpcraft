namespace SharpCraft.Client.Rendering.Shaders;

/// <summary>
/// Preprocesses shader source code, handling #include, #define, #ifndef, #ifdef, and #endif directives.
/// </summary>
public class ShaderPreprocessor
{
    private readonly HashSet<string> _processedIncludes = new();
    private readonly HashSet<string> _defines = new();
    private readonly Func<string, string?> _fileReader;

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
        var processed = ProcessInternal(source, currentDir);
        return StripComments(processed);
    }

    /// <summary>
    /// Strips single-line (//) and multi-line (/* */) comments from shader source.
    /// This prevents non-ASCII characters in comments from causing GPU compiler errors.
    /// </summary>
    private static string StripComments(string source)
    {
        var result = new System.Text.StringBuilder(source.Length);
        var i = 0;

        while (i < source.Length)
        {
            // Check for multi-line comment start
            if (i < source.Length - 1 && source[i] == '/' && source[i + 1] == '*')
            {
                i += 2;
                // Skip until we find */
                while (i < source.Length - 1 && !(source[i] == '*' && source[i + 1] == '/'))
                {
                    // Preserve newlines to maintain line numbers for error messages
                    if (source[i] == '\n')
                    {
                        result.Append('\n');
                    }
                    i++;
                }
                i += 2; // Skip */
                continue;
            }

            // Check for single-line comment start
            if (i < source.Length - 1 && source[i] == '/' && source[i + 1] == '/')
            {
                i += 2;
                // Skip until end of line
                while (i < source.Length && source[i] != '\n')
                {
                    i++;
                }
                // Don't skip the newline itself - let the normal path handle it
                continue;
            }

            result.Append(source[i]);
            i++;
        }

        return result.ToString();
    }

    private string ProcessInternal(string source, string currentDir)
    {
        var lines = source.Split('\n');
        var result = new List<string>();
        var conditionalStack = new Stack<bool>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            // Handle #def (custom directive for preprocessor-only symbols)
            if (trimmedLine.StartsWith("#def "))
            {
                if (IsCodeActive(conditionalStack))
                {
                    var symbol = trimmedLine.Substring(5).Split(' ', '\t')[0].Trim();
                    if (!string.IsNullOrEmpty(symbol))
                    {
                        _defines.Add(symbol);
                    }
                }
                continue;
            }

            // Handle #define - check if it's a symbol-only define (for include guards) or a value define
            if (trimmedLine.StartsWith("#define "))
            {
                if (IsCodeActive(conditionalStack))
                {
                    var afterDefine = trimmedLine.Substring(8).Trim();
                    var parts = afterDefine.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
                    var symbol = parts.Length > 0 ? parts[0] : "";
                    var hasValue = parts.Length > 1;

                    if (!string.IsNullOrEmpty(symbol))
                    {
                        _defines.Add(symbol);
                    }

                    // If it has a value, pass through to GLSL compiler
                    if (hasValue)
                    {
                        result.Add(line);
                    }
                }
                continue;
            }

            // Handle #ifndef
            if (trimmedLine.StartsWith("#ifndef "))
            {
                var symbol = trimmedLine.Substring(8).Trim();
                var isDefined = _defines.Contains(symbol);
                conditionalStack.Push(!isDefined && IsCodeActive(conditionalStack));
                continue;
            }

            // Handle #ifdef
            if (trimmedLine.StartsWith("#ifdef "))
            {
                var symbol = trimmedLine.Substring(7).Trim();
                var isDefined = _defines.Contains(symbol);
                conditionalStack.Push(isDefined && IsCodeActive(conditionalStack));
                continue;
            }

            // Handle #endif
            if (trimmedLine == "#endif" || trimmedLine.StartsWith("#endif ") || trimmedLine.StartsWith("#endif//"))
            {
                if (conditionalStack.Count > 0)
                {
                    conditionalStack.Pop();
                }
                continue;
            }

            // Handle #include
            if (trimmedLine.StartsWith("#include \""))
            {
                if (IsCodeActive(conditionalStack))
                {
                    var includePath = trimmedLine.Substring(10, trimmedLine.Length - 11);
                    var fullIncludePath = Path.GetFullPath(Path.Combine(currentDir, includePath));

                    if (_processedIncludes.Contains(fullIncludePath))
                    {
                        continue;
                    }

                    var includeContent = _fileReader(fullIncludePath);
                    if (includeContent != null)
                    {
                        _processedIncludes.Add(fullIncludePath);
                        var processedContent = ProcessInternal(
                            includeContent,
                            Path.GetDirectoryName(fullIncludePath)!);
                        result.Add(processedContent);
                    }
                }
                continue;
            }

            // Regular line - only include if code is active
            if (IsCodeActive(conditionalStack))
            {
                result.Add(line);
            }
        }

        return string.Join('\n', result);
    }

    private static bool IsCodeActive(Stack<bool> conditionalStack)
    {
        return conditionalStack.Count == 0 || conditionalStack.Peek();
    }
}
