using UnityEngine;
using UnityEngine.EventSystems;

namespace Submodules.Utility.UI
{
    public class FirstSelected : MonoBehaviour
    {
        private void OnEnable()
        {
            var current = EventSystem.current;

            if (current != null && current.firstSelectedGameObject == null)
                current.firstSelectedGameObject = gameObject;
        }
    }
}
