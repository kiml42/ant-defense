using UnityEngine;

namespace Assets.Scripts.Helpers
{
    internal static class MouseHelper
    {
        private static int? _groundLayermaskValue;
        public static int GroundLayermask
        {
            get
            {
                _groundLayermaskValue ??= LayerMask.GetMask("Ground");
                return _groundLayermaskValue.Value;
            }
        }
        public static bool RaycastToMouse(out RaycastHit hit, bool floorOnly = true)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            var layermask = floorOnly ? GroundLayermask : Physics.DefaultRaycastLayers;
            return Physics.Raycast(ray, out hit, 500, layermask, QueryTriggerInteraction.Ignore);
        }
    }
}
