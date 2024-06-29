using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements.Utilities
{
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
                new Vertex() { position = p1 ,tint = color },
                new Vertex() { position = p2,tint = color },
                new Vertex() { position =p3,tint = color },
            });
            capMesh.SetAllIndices(new ushort[] { 0, 1, 2 });
        }
    }
}