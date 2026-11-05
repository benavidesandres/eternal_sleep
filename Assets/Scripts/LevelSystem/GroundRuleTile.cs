using UnityEngine;
using UnityEngine.Tilemaps;

namespace EternalSleep.Level
{
    /// <summary>
    /// Suelo con borde automático. Todas las variantes de suelo (la normal y la de las paredes falsas,
    /// que no tiene colisión) cuentan como "la misma pieza", así la pared falsa no deja costuras que la delaten.
    /// </summary>
    [CreateAssetMenu(menuName = "Eternal Sleep/Ground Rule Tile")]
    public class GroundRuleTile : RuleTile
    {
        public override bool RuleMatch(int neighbor, TileBase other)
        {
            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This: return other is GroundRuleTile;
                case TilingRuleOutput.Neighbor.NotThis: return !(other is GroundRuleTile);
            }
            return base.RuleMatch(neighbor, other);
        }
    }
}
