using UnityEngine;

namespace Game.UI
{
    public abstract class PauseMenuPageBase : MonoBehaviour
    {
        public string PageTitle { get; protected set; }
        public virtual bool IsBusy => false;

        public virtual void Initialize(RectTransform pageRoot, Font font, Color normalColor, Color mutedColor)
        {
        }

        public virtual void Tick(float unscaledDeltaTime)
        {
        }

        public virtual void HandleInput()
        {
        }

        public virtual void OnMenuOpened()
        {
        }

        public virtual void OnMenuClosed()
        {
        }
    }
}
