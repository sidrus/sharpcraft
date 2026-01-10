using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering.Textures;

public class LinearTexture2d(GL gl, string path) : Texture2d(gl, path, InternalFormat.Rgba8);