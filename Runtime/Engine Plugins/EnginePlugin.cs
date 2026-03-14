using Ibralogue.Parser;
using UnityEngine;

namespace Ibralogue.Plugins
{ 
    public abstract class EnginePlugin : MonoBehaviour
    {
        public abstract void Display(Line line);
        public abstract void Clear();
    }
}
