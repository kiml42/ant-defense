using UnityEngine;

namespace Assets.Scripts.Helpers
{
    internal static class MouseHelper
    {
        public static bool RaycastToFloor(out RaycastHit hit)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            return Physics.Raycast(ray, out hit, 500, TranslateHandle.Instance.GroundLayermask, QueryTriggerInteraction.Ignore);
        }
    }
}
