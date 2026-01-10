using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering.Textures;

public class ColorTexture2d(GL gl, string path) : Texture2d(gl, path, InternalFormat.SrgbAlpha);