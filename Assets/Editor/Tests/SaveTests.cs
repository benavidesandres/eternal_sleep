using EternalSleep.Dream;
using EternalSleep.Player;
using EternalSleep.Village;
using NUnit.Framework;
using UnityEngine;

namespace EternalSleep.Tests
{
    /// <summary>
    /// Guardar y cargar la partida devuelve el mismo estado. No toca el archivo de la partida real:
    /// prueba el paso a JSON y WriteTo/ReadFrom de cada parte.
    /// </summary>
    public class SaveTests
    {
        [SetUp, TearDown]
        public void ResetState()
        {
            DreamWorldManager.ResetProgress();
            PlayerUpgrades.Reset();
            CollectibleTracker.Reset();
        }

        private static SaveData Sample() => new SaveData
        {
            partida = 12,
            despiertos = { "Aldeano_1", "Aldeano_3" },
            finalVisto = true,
            frasesId = { "Aldeano_1" },
            frasesTexto = { "Gracias, Kael." },
            mejoras = { "DoubleJump", "Dash", "ExtraHeart", "NoExiste" },
            recogidosMejoras = { "corazon_mausoleo" },
            recompensasDespertar = 2,
            corazonesExtra = 1,
            lagrimas = { "lagrima_pozo", "lagrima_torre" },
            secretos = { "sala_espejo" },
        };

        private static SaveData Load(SaveData data)
        {
            var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            DreamWorldManager.ReadFrom(copy);
            PlayerUpgrades.ReadFrom(copy);
            CollectibleTracker.ReadFrom(copy);
            return copy;
        }

        [Test]
        public void CargarRecuperaElEstado()
        {
            Load(Sample());

            Assert.That(DreamWorldManager.IsAwake("Aldeano_1") && DreamWorldManager.IsAwake("Aldeano_3"));
            Assert.That(DreamWorldManager.IsAwake("Aldeano_2"), Is.False);
            Assert.That(DreamWorldManager.WakeLine("Aldeano_1"), Is.EqualTo("Gracias, Kael."));
            Assert.That(PlayerUpgrades.Has(Upgrade.DoubleJump) && PlayerUpgrades.Has(Upgrade.Dash));
            Assert.That(PlayerUpgrades.Has(Upgrade.WallJump), Is.False);
            Assert.That(PlayerUpgrades.BonusHearts, Is.EqualTo(1));
            Assert.That(PlayerUpgrades.IsClaimed("corazon_mausoleo"));
            Assert.That(CollectibleTracker.Count, Is.EqualTo(2));
            Assert.That(CollectibleTracker.IsCollected("lagrima_torre"));
            Assert.That(CollectibleTracker.SecretsFound, Is.EqualTo(1));
        }

        [Test]
        public void GuardarLoCargadoDaLoMismo()
        {
            Load(Sample());
            var saved = new SaveData();
            DreamWorldManager.WriteTo(saved);
            PlayerUpgrades.WriteTo(saved);
            CollectibleTracker.WriteTo(saved);

            var expected = Sample();
            expected.mejoras.Remove("NoExiste"); // una mejora desconocida se ignora al cargar
            CollectionAssert.AreEquivalent(expected.despiertos, saved.despiertos);
            Assert.That(saved.finalVisto, Is.True);
            CollectionAssert.AreEquivalent(expected.mejoras, saved.mejoras);
            CollectionAssert.AreEquivalent(expected.recogidosMejoras, saved.recogidosMejoras);
            Assert.That(saved.recompensasDespertar, Is.EqualTo(2));
            Assert.That(saved.corazonesExtra, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(expected.lagrimas, saved.lagrimas);
            CollectionAssert.AreEquivalent(expected.secretos, saved.secretos);
            CollectionAssert.AreEqual(expected.frasesId, saved.frasesId);
            CollectionAssert.AreEqual(expected.frasesTexto, saved.frasesTexto);
        }

        [Test]
        public void UnaPartidaNuevaEmpiezaVacia()
        {
            Load(Sample());
            ResetState();
            var saved = new SaveData();
            DreamWorldManager.WriteTo(saved);
            PlayerUpgrades.WriteTo(saved);
            CollectibleTracker.WriteTo(saved);
            Assert.That(saved.despiertos, Is.Empty);
            Assert.That(saved.finalVisto, Is.False);
            Assert.That(saved.mejoras, Is.Empty);
            Assert.That(saved.lagrimas, Is.Empty);
        }
    }
}
