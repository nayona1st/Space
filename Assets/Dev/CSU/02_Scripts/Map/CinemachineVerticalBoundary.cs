using Unity.Cinemachine;
using UnityEngine;

namespace Dev.CSU._02_Scripts.Map
{
    [AddComponentMenu(
        "Cinemachine/Procedural/Extensions/Cinemachine Vertical Boundary")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CinemachineVerticalBoundary
        : CinemachineExtension
    {
        [Header("Boundary Source")]
        [Tooltip("MapBoarder controller that supplies the shared top and bottom playable world limits.")]
        [SerializeField] private MapBorderController mapBorderController;

        [Header("Camera Padding")]
        [Tooltip("Additional distance in Unity Units kept between the camera's top edge and the upper playable limit.")]
        [Min(0f)]
        [SerializeField] private float topCameraPadding;

        [Tooltip("Additional distance in Unity Units kept between the camera's bottom edge and the lower playable limit.")]
        [Min(0f)]
        [SerializeField] private float bottomCameraPadding;

        public MapBorderController MapBorderController =>
            mapBorderController;
        public float TopCameraPadding => topCameraPadding;
        public float BottomCameraPadding => bottomCameraPadding;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase virtualCamera,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body
                || mapBorderController == null
                || !state.Lens.Orthographic)
            {
                return;
            }

            Vector3 cameraPosition = state.GetCorrectedPosition();
            float halfViewHeight = Mathf.Max(
                0f,
                state.Lens.OrthographicSize);
            float lowerEdge = mapBorderController.BottomPlayableWorldY
                + bottomCameraPadding;
            float upperEdge = mapBorderController.TopPlayableWorldY
                - topCameraPadding;
            float minimumCenterY = lowerEdge + halfViewHeight;
            float maximumCenterY = upperEdge - halfViewHeight;

            float confinedCenterY;
            if (minimumCenterY > maximumCenterY)
            {
                confinedCenterY = (lowerEdge + upperEdge) * 0.5f;
            }
            else
            {
                confinedCenterY = Mathf.Clamp(
                    cameraPosition.y,
                    minimumCenterY,
                    maximumCenterY);
            }

            state.PositionCorrection += Vector3.up
                * (confinedCenterY - cameraPosition.y);
        }

        private void OnValidate()
        {
            topCameraPadding = Mathf.Max(0f, topCameraPadding);
            bottomCameraPadding = Mathf.Max(
                0f,
                bottomCameraPadding);
        }
    }
}
