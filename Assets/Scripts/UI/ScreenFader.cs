using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeanBlaster.UI
{
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private Graphic overlay;
        public IEnumerator Fade(float targetAlpha, float duration)
        {
            if (overlay == null) yield break;
            Color start = overlay.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = start;
                color.a = Mathf.Lerp(start.a, targetAlpha, duration <= 0f ? 1f : elapsed / duration);
                overlay.color = color;
                yield return null;
            }
        }
    }
}
