using System.Collections;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    public interface IScreenFader
    {
        float Alpha { get; }

        void SetAlpha(float alpha);
        IEnumerator FadeTo(float targetAlpha, float duration);
    }
}
