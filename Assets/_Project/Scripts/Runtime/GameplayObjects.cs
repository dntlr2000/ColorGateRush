using UnityEngine;

namespace ColorGateRush
{
    public sealed class CollectibleShard : MonoBehaviour
    {
        public ColorId ColorId { get; private set; }

        public void Configure(ColorId colorId)
        {
            ColorId = colorId;
            name = "Shard_" + colorId;
        }
    }

    public sealed class ColorGate : MonoBehaviour
    {
        public ColorId TargetColor { get; private set; }

        public void Configure(ColorId targetColor)
        {
            TargetColor = targetColor;
            name = "Gate_" + targetColor;
        }
    }

    public sealed class ObstacleBlock : MonoBehaviour
    {
    }

    public sealed class FinishLine : MonoBehaviour
    {
    }
}
