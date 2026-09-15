using Submodules.Utility.Attributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class LoadSceneButton : AbstractButton
    {
        [Space]
        [SerializeField, SceneRef] protected string sceneToLoad;

        protected override void OnClick() => SceneProvider.Instance.LoadScene(sceneToLoad);
    }
}
