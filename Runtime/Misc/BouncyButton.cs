using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Snm.Components.UI
{

    public class BouncyButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UnityEvent onClick;

        private Vector3 _orignalScale;
        private Coroutine _coroutine;
        private bool _interactable = true;

        public event Action<BouncyButton> OnClicked;

        private void Start()
        {
            _orignalScale = transform.localScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
            _coroutine = StartCoroutine(BounceAnimation());

            if (_interactable)
            {
                onClick?.Invoke();
                OnClicked?.Invoke(this);
            }
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        IEnumerator BounceAnimation()
        {
            var bounceDuration = .25f;
            var elapsedTime = 0f;
            var bounceAmplitude = .1f;

            while (elapsedTime < bounceDuration)
            {
                var t = elapsedTime / bounceDuration;
                t = Mathf.SmoothStep(0, 1, t);
                var scale = Mathf.Sin(t * Mathf.PI) * bounceAmplitude;
                transform.localScale = _orignalScale - Vector3.one * scale;

                elapsedTime += Time.deltaTime;

                yield return null;
            }

            transform.localScale = _orignalScale;
            _coroutine = null;
        }
    }
}
