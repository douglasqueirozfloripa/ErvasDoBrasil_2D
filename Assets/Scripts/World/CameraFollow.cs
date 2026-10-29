using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Câmera top-down que segue a jogadora, presa aos limites do mapa e tremendo com a ansiedade.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Smooth = 6f;
        Camera cam;
        bool loggedFirst;

        static readonly Color Dia = new Color(0.47f, 0.62f, 0.33f);
        static readonly Color Noite = new Color(0.08f, 0.10f, 0.20f);

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        void LateUpdate()
        {
            if (!loggedFirst)
            {
                loggedFirst = true;
                LifecycleLog.Add("CameraFollow.LateUpdate(): primeira vez nesta cena, depois de todos os Update()");
            }
            var gm = GameManager.I;
            if (Target == null || gm == null) return;

            // no iPhone em paisagem a tela é larga: um pouco mais de zoom
            cam.orthographicSize = Application.isMobilePlatform ? 4.6f : 5.5f;

            Vector3 desired = Target.position;
            var b = gm.WorldBounds;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            desired.x = b.width > halfW * 2 ? Mathf.Clamp(desired.x, b.xMin + halfW, b.xMax - halfW) : b.center.x;
            desired.y = b.height > halfH * 2 ? Mathf.Clamp(desired.y, b.yMin + halfH, b.yMax - halfH) : b.center.y;

            Vector3 p = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-Smooth * Time.deltaTime));
            float shake = gm.Session.Status.ScreenShake;
            if (shake > 0f) p += (Vector3)(0.06f * shake * Random.insideUnitCircle);
            p.z = -10f;
            transform.position = p;

            cam.backgroundColor = Color.Lerp(Dia, Noite, gm.Session.Clock.Darkness);
        }
    }
}
