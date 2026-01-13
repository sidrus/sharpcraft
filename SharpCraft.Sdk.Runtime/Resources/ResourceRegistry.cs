using SharpCraft.Sdk.Resources;

namespace SharpCraft.Sdk.Runtime.Resources;

/// <summary>
/// A registry that enforces namespaced resource locations.
/// </summary>
/// <typeparam name="T">The type of the resource.</typeparam>
public class ResourceRegistry<T> : Registry<T>
{
    public override void Register(string id, T item)
    {
        if (!ResourceLocation.TryParse(id, out _))
        {
            throw new ArgumentException($"Invalid resource ID: '{id}'. All resources must follow the 'namespace:path' pattern.", nameof(id));
        }

        base.Register(id, item);
    }
}
