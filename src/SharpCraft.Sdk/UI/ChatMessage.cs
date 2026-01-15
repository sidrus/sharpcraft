using System.Numerics;

namespace SharpCraft.Sdk.UI;

/// <summary>
/// Represents a message in the chat.
/// </summary>
/// <param name="Text">The message text.</param>
/// <param name="Color">The message color.</param>
public record ChatMessage(string Text, Vector4 Color);
