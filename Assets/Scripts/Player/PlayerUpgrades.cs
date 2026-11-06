using System;
using System.Collections.Generic;

namespace EternalSleep.Player
{
    public enum Upgrade { DoubleJump, Dash, ExtraHeart }

    /// <summary>
    /// Mejoras que Kael consigue durante la partida.
    /// Cada aldeano que despierta le regala la siguiente de la lista (WakeRewards);
    /// el Corazón de Luna escondido en el mausoleo da un corazón más.
    /// </summary>
    public static class PlayerUpgrades
    {
        private static readonly Upgrade[] WakeRewards = { Upgrade.DoubleJump, Upgrade.Dash, Upgrade.ExtraHeart };

        private static readonly HashSet<Upgrade> owned = new HashSet<Upgrade>();
        private static readonly HashSet<string> claimedHearts = new HashSet<string>();
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

        public static bool IsHeartClaimed(string id) => claimedHearts.Contains(id);

        public static void ClaimHeart(string id)
        {
            if (claimedHearts.Add(id)) Grant(Upgrade.ExtraHeart);
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
            claimedHearts.Clear();
            wakeRewardsGiven = 0;
            BonusHearts = 0;
            HeartAdded = null;
        }
    }
}
