using System;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Contrato de solo lectura entre el Motor (física) y quien quiera observarlo
    /// (Animator, sonido, partículas, cámara...). El Motor no sabe quién lo escucha.
    /// </summary>
    public interface IMotorState
    {
        Vector2 Velocity { get; }
        bool IsGrounded { get; }
        int FacingDirection { get; } // 1 = derecha, -1 = izquierda
        bool IsDashing { get; }

        event Action Jumped;
        event Action Landed;
        event Action AirJumped;
        event Action Dashed;
    }
}
