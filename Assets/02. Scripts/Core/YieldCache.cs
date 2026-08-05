using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public static class YieldCache
    {
        private static Dictionary<float, WaitForSeconds> wfsCache = new();
        
        public static WaitForSeconds WaitForSeconds(float seconds)
        {
            if(!wfsCache.ContainsKey(seconds))
                wfsCache[seconds] = new WaitForSeconds(seconds);
            
            return wfsCache[seconds];
        }
    }
}