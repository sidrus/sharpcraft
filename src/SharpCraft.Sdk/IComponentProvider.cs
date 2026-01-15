
namespace SharpCraft.Sdk;

public interface IComponentProvider
{
    public T? GetComponent<T>() where T : class;
    public void AddComponent<T>(T component) where T : class;
}