#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphVisualizer
{
    public static class GraphVEBuilder
    {

        public static VisualElement BuildGraphVE(Graph graph)
        {
            var root = new VisualElement() { style = { position = Position.Absolute, width = Length.Auto(), height = Length.Auto(), backgroundColor = Color.cyan } };

            var portVEDic = new Dictionary<string, VisualElement>();
            var connectionVERepaintActions = new List<Action>();

            for (int i = 0; i < graph.nodes.Length; i++)
            {
                var node = graph.nodes[i];

                var nodeVE = CreateNodeVE(node,
                    createPortVECallback: (port, portVE) =>
                    {
                        portVEDic.Add(port.id, portVE);
                    },
                    dragCallback: () =>
                    {
                        foreach (var ra in connectionVERepaintActions)
                        {
                            ra.Invoke();
                        }
                    });

                root.Add(nodeVE);
            }

            foreach (var connection in graph.connections)
            {
                var connectionVE = CreateConnectionVE(connection, portVEResolver: ResolvePortVE, out var repaintAction);

                root.Add(connectionVE);
                connectionVERepaintActions.Add(repaintAction);
            }
            return root;

            VisualElement ResolvePortVE(string port)
            {
                return portVEDic[port];
            }
        }

        private static VisualElement CreateNodeVE(
            Node node,
            Action<Port, VisualElement> createPortVECallback,
            Action dragCallback)
        {
            var root = new VisualElement()
            {
                style = {
                    position = Position.Absolute,
                    left = node.position.x,
                    top = node.position.y,
                    width = Length.Auto(),
                    height = Length.Auto(),
                    backgroundColor = Color.gray,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                }
            };
            var layout_PortSpace = new VisualElement() { style = { width = 1, backgroundColor = Color.blue, marginLeft = 10, marginRight = 10 } };
            var layout_Ports = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = Length.Auto(), height = Length.Auto() } };
            var layout_Inputs = new VisualElement() { style = { width = Length.Auto(), height = Length.Auto() } };
            var layout_Outputs = new VisualElement() { style = { width = Length.Auto(), height = Length.Auto(), alignItems = Align.FlexEnd } };
            var label = new Label() { text = node.name, style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter, color = Color.black, unityFontStyleAndWeight = FontStyle.Bold } };

            layout_Ports.Add(layout_Inputs);
            layout_Ports.Add(layout_PortSpace);
            layout_Ports.Add(layout_Outputs);
            root.Add(label);
            root.Add(layout_Ports);

            foreach (var input in node.inputs)
            {
                var portVE_Input = CreatePortVE(input, Color.red, out var dot);
                layout_Inputs.Add(portVE_Input);

                createPortVECallback.Invoke(input, dot);
            }

            foreach (var output in node.outputs)
            {
                var portVE_Output = CreatePortVE(output, Color.green, out var dot);
                portVE_Output.style.flexDirection = FlexDirection.RowReverse;
                layout_Outputs.Add(portVE_Output);

                createPortVECallback.Invoke(output, dot);
            }

            root.RegisterCallbackOnce<AttachToPanelEvent>(evt =>
            {
                GraphVESupport.SetupDraggable(root, () =>
                {
                    dragCallback();
                    node.position = new Vector2(root.style.left.value.value, root.style.top.value.value);
                }, true);
            });
            return root;
        }

        private static VisualElement CreatePortVE(Port port, Color color, out VisualElement dot)
        {
            var root = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 2,
                    marginBottom = 2,
                    alignItems = Align.Center,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                }
            };
            dot = new VisualElement() { style = { width = 10, height = 10, backgroundColor = color } }; //VisualElementSpaceCreator.CreateWorldRect(port.name, Vector2.zero, Vector2.one * 10, Color.red);
            var label = new Label() { text = port.name, style = { flexGrow = 1 } };
            root.Add(dot);
            root.Add(label);
            return root;
        }

        private static VisualElement CreateConnectionVE(
            Connection connection,
            Func<string, VisualElement> portVEResolver,
            out Action repaintAction)
        {
            var root = new VisualElement()
            {
                style = {
                    // backgroundColor = Color.green,
                    position = Position.Absolute,
                    width=100,
                    height=100,
                }
            };
            var portVE_From = portVEResolver.Invoke(connection.from);
            var portVE_To = portVEResolver.Invoke(connection.to);

            root.generateVisualContent += (context) =>
            {
                var from = GetDotPosition(portVE_From);
                var to = GetDotPosition(portVE_To);

                var dotRadius = portVE_From.style.width.value.value;

                var localPos1 = root.WorldToLocal(from);
                var localPos2 = root.WorldToLocal(to);

                var color = new Color(.05f, .05f, .05f, 1f);

                Painter2DUtility.DrawPath(context.painter2D, new[] { localPos1, localPos2 }, color, 10f, 1f);
                Painter2DUtility.DrawCap(context, localPos1, localPos2, new Vector2(8, 6), color);
            };

            repaintAction = () =>
            {
                var from = GetDotPosition(portVE_From);
                var to = GetDotPosition(portVE_To);
                SetEdgePoints(from, to);

                root.MarkDirtyRepaint();

            };

            return root;

            void SetEdgePoints(Vector2 pos1, Vector2 pos2)
            {
                var localPos1 = root.parent.WorldToLocal(pos1);
                var localPos2 = root.parent.WorldToLocal(pos2);

                var xMin = Mathf.Min(localPos1.x, localPos2.x);
                var yMin = Mathf.Min(localPos1.y, localPos2.y);
                var xMax = Mathf.Max(localPos1.x, localPos2.x);
                var yMax = Mathf.Max(localPos1.y, localPos2.y);

                root.style.left = xMin;
                root.style.top = yMin;
                root.style.width = xMax - xMin;
                root.style.height = yMax - yMin;
            }

            static Vector2 GetDotPosition(VisualElement port)
            {
                var x = port.style.left.value.value;
                var y = port.style.top.value.value;
                var width = port.style.width.value.value;
                var height = port.style.width.value.value;
                return port.LocalToWorld(new Vector2(x + width / 2f, y + height / 2f));
            }
        }

    }

    public static class Painter2DUtility
    {
        public static void DrawLine(Painter2D painter, Vector2 p1, Vector2 p2, Color strokeColor, float strokeSize = 2f)
        {
            var prevColor = painter.strokeColor;
            var prevLineWidth = painter.lineWidth;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeSize;

            painter.BeginPath();
            painter.MoveTo(p1);
            painter.LineTo(p2);
            painter.Stroke();

            painter.strokeColor = prevColor;
            painter.lineWidth = prevLineWidth;
        }
        public static void DrawPath(Painter2D painter, Vector2[] path, Color strokeColor, float cornerRadius, float strokeSize = 2f)
        {
            if (path.Length < 2) return;
            if (path.Length == 2)
            {
                DrawLine(painter, path[0], path[1], strokeColor, strokeSize);
                return;
            }
            var prevColor = painter.strokeColor;
            var prevLineWidth = painter.lineWidth;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeSize;

            painter.BeginPath();
            painter.MoveTo(path[0]);
            for (var i = 0; i < path.Length - 2; i++)
            {
                painter.ArcTo(path[i + 1], path[i + 2], cornerRadius);
            }
            painter.LineTo(path[^1]);
            painter.Stroke();

            painter.strokeColor = prevColor;
            painter.lineWidth = prevLineWidth;
        }
        public static void DrawCrossSign(Painter2D painter, Vector2 pos, float size, Color strokeColor, float strokeSize = 2f)
        {
            var topLeft = pos + Vector2.up * size + Vector2.left * size;
            var bottomRight = pos + Vector2.down * size + Vector2.right * size;
            var topRight = pos + Vector2.up * size + Vector2.right * size;
            var bottomLeft = pos + Vector2.down * size + Vector2.left * size;

            var prevColor = painter.strokeColor;
            var prevLineWidth = painter.lineWidth;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeSize;

            painter.BeginPath();
            painter.MoveTo(topLeft);
            painter.LineTo(bottomRight);
            painter.MoveTo(topRight);
            painter.LineTo(bottomLeft);
            painter.Stroke();

            painter.strokeColor = prevColor;
            painter.lineWidth = prevLineWidth;

        }
        public static void DrawRect(Painter2D painter, Rect rect, Color strokeColor, float strokeSize = 2f)
        {
            var topLeft = new Vector2(rect.xMin, rect.yMin);
            var bottomLeft = new Vector2(rect.xMin, rect.yMax);
            var bottomRight = new Vector2(rect.xMax, rect.yMax);
            var topRight = new Vector2(rect.xMax, rect.yMin);

            var prevColor = painter.strokeColor;
            var prevLineWidth = painter.lineWidth;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeSize;

            painter.BeginPath();
            painter.MoveTo(topLeft);
            painter.LineTo(bottomLeft);
            painter.LineTo(bottomRight);
            painter.LineTo(topRight);
            painter.LineTo(topLeft);
            painter.Stroke();

            painter.strokeColor = prevColor;
            painter.lineWidth = prevLineWidth;
        }
        public static void FillRect(Painter2D painter, Rect rect, Color color)
        {
            var topLeft = new Vector2(rect.xMin, rect.yMin);
            var bottomLeft = new Vector2(rect.xMin, rect.yMax);
            var bottomRight = new Vector2(rect.xMax, rect.yMax);
            var topRight = new Vector2(rect.xMax, rect.yMin);

            var prevColor = painter.fillColor;
            painter.fillColor = color;

            painter.BeginPath();
            painter.MoveTo(topLeft);
            painter.LineTo(bottomLeft);
            painter.LineTo(bottomRight);
            painter.LineTo(topRight);
            painter.LineTo(topLeft);
            painter.Fill();

            painter.fillColor = prevColor;
        }

        public static void DrawRoundedCornerRect(Painter2D painter, Rect rect, Color strokeColor, float strokeSize = 2f, float cornerRadius = 2.5f)
        {
            var middleLeft = new Vector2(rect.xMin, rect.yMin + rect.height / 2f);
            var topLeft = new Vector2(rect.xMin, rect.yMin);
            var bottomLeft = new Vector2(rect.xMin, rect.yMax);
            var bottomRight = new Vector2(rect.xMax, rect.yMax);
            var topRight = new Vector2(rect.xMax, rect.yMin);

            var prevColor = painter.strokeColor;
            var prevLineWidth = painter.lineWidth;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeSize;

            painter.BeginPath();
            painter.MoveTo(middleLeft);
            painter.ArcTo(bottomLeft, bottomRight, cornerRadius);
            painter.ArcTo(bottomRight, topRight, cornerRadius);
            painter.ArcTo(topRight, topLeft, cornerRadius);
            painter.ArcTo(topLeft, middleLeft, cornerRadius);
            painter.LineTo(middleLeft);
            painter.Stroke();

            painter.strokeColor = prevColor;
            painter.lineWidth = prevLineWidth;
        }

        public static void FillAndStrokeRoundedCornerRect(Painter2D painter, Rect rect, Color fillColor, Color strokeColor, float strokeSize = 2f, float cornerRadius = 2.5f)
        {
            var middleLeft = new Vector2(rect.xMin, rect.yMin + rect.height / 2f);
            var topLeft = new Vector2(rect.xMin, rect.yMin);
            var bottomLeft = new Vector2(rect.xMin, rect.yMax);
            var bottomRight = new Vector2(rect.xMax, rect.yMax);
            var topRight = new Vector2(rect.xMax, rect.yMin);

            painter.BeginPath();
            painter.MoveTo(middleLeft);
            painter.ArcTo(bottomLeft, bottomRight, cornerRadius);
            painter.ArcTo(bottomRight, topRight, cornerRadius);
            painter.ArcTo(topRight, topLeft, cornerRadius);
            painter.ArcTo(topLeft, middleLeft, cornerRadius);
            painter.LineTo(middleLeft);

            var prevFillColor = painter.strokeColor;
            painter.fillColor = fillColor;
            painter.Fill();
            painter.fillColor = prevFillColor;

            var prevColor = painter.strokeColor;
            var prevLineWidth = painter.lineWidth;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeSize;
            painter.Stroke();
            painter.strokeColor = prevColor;
            painter.lineWidth = prevLineWidth;
        }

        public static void FillTriangle(MeshGenerationContext context, Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            var capMesh = context.Allocate(3, 3);
            capMesh.SetAllVertices(new[] {
                new Vertex() { position = p1, tint = color },
                new Vertex() { position = p2, tint = color },
                new Vertex() { position = p3, tint = color },
            });
            capMesh.SetAllIndices(new ushort[] { 0, 1, 2 });
        }

        public static void DrawCap(MeshGenerationContext context, Vector2 p1, Vector2 p2, Vector2 size, Color color)
        {
            var capDir = (p2 - p1).normalized;
            var capNor = Vector2.Perpendicular(capDir);
            FillTriangle(
                context,
                p2,
                p2 - capDir * size.y + capNor * size.x / 2f,
                p2 - capDir * size.y - capNor * size.x / 2f,
                color);
        }
    }
}
#endif