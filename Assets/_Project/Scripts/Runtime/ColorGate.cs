using UnityEngine;

namespace ColorGateRush
{
    public sealed class ColorGate : MonoBehaviour
    {
        public ColorId TargetColor { get; private set; }

        // Stores the target color applied to the player when the gate is crossed.
        public void Configure(ColorId targetColor)
        {
            TargetColor = targetColor;
            name = "Gate_" + targetColor;
        }
    }
}
