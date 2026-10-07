namespace Reflect.Core.Interfaces
{
    public interface INtfsManager
    {
        void CreateJunction(string junctionPoint, string targetDir);
        bool IsJunction(string path);
    }
}
