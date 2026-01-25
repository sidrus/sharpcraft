namespace SharpCraft.Engine.Rendering.Textures;

public class ColorTexture2d(GL gl, string path) : Texture2d(gl, path, InternalFormat.SrgbAlpha);