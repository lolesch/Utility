namespace Submodules.Utility.UI
{
    public interface IDisplay<in T>
    {
        void Repaint(T data);
    }
}
