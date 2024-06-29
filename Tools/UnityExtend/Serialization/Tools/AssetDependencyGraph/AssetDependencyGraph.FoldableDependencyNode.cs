#if UNITY_EDITOR
using Common.UnityExtend.UIElements.GraphView;
using Common.UnityExtend.UIElements.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class AssetDependencyGraph
{
    private class FoldableDependencyNode : DependencyNode
    {
        private FoldoutButton _foldOutButton;
        private bool _foldout = false;
        private readonly List<DependencyNode> _subNodes = new();
        private readonly List<DependencyEdge> _toggledEdges = new();
        private readonly GraphView _graph;
        public IEnumerable<DependencyNode> SubNodes => _subNodes;

        public FoldableDependencyNode(GraphView graph, string path) : base(path)
        {
            _graph = graph;
            OnMove += OnNodeMove;
        }

        private void OnNodeMove(NodeView view)
        {
            for (int i = 0; i < _subNodes.Count; i++)
            {
                var sn = _subNodes[i];

                var x = style.left.value.value + 10;
                var y = style.top.value.value + (i + 1) * 40;

                sn.style.left = x;
                sn.style.top = y;
                sn.InvokeMoveEvent();
            }
        }

        public void Init()
        {
            LoadChildren();
            if (_subNodes.Count > 0)
            {
                _foldOutButton = new FoldoutButton();
                _foldOutButton.style.position = Position.Absolute;
                _foldOutButton.style.width = 10;
                _foldOutButton.style.height = 10;
                _foldOutButton.style.left = -16;
                _foldOutButton.Clicked += OnFoldoutButtonClicked;
                Add(_foldOutButton);
            }
            for (int i = 0; i < _subNodes.Count; i++)
            {
                var sn = _subNodes[i];

                var x = style.left.value.value + 10;
                var y = style.top.value.value + (i + 1) * 40;

                sn.DefaultPosition = new Vector2(x, y);
            }

        }
        private void OnFoldoutButtonClicked(FoldoutButton evt)
        {
            _foldout = !_foldout;
            _foldOutButton.SetRotateDown(_foldout);
            UpdateFoldoutChildren();
        }
        private void UpdateFoldoutChildren()
        {
            if (_foldout)
            {
                //style.top = DefaultPosition.y - _subNodes.Count * 40f / 2f;
                for (int i = 0; i < _subNodes.Count; i++)
                {
                    var sn = _subNodes[i];

                    _graph.AddNode(sn);

                    var x = style.left.value.value + 10;
                    var y = style.top.value.value + (i + 1) * 40;

                    sn.style.left = x;
                    sn.style.top = y;

                }

                foreach (var e in _toggledEdges)
                {
                    _graph.AddEdge(e);
                }

                foreach (var e in ConnectedEdges)
                {
                    if (_toggledEdges.Contains(e)) continue;
                    _graph.RemoveEdge(e);
                }
            }
            else
            {
                //style.top = DefaultPosition.y;
                foreach (var sn in _subNodes)
                {
                    _graph.RemoveNode(sn);
                }
                foreach (var e in _toggledEdges)
                {
                    _graph.RemoveEdge(e);
                }

                foreach (var e in ConnectedEdges)
                {
                    if (_toggledEdges.Contains(e)) continue;
                    _graph.AddEdge(e);
                }
            }
        }
        private void LoadChildren()
        {
            var objectTexts = LoadAllObjectTexts(Path);
            var subDependenciesDict = objectTexts
                .ToDictionary(ExtractLocalID, t => DependencyGraphCreator.ParseReferences2(t));
            var objectTextDict = objectTexts
                .ToDictionary(ExtractLocalID, t => t);

            //var localIdArray = subDependenciesDict.Keys.ToArray();

            var subObjectDict = GetSubObjects(objectTextDict);

            if (subObjectDict.Count == 0) return;

            var dependencyNodes = QueryConnectedNodes(true)
                .Select(n => n as DependencyNode).ToDictionary(n => n.Path, n => n);

            var showSubObjectType = Target is GameObject;

            foreach (var subObject in subObjectDict)
            {
                var connectNodes = new List<DependencyNode>();
                if (subDependenciesDict.TryGetValue(subObject.Key, out var ds))
                {
                    foreach (var d in ds)
                    {
                        if (dependencyNodes.TryGetValue(AssetDatabase.GUIDToAssetPath(d), out var node))
                        {
                            connectNodes.Add(node);
                        }
                    }
                }
                if (connectNodes.Count > 0)
                {
                    if (subObject.Value != Target)
                    {
                        if (string.IsNullOrEmpty(subObject.Value.name))
                        {
                            Debug.Log(subObject.Value);
                        }
                        var displayName = showSubObjectType ? $"{subObject.Value.name} ({subObject.Value.GetType().Name})" : subObject.Value.name;
                        var subNode = new DependencyNode(subObject.Value, displayName);
                        _subNodes.Add(subNode);

                        foreach (var n in connectNodes)
                        {
                            var dependencyEdge = new DependencyEdge();
                            dependencyEdge.Connect(subNode, n);
                            _toggledEdges.Add(dependencyEdge);
                        }
                    }
                    else
                    {
                        foreach (var n in connectNodes)
                        {
                            var dependencyEdge = new DependencyEdge();
                            dependencyEdge.Connect(this, n);
                            _toggledEdges.Add(dependencyEdge);
                        }
                    }
                }
            }
        }
        public class TempPrefabInstance : ScriptableObject
        {
            // You can add properties and fields to this class as needed.
        }
        private Dictionary<long, Object> GetSubObjects(Dictionary<long, string> objectTextDict)
        {
            bool isSceneAsset = Path.EndsWith(".unity");
            if (isSceneAsset)
            {
                return new Dictionary<long, Object>();
            }
            var localIds = objectTextDict.Keys.ToArray();
            if (AssetDatabase.LoadMainAssetAtPath(Path) is GameObject go)
            {
                var prefabInstanceType = System.Type.GetType("UnityEngine.PrefabInstance");
                return AssetDatabase.LoadAllAssetsAtPath(Path)
                    .Select(a =>
                    {
                        if (a is not Component && a is not GameObject)
                        {
                            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var _, out long localid))
                            {
                                if (objectTextDict.TryGetValue(localid, out var objectText))
                                {
                                    var found = DependencyGraphCreator.ParseReferences3(objectText).FirstOrDefault(p => p.Item1.Equals("m_SourcePrefab"));
                                    if (found.Item1 != null && found.Item2 != null)
                                    {
                                        var targetObject = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(found.Item2));

                                        var namePattern = @"propertyPath:\s*m_Name\s*\n\s*value:\s*(.+)";
                                        var match = Regex.Match(objectText, namePattern);

                                        if (match.Success)
                                        {
                                            string extractedValue = match.Groups[1].Value.Trim();
                                            targetObject.name = extractedValue;
                                        }
                                        return (localid, targetObject);
                                    }
                                }
                                var obj = ScriptableObject.CreateInstance<TempPrefabInstance>();
                                obj.name = "PrefabInstance";

                                return (localid, obj);
                            }
                            else
                            {
                                return (0, null);
                            }
                        }
                        else
                        {
                            return (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var _, out long localid) ? localid : 0, a);
                        }
                    })
                    .Where(a => localIds.Contains(a.Item1))
                    .OrderBy(d => System.Array.IndexOf(localIds, d.Item1))
                    .ToDictionary(so => so.Item1, so => so.Item2);
            }
            else
            {
                return AssetDatabase.LoadAllAssetsAtPath(Path)
                    .Select(a => (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var _, out long localid) ? localid : 0, a))
                    .Where(a => localIds.Contains(a.Item1))
                    .OrderBy(d => System.Array.IndexOf(localIds, d.Item1))
                    .ToDictionary(so => so.Item1, so => so.a);
            }
        }
        private static long ExtractLocalID(string yamlString)
        {
            var pattern = @"(?<=&)[+-]?\d+";
            var match = Regex.Match(yamlString, pattern);

            if (match.Success)
            {
                var localIDString = match.Groups[0].Value;
                var localID = long.Parse(localIDString);

                return localID;
            }

            return -1;
        }

        private IEnumerable<string> LoadAllObjectTexts(string path)
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, path["Assets/".Length..]);

            if (File.Exists(fullPath))
            {
                var assetText = File.ReadAllText(fullPath);
                return YamlParse(assetText);
            }
            return new string[0];
        }
        public static IEnumerable<string> YamlParse(string yamlText)
        {
            int startIndex = yamlText.IndexOf("--- !u!");

            if (startIndex != -1)
            {
                string separator = "--- !u!";
                string[] unityObjects = yamlText[startIndex..].Split(new string[] { separator }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (string unityObjectYAML in unityObjects)
                {
                    yield return unityObjectYAML;
                }
            }
            else
            {
                Debug.LogError("Invalid YAML format: Could not find Unity objects.");
            }
        }


        private bool HasReferenceToOutside(Object obj)
        {
            return GetAllReferences(obj).Any(o => o != Target && AssetDatabase.IsMainAsset(o));
        }
        private IEnumerable<Object> GetAllReferences(Object obj)
        {
            var it = new SerializedObject(obj).GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                {
                    yield return it.objectReferenceValue;
                }
            }
        }

        public class FoldoutButton : VisualElement
        {
            private bool _down;
            public event System.Action<FoldoutButton> Clicked;
            public FoldoutButton()
            {
                generateVisualContent += OnRepaint;
                RegisterCallback<MouseDownEvent>(OnMouseDown);
                RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            private void OnMouseUp(MouseUpEvent evt)
            {
                if (evt.button == 0)
                {
                    if (_down)
                    {
                        _down = false;
                        Clicked?.Invoke(this);
                    }
                    evt.StopPropagation();
                }
            }

            private void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.pressedButtons == 1)
                {
                    _down = true;
                    evt.StopPropagation();
                }
            }

            public void SetRotateDown(bool rotateDown)
            {
                var angle = rotateDown ? (90f) : 0f;
                style.rotate = new StyleRotate(new Rotate(new Angle(angle, AngleUnit.Degree))); ;
            }

            private void OnRepaint(MeshGenerationContext context)
            {
                Painter2DUtility.FillTriangle(context,
                    new Vector2(0, 0),
                    new Vector2(contentRect.width, contentRect.height / 2f),
                    new Vector2(0, contentRect.height),
                    Color.gray);
            }
        }
    }
}
#endif