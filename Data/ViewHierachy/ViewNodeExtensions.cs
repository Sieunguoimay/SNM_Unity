using System.Collections.Generic;

namespace Supports.ViewHierachy
{
    public static class ViewNodeExtensions
    {
        public static IEnumerable<ViewNode> Iterate(this ViewNode node)
        {
            yield return node;
            foreach (var n in node.Children)
            {
                foreach (var n2 in Iterate(n))
                {
                    yield return n2;
                }
            }
        }
    }
}

