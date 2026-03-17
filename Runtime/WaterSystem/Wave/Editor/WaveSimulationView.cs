using Snm.Reactivity;
using Snm.Reactivity.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationView
    {
        private static readonly string[] DisplayModeLabels =
        {
            "Height",
            "Normal",
            "Heightfield"
        };

        private readonly VisualElement _root;

        private Image _waveImage;

        private IWaveSimulation _simulation;

        private readonly Signal<float> radius = new(0.02f);
        private readonly Signal<float> strength = new(1f);
        private readonly Signal<int> _displayModeSignal = new(0);

        private bool _dragging;
        private Vector2 _lastUV;
        public VisualElement Root => _root;

        public WaveSimulationView()
        {
            _root = new VisualElement();
        }

        public void Attach(IWaveSimulation simulation)
        {
            Detach();

            _simulation = simulation;

            BuildUI();

            _waveImage.image = simulation.GetDisplayTexture();

            _waveImage.schedule.Execute(_ =>
            {
                _waveImage.MarkDirtyRepaint();
            }).Every(50);
        }

        public void Detach()
        {
            _root.Clear();
            _simulation = null;
        }

        void BuildUI()
        {
            var field_Strength = new FloatField() { label = "Strength", value = strength.Value };
            var field_Radius = new FloatField() { label = "Radius", value = radius.Value };
            var displayLabel = new Label("Wave Display - Height");

            // Quick action buttons
            var buttonRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 4 }
            };

            var randomDropBtn = new Button { text = "Random Drop" };
            randomDropBtn.clicked += AddRandomDisturbance;

            var clearBtn = new Button { text = "Clear Waves" };
            clearBtn.clicked += ClearWaves;

            var centerDropBtn = new Button { text = "Center Drop" };
            centerDropBtn.clicked += () => AddDisturbance(new Vector2(0.5f, 0.5f));

            var toggleModeBtn = new Button { text = "Toggle Display Mode" };
            toggleModeBtn.clicked += ToggleDisplayMode;

            buttonRow.Add(randomDropBtn);
            buttonRow.Add(centerDropBtn);
            buttonRow.Add(clearBtn);
            buttonRow.Add(toggleModeBtn);

            _root.Add(field_Radius);
            _root.Add(field_Strength);
            _root.Add(buttonRow);
            _root.Add(displayLabel);

            SignalUIInput.BindTwoWayFloat(field_Strength, strength);
            SignalUIInput.BindTwoWayFloat(field_Radius, radius);
            var effect_DisplayMode = new Effect(() =>
            {
                displayLabel.text = "Wave Display - " + DisplayModeLabels[_displayModeSignal.Value];
                if (_simulation != null) _simulation.Config.displayMode = _displayModeSignal.Value;
            });
            UIBindingUtil.AutoDispose(displayLabel, effect_DisplayMode);

            _waveImage = new Image
            {
                style =
                {
                    flexGrow = 1,
                    minHeight = 300
                }
            };

            _waveImage.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _waveImage.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _waveImage.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _waveImage.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            _root.Add(_waveImage);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (_simulation == null) return;

            _dragging = true;

            var uv = GetUV(evt.localMousePosition);
            AddDisturbance(uv);

            _lastUV = uv;
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_dragging || _simulation == null) return;

            var uv = GetUV(evt.localMousePosition);

            if (Vector2.Distance(uv, _lastUV) > 0.01f)
            {
                AddDisturbance(uv);
                _lastUV = uv;
            }
        }

        void OnMouseUp(MouseUpEvent evt) => _dragging = false;
        void OnMouseLeave(MouseLeaveEvent evt) => _dragging = false;

        void AddDisturbance(Vector2 uv)
        {
            _simulation.AddDisturbance(new WaveDisturbance
            {
                uvPos = uv,
                radius = radius.Value,
                strength = strength.Value
            });
        }

        private void AddRandomDisturbance()
        {
            var uv = new Vector2(
                Random.Range(0.1f, 0.9f),
                Random.Range(0.1f, 0.9f)
            );
            AddDisturbance(uv);
        }

        private void ToggleDisplayMode()
        {
            if (_displayModeSignal == null) return;

            _displayModeSignal.Value = (_displayModeSignal.Value + 1) % 3;
        }

        private void ClearWaves()
        {
            _simulation?.ClearSimulation();
        }

        Vector2 GetUV(Vector2 local)
        {
            var rect = _waveImage.contentRect;
            return GetUVFromMouse(local, rect);
        }

        private Vector2 GetUVFromMouse(Vector2 localPosition, Rect contentRect)
        {
            var imageRect = GetImageRect(contentRect);

            var uv = new Vector2(
                (localPosition.x - imageRect.x) / imageRect.width,
                1f - (localPosition.y - imageRect.y) / imageRect.height
            );

            return uv;
        }

        private Rect GetImageRect(Rect contentRect)
        {
            int textureWidth = 512;
            int textureHeight = 512;

            if (_simulation != null)
            {
                var displayTex = _simulation.GetDisplayTexture();
                if (displayTex != null)
                {
                    textureWidth = displayTex.width;
                    textureHeight = displayTex.height;
                }
            }

            float contentAspect = contentRect.width / contentRect.height;
            float textureAspect = (float)textureWidth / textureHeight;

            Rect imageRect = contentRect;

            if (contentAspect > textureAspect)
            {
                float scaledWidth = contentRect.height * textureAspect;
                imageRect.x = contentRect.x + (contentRect.width - scaledWidth) * 0.5f;
                imageRect.width = scaledWidth;
            }
            else
            {
                float scaledHeight = contentRect.width / textureAspect;
                imageRect.y = contentRect.y + (contentRect.height - scaledHeight) * 0.5f;
                imageRect.height = scaledHeight;
            }

            return imageRect;
        }
    }
}