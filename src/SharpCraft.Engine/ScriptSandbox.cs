using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Messaging;

namespace SharpCraft.Engine;

/// <summary>
/// Provides a sandbox for Tier 2 scripts.
/// </summary>
public class ScriptSandbox
{
    private readonly ScriptOptions _options = ScriptOptions.Default
        .WithReferences(typeof(IBlockRegistry).Assembly, typeof(IMessageChannel).Assembly)
        .WithImports(
            "System", 
            "System.Collections.Generic", 
            "SharpCraft.Sdk", 
            "SharpCraft.Sdk.Blocks",
            "SharpCraft.Sdk.Commands",
            "SharpCraft.Sdk.Messaging",
            "SharpCraft.Sdk.Physics",
            "SharpCraft.Sdk.Universe"
        );

    public async Task<T> ExecuteAsync<T>(string code, object? globals = null, CancellationToken ct = default)
    {
        return await CSharpScript.EvaluateAsync<T>(code, _options, globals, globals?.GetType(), ct);
    }
}
