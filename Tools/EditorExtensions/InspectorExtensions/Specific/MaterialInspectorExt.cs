#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class MaterialInspectorExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        Type IInspectorExtension.TargetType => typeof(Material);

        void IInspectorExtension.CleanUp()
        {
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            extensionElement.Add(CreateMaterialInspectorExtension(extensionElement));
        }

        private VisualElement CreateMaterialInspectorExtension(InspectorExtensionElement ext)
        {
            var ve = new VisualElement();
            if (ext.Target is Material material)
            {
                var foldout = new Foldout { text = "Material keywords", value = false };
                foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
                ve.Add(foldout);
                ve.Add(new ThreeDots());

                var shader = material.shader;
                foreach (var k in IterateKeyWords(shader, material))
                {
                    var toggle = new Toggle(k.Item1.name) { value = k.Item2 };
                    toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
                    {
                        material.SetKeyword(k.Item1, evt.newValue);
                    });
                    foldout.Add(toggle);

                }

                // for (var i = 0; i < shader.GetPropertyCount(); i++)
                // {
                //     var propType = shader.GetPropertyType(i);
                //     var propName = shader.GetPropertyName(i);
                //     var isGlobal = false;

                //     foreach (var a in shader.GetPropertyAttributes(i))
                //     {
                //         if (a == GlobalProperty)
                //         {
                //             isGlobal = true;
                //             break;
                //         }
                //     }
                //     if (isGlobal)
                //     {
                //         if (propType == UnityEngine.Rendering.ShaderPropertyType.Float)
                //         {
                //             foldout.Add(new GlobalFloatField(propName));
                //         }
                //         if (propType == UnityEngine.Rendering.ShaderPropertyType.Range)
                //         {
                //             foldout.Add(new GlobalRangeField(propName, shader.GetPropertyRangeLimits(i)));
                //         }
                //         if (propType == UnityEngine.Rendering.ShaderPropertyType.Color)
                //         {
                //             foldout.Add(new GlobalColorField(propName));
                //         }
                //     }
                // }
            }
            // ve.style.marginLeft = 30;
            return ve;
        }

        private IEnumerable<(UnityEngine.Rendering.LocalKeyword, bool)> IterateKeyWords(Shader shader, Material material)
        {
            var keywordSpace = shader.keywordSpace;

            foreach (var localKeyword in keywordSpace.keywords)
            {
                // If the local keyword is overridable (i.e., it was declared with a global scope),
                // and a global keyword with the same name exists and is enabled,
                // then Unity uses the global keyword state
                if (localKeyword.isOverridable && Shader.IsKeywordEnabled(localKeyword.name))
                {
                    yield return (localKeyword, true);
                }
                // Otherwise, Unity uses the local keyword state
                else
                {
                    yield return (localKeyword, material.IsKeywordEnabled(localKeyword));
                }
            }
        }

        private class ThreeDots : Button
        {
            public ThreeDots()
            {
                text = "...";
                style.position = Position.Absolute;
                style.width = 20;
                style.height = 18;
                style.alignSelf = Align.FlexEnd;
                RegisterCallback<ClickEvent>(OnClicked);
            }

            private void OnClicked(ClickEvent evt)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Ping Script"), false, () =>
                {
                    EditorGUIUtility.PingObject(AssetDatabase.FindAssets($"t:MonoScript {nameof(MaterialInspectorExt)}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>).FirstOrDefault());
                });
                menu.ShowAsContext();
            }
        }

        private class GlobalFloatField : FloatField
        {
            public string ShaderPropertyName { get; private set; }
            public GlobalFloatField(string shaderPropertyName)
            {
                ShaderPropertyName = shaderPropertyName;
                value = Shader.GetGlobalFloat(ShaderPropertyName[1..]);
                label = shaderPropertyName;
                RegisterCallback<ChangeEvent<float>>(ValueChanged);

                style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            private void ValueChanged(ChangeEvent<float> evt)
            {
                Shader.SetGlobalFloat(ShaderPropertyName[1..], evt.newValue);
            }
        }
        private class GlobalRangeField : Slider
        {
            public string ShaderPropertyName { get; private set; }
            public GlobalRangeField(string shaderPropertyName, Vector2 range)
            {
                ShaderPropertyName = shaderPropertyName;
                value = Shader.GetGlobalFloat(ShaderPropertyName[1..]);
                label = shaderPropertyName;
                lowValue = range.x;
                highValue = range.y;
                showInputField = true;
                RegisterCallback<ChangeEvent<float>>(ValueChanged);

                style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            private void ValueChanged(ChangeEvent<float> evt)
            {
                Shader.SetGlobalFloat(ShaderPropertyName[1..], evt.newValue);
            }
        }

        private class GlobalColorField : ColorField
        {
            public string ShaderPropertyName { get; private set; }
            public GlobalColorField(string shaderPropertyName)
            {
                ShaderPropertyName = shaderPropertyName;
                value = Shader.GetGlobalColor(ShaderPropertyName[1..]);
                label = shaderPropertyName;
                RegisterCallback<ChangeEvent<Color>>(ValueChanged);

                style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            private void ValueChanged(ChangeEvent<Color> evt)
            {
                Shader.SetGlobalColor(ShaderPropertyName[1..], evt.newValue);
            }
        }
    }
}
#endif