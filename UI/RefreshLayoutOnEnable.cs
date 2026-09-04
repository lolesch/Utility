using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class RefreshLayoutOnEnable : MonoBehaviour
    {
        //private void Start() => (transform as RectTransform).RefreshContentFitter();

        private void OnEnable() => (transform as RectTransform).RefreshContentFitter();
    }
}