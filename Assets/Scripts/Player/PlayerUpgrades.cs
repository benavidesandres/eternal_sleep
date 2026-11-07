using System;
using System.Collections.Generic;

namespace EternalSleep.Player
{
    public enum Upgrade { DoubleJump, Dash, ExtraHeart, WallJump, GroundSlam, ChargedAttack }

    /// <summary>
    /// Mejoras que Kael consigue durante la partida.
    /// Cada aldeano que despierta le regala la siguiente de la lista (WakeRewards);
    /// el Corazón de Luna escondido en el mausoleo da un corazón más
    /// y las otras tres las suelta cada jefe al morir (BossRewards), en un orbe que se recoge (UpgradePickup).
    /// </summary>
    public static class PlayerUpgrades
    {
        private static readonly Upgrade[] WakeRewards = { Upgrade.DoubleJump, Upgrade.Dash, Upgrade.ExtraHeart };

        /// <summary>Lo que suelta cada jefe al morir, en orden: Bestia, Reina de las Polillas y Ojo.</summary>
        private static readonly Upgrade[] BossRewards = { Upgrade.WallJump, Upgrade.GroundSlam, Upgrade.ChargedAttack };

        private static readonly HashSet<Upgrade> owned = new HashSet<Upgrade>();
        private static readonly HashSet<string> claimedPickups = new HashSet<string>();
        private static int wakeRewardsGiven;

        public static int BonusHearts { get; private set; }

        /// <summary>Se dispara al conseguir un corazón extra (la vida máxima cambia en plena escena).</summary>
        public static event Action HeartAdded;

        public static bool Has(Upgrade upgrade) => owned.Contains(upgrade);

        public static void Grant(Upgrade upgrade)
        {
            owned.Add(upgrade);
            if (upgrade != Upgrade.ExtraHeart) return;
            BonusHearts++;
            HeartAdded?.Invoke();
        }

        /// <summary>Entrega la siguiente mejora por despertar a un aldeano. Devuelve false si ya no quedan.</summary>
        public static bool GrantNextWakeReward(out Upgrade reward)
        {
            reward = default;
            if (wakeRewardsGiven >= WakeRewards.Length) return false;
            reward = WakeRewards[wakeRewardsGiven++];
            Grant(reward);
            return true;
        }

        /// <summary>Qué jefe toca ahora (0, 1 o 2). -1 si ya se vencieron todos.</summary>
        public static int NextBossIndex()
        {
            for (int i = 0; i < BossRewards.Length; i++)
                if (!Has(BossRewards[i])) return i;
            return -1;
        }

        public static Upgrade BossReward(int index) => BossRewards[index];

        public static bool IsHeartClaimed(string id) => IsClaimed(id);

        public static void ClaimHeart(string id) => Claim(id, Upgrade.ExtraHeart);

        /// <summary>¿Ya se recogió el objeto con este id en esta partida?</summary>
        public static bool IsClaimed(string id) => claimedPickups.Contains(id);

        /// <summary>Recoge un objeto del mapa y da su mejora (solo la primera vez).</summary>
        public static void Claim(string id, Upgrade upgrade)
        {
            if (claimedPickups.Add(id)) Grant(upgrade);
        }

        /// <summary>Texto que se muestra al recibir la mejora.</summary>
        public static string[] Describe(Upgrade upgrade)
        {
            switch (upgrade)
            {
                case Upgrade.DoubleJump:
                    return new[]
                    {
                        "Al despertar, deja en tu mano un polvo de alas de polilla.",
                        "ALAS DE POLILLA: pulsa saltar otra vez en el aire para hacer un doble salto.",
                        "Ahora puedes subir a la torre del pueblo y llegar al barrio del cementerio, al otro lado.",
                    };
                case Upgrade.Dash:
                    return new[]
                    {
                        "Al despertar, una sombra se desprende de su manta y se enrosca en tus pies.",
                        "PASO DE SOMBRA: pulsa Shift o K (mando: RB) para lanzarte hacia delante, también en el aire.",
                        "Con él cruzas los velos de sombra, como el que sella el mausoleo del cementerio.",
                    };
                case Upgrade.WallJump:
                    return new[]
                    {
                        "Una hiedra de sueño te trepa por los dedos y ya no te suelta.",
                        "GARRAS DE HIEDRA: en el aire, empuja contra una pared para deslizarte despacio.",
                        "Pulsa saltar mientras te deslizas para impulsarte hacia el otro lado.",
                    };
                case Upgrade.GroundSlam:
                    return new[]
                    {
                        "Sientes sobre la espalda el peso de todos los que duermen en el pueblo.",
                        "PESO DEL SUEÑO: en el aire, mantén abajo y pulsa Shift o K (mando: RB) para caer en picado.",
                        "El impacto daña a los enemigos cercanos y rompe los suelos agrietados.",
                    };
                case Upgrade.ChargedAttack:
                    return new[]
                    {
                        "Aprendes a contener el aliento antes de golpear.",
                        "ALIENTO CONTENIDO: mantén pulsado atacar hasta que brilles y suelta.",
                        "El golpe cargado hace el triple de daño.",
                    };
                default:
                    return new[]
                    {
                        "Un calor suave te llena el pecho.",
                        "CORAZÓN DE LUNA: tu vida máxima aumenta en un corazón.",
                    };
            }
        }

        public static void Reset()
        {
            owned.Clear();
            claimedPickups.Clear();
            wakeRewardsGiven = 0;
            BonusHearts = 0;
            HeartAdded = null;
        }
    }
}
