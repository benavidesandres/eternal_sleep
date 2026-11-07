using System.Collections;
using EternalSleep.Effects;
using EternalSleep.Player;
using EternalSleep.Village;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Arena del final de los sueños (escena "Arena").
    /// Despierta al jefe que toca según las mejoras que ya tiene Kael (PlayerUpgrades.NextBossIndex):
    /// primero la Bestia, luego la Reina de las Polillas y por último el Ojo.
    /// Al vencerlo suelta su orbe de mejora y, al recogerlo, se abre el portal para despertar al aldeano.
    /// </summary>
    public class BossArena : MonoBehaviour
    {
        [Tooltip("En el mismo orden que las recompensas de PlayerUpgrades. Empiezan desactivados.")]
        [SerializeField] private BossBase[] bosses;
        [SerializeField] private GameObject portal;
        [SerializeField] private UpgradePickup reward;
        [SerializeField] private float rewardDelay = 1.2f;
        [Tooltip("Altura sobre el suelo a la que aparece el orbe.")]
        [SerializeField] private float rewardHeight = 1.5f;

        private int index;
        private float floorY;

        private void Start()
        {
            index = PlayerUpgrades.NextBossIndex(); // -1 si ya los venció a todos (al probar la escena sola)
            if (portal != null) portal.SetActive(false);
            if (reward != null) reward.gameObject.SetActive(false);
            if (bosses == null || bosses.Length == 0) { OpenPortal(); return; }

            foreach (var b in bosses) if (b != null) b.gameObject.SetActive(false);
            var boss = bosses[Mathf.Clamp(index, 0, bosses.Length - 1)];
            floorY = portal != null ? portal.transform.position.y : boss.transform.position.y;
            boss.gameObject.SetActive(true);
            boss.Defeated += OnDefeated;
        }

        private void OnDefeated(BossBase boss)
        {
            boss.Defeated -= OnDefeated;
            StartCoroutine(Victory(boss.transform.position));
        }

        private IEnumerator Victory(Vector2 at)
        {
            yield return new WaitForSeconds(rewardDelay);
            if (index < 0 || reward == null) { OpenPortal(); yield break; }

            reward.transform.position = new Vector3(at.x, floorY + rewardHeight, 0f);
            reward.Setup(PlayerUpgrades.BossReward(index));
            reward.Claimed += OpenPortal;
            reward.gameObject.SetActive(true);
            HitFeedback.Burst(reward.transform.position, new Color(0.8f, 0.7f, 1f), 24, 5f, 0.12f, -2f);
        }

        private void OpenPortal()
        {
            if (portal == null) return;
            portal.SetActive(true);
            HitFeedback.Burst((Vector2)portal.transform.position + Vector2.up, new Color(0.85f, 0.8f, 1f), 24, 5f, 0.12f, -2f);
        }
    }
}
