using System.Collections.Generic;

namespace Spard.Common
{
    internal sealed class CacheData
    {
        public CacheDictionary Cache { get; } = new CacheDictionary();

        public int Index { get; set; }
        public List<SimpleTransformState> ActiveList { get; set; }
    }
}
