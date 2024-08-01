using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;

#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Identification
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SUIDSpaceAttribute : Attribute { }

#if UNITY_EDITOR
    public partial class SUIDsToolWindow : EditorWindow
    {
        [SerializeField] private MonoScript suidsSpaceScript;
        [SerializeField] private List<SUID> suids = new();
        [SerializeField] private int counter = 0;

        [MenuItem("Tools/Game/SUIDsTool")]
        public static void OpenWindow()
        {
            GetWindow<SUIDsToolWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new ToolVE(this));
        }

        private class ToolVE : VisualElement
        {
            private readonly SUIDsToolWindow window;
            private readonly VisualElement container;
            private readonly ObjectField script;

            public ToolVE(SUIDsToolWindow window)
            {
                this.window = window;
                window.LoadSUIDSpace();

                var bottom = new VisualElement();
                bottom.style.flexDirection = FlexDirection.Row;
                bottom.style.minHeight = UnityEditor.EditorGUIUtility.singleLineHeight;
                var top = new VisualElement();
                top.style.flexDirection = FlexDirection.Row;
                top.style.minHeight = UnityEditor.EditorGUIUtility.singleLineHeight;

                script = new UnityEditor.UIElements.ObjectField
                {
                    name = "Script",
                    objectType = typeof(MonoScript),
                    value = window.suidsSpaceScript
                };
                script.RegisterValueChangedCallback(OnScriptChanged);
                script.style.flexGrow = 1;

                var select = new Button() { text = "..." };
                select.RegisterCallback<ClickEvent>(evt => OnSelect());

                var refresh = new Button() { text = "Reload" };
                refresh.RegisterCallback<ClickEvent>(evt => OnReload());

                var addNew = new Button() { text = "Add New" };
                addNew.RegisterCallback<ClickEvent>(evt => OnAddNew());
                addNew.style.flexGrow = 1;

                var export = new Button() { text = "Export" };
                export.RegisterCallback<ClickEvent>(evt => OnExport());
                export.style.flexGrow = 1;

                Add(top);
                top.Add(script);
                top.Add(select);
                top.Add(refresh);
                Add(container = new ScrollView() { });
                Add(bottom);
                bottom.Add(addNew);
                bottom.Add(export);
                PopulateSUIDVEs();
            }

            private void OnSelect()
            {
                var gm = new GenericMenu();
                foreach (var t in GetTypesWithAttribute<SUIDSpaceAttribute>())
                {
                    var isCurrent = window.suidsSpaceScript != null && window.suidsSpaceScript.GetClass() == t;
                    gm.AddItem(new GUIContent(t.Name), isCurrent, () =>
                    {
                        script.value = AssetDatabase.FindAssets($"t:MonoScript {t.Name}")
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                            .Where(ms => ms.GetClass().GetAttribute<SUIDSpaceAttribute>() != null)
                            .FirstOrDefault();
                        window.LoadSUIDSpace();
                        Refresh();
                    });
                }
                gm.ShowAsContext();
            }
            private void OnExport()
            {
                window.Export();
            }

            private void OnAddNew()
            {
                window.AddNew();
                Refresh();
            }

            private void OnScriptChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                window.suidsSpaceScript = evt.newValue as MonoScript;
            }

            private void OnReload()
            {
                window.LoadSUIDSpace();
                Refresh();
            }

            private void Refresh()
            {
                container.Clear();
                PopulateSUIDVEs();
            }

            private void PopulateSUIDVEs()
            {
                container.Add(new Label($"Counter = {window.counter}"));
                for (int i = 0; i < window.suids.Count; i++)
                {
                    var suid = window.suids[i];
                    container.Add(new SUIDVE(suid, i, this));
                }
            }

            private class SUIDVE : VisualElement
            {
                public SUIDVE(SUID suid, int index, ToolVE tool)
                {
                    style.flexDirection = FlexDirection.Row;
                    style.alignItems = Align.Center;
                    var obj = suid.IdentifiedObject != null ? $" -> {suid.IdentifiedObject}" : "";
                    var label = new Label($"{index}. {suid.ID}{obj}");
                    label.style.flexGrow = 1;
                    var btn = new Button() { text = "X" };
                    btn.style.width = 20;
                    btn.RegisterCallback<ClickEvent>(evt =>
                    {
                        tool.window.suids.Remove(suid);
                        tool.Refresh();
                    });
                    Add(label);
                    Add(btn);
                }
            }

        }

        private void LoadSUIDSpace()
        {
            if (suidsSpaceScript == null) return;
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var t = suidsSpaceScript.GetClass();
            suids = t.GetProperties(flags)
                .Where(p => p.PropertyType == typeof(SUID))
                .Select(p => p.GetValue(null))
                .OfType<SUID>().ToList();
            var ct = t.GetField("Counter");
            counter = ct != null ? (int)ct.GetValue(null) : 0;
        }

        [ContextMenu("AddNew")]
        private void AddNew()
        {
            if (suidsSpaceScript == null) return;
            suids.Add(new SUID(GenerateGUID()));
            counter++;
        }

        [ContextMenu("Export")]
        private void Export()
        {
            if (suidsSpaceScript == null) return;

            var path = AssetDatabase.GetAssetPath(suidsSpaceScript);
            var absolutePath = path.Replace("Assets", Application.dataPath);
            var cs = string.Join("\n", CreateCSLines());

            File.WriteAllText(absolutePath, cs);

            AssetDatabase.Refresh();

            Debug.Log($"{absolutePath}");
        }

        private IEnumerable<string> CreateCSLines()
        {
            var tab = "    ";
            yield return $"namespace Identification";
            yield return $"{{";
            yield return $"{tab}[SUIDSpace]";
            yield return $"{tab}public class {suidsSpaceScript.name}";
            yield return $"{tab}{{";
            yield return $"{tab}{tab}public const int Counter = {counter}; //Counter is not Total SUIDs of this space";
            foreach (var metaObject in suids)
            {
                yield return $"{tab}{tab}public static SUID _{metaObject.ID} {{ get; }} = new(\"{metaObject.ID}\");";
            }
            yield return $"{tab}}}";
            yield return $"}}";
        }

        public string GenerateGUID()
        {
            var uniqueId = GenerateHash(counter);
            return uniqueId;
        }
        public static string GenerateHash(int input)
        {
            // Convert integer to byte array
            byte[] inputBytes = BitConverter.GetBytes(input);

            // Create SHA256 hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Convert hash bytes to base32 string
                string base32String = Base32Encode(hashBytes);

                // Return the first 8 characters of the base32 string
                return base32String.Substring(0, 8);
            }
        }

        private static string Base32Encode(byte[] bytes)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            StringBuilder sb = new StringBuilder();
            int value = 0, bits = 0;

            foreach (byte b in bytes)
            {
                value = (value << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    int index = (value >> (bits - 5)) & 0x1F;
                    sb.Append(base32Chars[index]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                int index = (value << (5 - bits)) & 0x1F;
                sb.Append(base32Chars[index]);
            }

            return sb.ToString();
        }


        public static Type[] GetTypesWithAttribute<T>() where T : Attribute
        {
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            var result = types.Where(type => type.GetCustomAttributes(typeof(T), false).Any()).ToArray();

            return result;
        }

    }
#endif
}