#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphPresentation
{
    public interface INodeVEBuilder
    {
        VisualElement CreateNodeVE(Node node, Action<Port, VisualElement> createPortVECallback);
    }

    public static class GraphVEBuilder
    {
        public static VisualElement BuildGraphVE(Graph graph, INodeVEBuilder nodeVEBuilder)
        {
            nodeVEBuilder ??= new DefaultNodeVEBuilder();
            var root = new VisualElement() { style = { position = Position.Absolute, width = Length.Auto(), height = Length.Auto(), backgroundColor = Color.cyan } };

            var connectVEDic = new Dictionary<string, VisualElement>();
            var connectionVERepaintActions = new List<Action>();

            for (int i = 0; i < graph.nodes.Length; i++)
            {
                var node = graph.nodes[i];

                var nodeVE = CreateNodeVE(node, nodeVEBuilder,
                    createPortVECallback: (port, portVE) =>
                    {
                        connectVEDic.Add(port.id, portVE);
                    },
                    dragCallback: () =>
                    {
                        foreach (var ra in connectionVERepaintActions)
                        {
                            ra.Invoke();
                        }
                    });

                connectVEDic.Add(node.id, nodeVE);
                root.Add(nodeVE);
            }

            foreach (var connection in graph.connections)
            {
                var connectionVE = CreateConnectionVE(connection, connectVEResolver: id => connectVEDic[id], out var repaintAction);

                root.Add(connectionVE);
                connectionVERepaintActions.Add(repaintAction);
            }
            return root;
        }

        private static VisualElement CreateNodeVE(
            Node node,
            INodeVEBuilder nodeVEBuilder,
            Action<Port, VisualElement> createPortVECallback,
            Action dragCallback)
        {
            var nodeVE = nodeVEBuilder.CreateNodeVE(node,
                createPortVECallback: createPortVECallback);

            nodeVE.RegisterCallbackOnce<AttachToPanelEvent>(evt =>
            {
                GraphVESupport.SetupDraggable(nodeVE, () =>
                {
                    dragCallback();
                    node.position = new Vector2(nodeVE.resolvedStyle.left, nodeVE.resolvedStyle.top);
                }, true);
            });

            return nodeVE;
        }

        private static VisualElement CreateConnectionVE(
            Connection connection,
            Func<string, VisualElement> connectVEResolver,
            out Action repaintAction)
        {
            var root = new VisualElement()
            {
                style = {
                    // backgroundColor = Color.green,
                    position = Position.Absolute,
                    width=100,
                    height=100,
                },
                pickingMode = PickingMode.Ignore,
            };
            var fromVE = connectVEResolver.Invoke(connection.from);
            var toVE = connectVEResolver.Invoke(connection.to);

            root.generateVisualContent += (context) =>
            {
                var from = GetDotPosition(fromVE);
                var to = GetDotPosition(toVE);

                from = GetRectEdgeIntersection(fromVE.worldBound, to);
                to = GetRectEdgeIntersection(toVE.worldBound, from);

                var localPos1 = root.WorldToLocal(from);
                var localPos2 = root.WorldToLocal(to);


                var color = new Color(.05f, .05f, .05f, 1f);

                Painter2DUtility.DrawPath(context.painter2D, new[] { localPos1, localPos2 }, color, 10f, 1f);
                Painter2DUtility.DrawCap(context, localPos1, localPos2, new Vector2(8, 6), color);
            };

            repaintAction = () =>
            {
                var from = GetDotPosition(fromVE);
                var to = GetDotPosition(toVE);
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

            static Vector2 GetDotPosition(VisualElement ve)
            {
                var x = ve.resolvedStyle.left;
                var y = ve.resolvedStyle.top;
                var width = ve.resolvedStyle.width;
                var height = ve.resolvedStyle.height;
                return ve.parent.LocalToWorld(new Vector2(x + width / 2f, y + height / 2f));
            }
        }

        public static Vector2 GetRectEdgeIntersection(Rect rect, Vector2 externalPoint)
        {
            var center = rect.center;
            var dir = center - externalPoint;

            // Handle degenerate case
            if (dir.sqrMagnitude < Mathf.Epsilon)
                return center;

            // Normalize direction
            dir.Normalize();

            // Compute t for intersection against 4 rectangle borders
            float tMin = float.MaxValue;

            // Avoid divide-by-zero
            const float EPS = 1e-6f;

            // Left edge (x = rect.xMin)
            if (Mathf.Abs(dir.x) > EPS)
            {
                float t = (rect.xMin - externalPoint.x) / dir.x;
                if (t > 0)
                {
                    float y = externalPoint.y + t * dir.y;
                    if (y >= rect.yMin && y <= rect.yMax)
                        tMin = Mathf.Min(tMin, t);
                }
            }

            // Right edge (x = rect.xMax)
            if (Mathf.Abs(dir.x) > EPS)
            {
                float t = (rect.xMax - externalPoint.x) / dir.x;
                if (t > 0)
                {
                    float y = externalPoint.y + t * dir.y;
                    if (y >= rect.yMin && y <= rect.yMax)
                        tMin = Mathf.Min(tMin, t);
                }
            }

            // Bottom edge (y = rect.yMin)
            if (Mathf.Abs(dir.y) > EPS)
            {
                float t = (rect.yMin - externalPoint.y) / dir.y;
                if (t > 0)
                {
                    float x = externalPoint.x + t * dir.x;
                    if (x >= rect.xMin && x <= rect.xMax)
                        tMin = Mathf.Min(tMin, t);
                }
            }

            // Top edge (y = rect.yMax)
            if (Mathf.Abs(dir.y) > EPS)
            {
                float t = (rect.yMax - externalPoint.y) / dir.y;
                if (t > 0)
                {
                    float x = externalPoint.x + t * dir.x;
                    if (x >= rect.xMin && x <= rect.xMax)
                        tMin = Mathf.Min(tMin, t);
                }
            }

            // Intersection point
            return externalPoint + dir * tMin;
        }
    }

    public class DefaultNodeVEBuilder : INodeVEBuilder
    {
        VisualElement INodeVEBuilder.CreateNodeVE(
            Node node,
            Action<Port, VisualElement> createPortVECallback)
        {
            return CreateNodeVE(
                node,
                createPortVECallback);
        }

        private static VisualElement CreateNodeVE(
            Node node,
            Action<Port, VisualElement> createPortVECallback)
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
            var layout_PortSpace = new VisualElement() { style = { width = 1, backgroundColor = Color.black, marginLeft = 10, marginRight = 10 } };
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