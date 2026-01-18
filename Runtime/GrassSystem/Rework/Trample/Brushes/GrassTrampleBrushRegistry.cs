using System.Collections.Generic;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleBrushRegistry
    {
        private readonly List<GrassTrampleBrush> brushes = new();

        public void Register(GrassTrampleBrush brush) { brushes.Add(brush); }
        public void Unregister(GrassTrampleBrush brush) { brushes.Remove(brush); }
        public IReadOnlyList<GrassTrampleBrush> GetBrushes() => brushes;
    }
}