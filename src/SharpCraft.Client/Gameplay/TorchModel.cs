using System.Numerics;

namespace SharpCraft.Client.Gameplay;

/// <summary>
/// The torch's static-mesh geometry and procedural texture: a thin tapered column whose wooden
/// handle is sun/ambient lit and whose burning head is emissive. Pure data (vertex layout
/// pos(3)/uv(2)/normal(3)) so the renderer stays generic and unaware that this mesh is a torch.
/// </summary>
public static class TorchModel
{
    // Model dimensions, in block units (1.0 == one block edge). A Minecraft-style torch is
    // 2px wide and ~10px tall, resting with its base on the supporting block's top face.
    private const float HalfThickness = 1.0f / 16.0f; // 2px wide overall
    private const float Height = 10.0f / 16.0f;

    public static float[] BuildMesh()
    {
        const float h = HalfThickness;
        const float t = Height;

        // The 8 box corners (base on the y=0 plane, centered on x/z).
        var c0 = new Vector3(-h, 0, -h);
        var c1 = new Vector3(h, 0, -h);
        var c2 = new Vector3(h, 0, h);
        var c3 = new Vector3(-h, 0, h);
        var c4 = new Vector3(-h, t, -h);
        var c5 = new Vector3(h, t, -h);
        var c6 = new Vector3(h, t, h);
        var c7 = new Vector3(-h, t, h);

        // Texture regions (16x16). Side faces sample the central 2px strip over the full height;
        // the top face samples the bright flame tip.
        const float su0 = 7.0f / 16.0f;
        const float su1 = 9.0f / 16.0f;
        const float tv0 = 13.0f / 16.0f;
        const float tv1 = 15.0f / 16.0f;

        var verts = new List<float>(6 * 6 * 8);

        // Side faces: (bottom-left, bottom-right, top-right, top-left) with v running 0..1 up the model.
        AddQuad(verts, c1, c2, c6, c5, new Vector3(1, 0, 0), su0, 0f, su1, 1f);  // +X
        AddQuad(verts, c3, c0, c4, c7, new Vector3(-1, 0, 0), su0, 0f, su1, 1f); // -X
        AddQuad(verts, c2, c3, c7, c6, new Vector3(0, 0, 1), su0, 0f, su1, 1f);  // +Z
        AddQuad(verts, c0, c1, c5, c4, new Vector3(0, 0, -1), su0, 0f, su1, 1f); // -Z
        // Top face (the glowing tip).
        AddQuad(verts, c4, c5, c6, c7, new Vector3(0, 1, 0), su0, tv0, su1, tv1);

        return verts.ToArray();
    }

    public static (int width, int height, byte[] pixels) BuildTexture()
    {
        const int size = 16;
        var data = new byte[size * size * 4]; // RGBA, fully transparent by default

        // Row 0 is the bottom of the texture (v=0) so the wooden base maps to the model's base.
        // Only the central 2px column (x=7,8) is sampled by the mesh; the rest stays transparent.
        for (var y = 0; y < size; y++)
        {
            (byte r, byte g, byte b) color;
            if (y <= 8)
            {
                // Wooden handle, with a faint grain alternating per row.
                color = (y & 1) == 0 ? ((byte)122, (byte)78, (byte)38) : ((byte)92, (byte)58, (byte)28);
            }
            else if (y <= 11)
            {
                color = (200, 70, 20); // smoldering ember
            }
            else if (y <= 13)
            {
                color = (255, 140, 30); // flame body
            }
            else
            {
                color = (255, 220, 90); // bright tip
            }

            for (var x = 7; x <= 8; x++)
            {
                var i = (y * size + x) * 4;
                data[i] = color.r;
                data[i + 1] = color.g;
                data[i + 2] = color.b;
                data[i + 3] = 255;
            }
        }

        return (size, size, data);
    }

    private static void AddQuad(List<float> verts, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
        Vector3 normal, float u0, float v0, float u1, float v1)
    {
        // Two triangles: bl, br, tr / bl, tr, tl.
        AddVertex(verts, bl, u0, v0, normal);
        AddVertex(verts, br, u1, v0, normal);
        AddVertex(verts, tr, u1, v1, normal);
        AddVertex(verts, bl, u0, v0, normal);
        AddVertex(verts, tr, u1, v1, normal);
        AddVertex(verts, tl, u0, v1, normal);
    }

    private static void AddVertex(List<float> verts, Vector3 pos, float u, float v, Vector3 normal)
    {
        verts.Add(pos.X);
        verts.Add(pos.Y);
        verts.Add(pos.Z);
        verts.Add(u);
        verts.Add(v);
        verts.Add(normal.X);
        verts.Add(normal.Y);
        verts.Add(normal.Z);
    }
}
