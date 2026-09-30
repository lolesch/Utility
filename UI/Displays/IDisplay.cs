namespace Submodules.Utility.UI
{
    public interface IDisplay<in T>
    {
        void Refresh(T data);
    }
}
