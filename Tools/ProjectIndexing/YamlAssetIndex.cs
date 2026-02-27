#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace Snm.Tools
{
    [Serializable]
    public class YamlAssetIndex
    {
        public List<Entry> entries = new();

        [Serializable]
        public class Entry
        {
            public string guid;
            public string path;
            public string[] lines;
        }
    }
}

#endif