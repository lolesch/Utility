using Submodules.Utility.Attributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public sealed class LoadSceneButton : AbstractButton
    {
        [Space]
        [SerializeField, SceneRef] private string sceneToLoad;

        protected override void OnClick() => SceneProvider.Instance.LoadScene(sceneToLoad);
    }
}
