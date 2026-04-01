#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class SecondHeaderVECreator
    {
        private static readonly StyleLength ZeroMargin = new(0f);
        private static readonly StyleLength SmallPadding = new(3f);

        public static VisualElement Create(SerializedObject serializedObject, VisualElement imguiContainer)
        {
            if (serializedObject.targetObjects.Length > 1) return new VisualElement();

            var target = serializedObject.targetObject;
            var assetPath = AssetDatabase.GetAssetPath(target);
            var isAsset = !string.IsNullOrEmpty(assetPath);
            var isScript = target is MonoBehaviour || target is ScriptableObject;

            var root = new VisualElement();
            var layout_Refs = new VisualElement();

            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    marginBottom = 4,
                    backgroundColor = new Color(0f, 0f, 0f, .2f),
                    height = EditorGUIUtility.singleLineHeight + 2,
                    alignItems = Align.Center,
                }
            };

            // Left group: view mode
            bar.Add(CompactButton("Fields", CustomEditorVECreator.BuildVE(serializedObject, imguiContainer, layout_Refs)));

            bar.Add(Separator());

            // Center group: navigation
            bar.Add(CompactButton("Browse", new Button(() => SecondHeaderTools.OpenObjectBrowser(target))));
            bar.Add(CompactButton("Window", new Button(() => EditorPopupWindow.Open(target)) { tooltip = "Open in new Window" }));
            bar.Add(CompactButton("Ping", new Button(() => EditorGUIUtility.PingObject(target))));
            if (isScript)
                bar.Add(CompactButton("Script", new Button(() => SecondHeaderTools.OpenScript(target)) { tooltip = "Edit Script" }));

            bar.Add(Separator());

            // Right group: search & copy
            bar.Add(CreateFindReferences(target));
            if (isAsset)
                bar.Add(CreateCopyPathButton(assetPath));

            root.Add(bar);
            root.Add(layout_Refs);

            return root;
        }

        private static VisualElement CompactButton(string label, VisualElement element)
        {
            if (element is Button btn && string.IsNullOrEmpty(btn.text))
                btn.text = label;

            element.style.marginLeft = ZeroMargin;
            element.style.marginRight = ZeroMargin;
            element.style.paddingLeft = SmallPadding;
            element.style.paddingRight = SmallPadding;

            return element;
        }

        private static VisualElement Separator()
        {
            return new VisualElement
            {
                style =
                {
                    width = 1,
                    height = new Length(70, LengthUnit.Percent),
                    backgroundColor = new Color(1f, 1f, 1f, 0.1f),
                    marginLeft = 2,
                    marginRight = 2,
                }
            };
        }

        private static VisualElement CreateCopyPathButton(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            return CompactButton("Copy", new Button()
            {
                clickable = new(() =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Asset Path"), false, () =>
                    {
                        GUIUtility.systemCopyBuffer = assetPath;
                        Debug.Log($"Copied path: {assetPath}");
                    });
                    menu.AddItem(new GUIContent("GUID"), false, () =>
                    {
                        GUIUtility.systemCopyBuffer = guid;
                        Debug.Log($"Copied GUID: {guid}");
                    });
                    menu.ShowAsContext();
                }),
            });
        }

        private static VisualElement CreateFindReferences(Object target)
        {
            var button = new Button()
            {
                text = "Find",
                clickable = new(() =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Find Scene References"), false, () => SecondHeaderTools.OpenFindReferencesInScene(target));
                    menu.AddItem(new GUIContent("Find Asset References"), false, () => SecondHeaderTools.FindRefrencesInProject(target));
                    menu.ShowAsContext();
                }),
#if UNITY_2023_2_OR_NEWER
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image)
#endif
            };
#if UNITY_2023_2_OR_NEWER
            var image = button.Q<Image>();
            image.style.height = 17;
            image.style.width = 17;
#endif
            button.style.paddingTop = ZeroMargin;
            button.style.paddingBottom = ZeroMargin;
            return CompactButton("Find", button);
        }
    }
}
#endif
