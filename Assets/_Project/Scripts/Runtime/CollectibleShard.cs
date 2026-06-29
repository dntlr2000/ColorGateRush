using UnityEngine;

namespace ColorGateRush
{
    public sealed class CollectibleShard : MonoBehaviour
    {
        public ColorId ColorId { get; private set; }

        // Stores the shard color used by collection scoring and renames the generated object.
        public void Configure(ColorId colorId)
        {
            ColorId = colorId;
            name = "Shard_" + colorId;
        }
    }
}
