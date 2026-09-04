namespace Submodules.Utility.UI
{
    public interface IView<T>
    {
        void Refresh(T data);
    }
}
