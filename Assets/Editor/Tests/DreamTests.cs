using System.Collections.Generic;
using System.IO;
using System.Linq;
using EternalSleep.Dream;
using NUnit.Framework;
using UnityEngine;

namespace EternalSleep.Tests
{
    /// <summary>
    /// Pruebas del generador de sueños (sin IA): todo sueño debe tener solución, salir igual con la misma semilla
    /// y tener la forma que espera DreamLevelBuilder. También la limpieza de lo que devuelve la IA.
    /// Se ejecutan en Window > General > Test Runner (EditMode) o con "unity test . --mode EditMode".
    /// </summary>
    public class DreamTests
    {
        private const string ChunksPath = "Assets/Resources/Dreams/Tramos.txt";
        private List<DreamGenerator.Chunk> library;

        [OneTimeSetUp]
        public void LoadChunks() => library = DreamGenerator.Parse(File.ReadAllText(ChunksPath));

        private string[] Dream(int seed, bool doubleJump = false) =>
            DreamGenerator.Generate(DreamRecipe.Procedural(seed, seed % 5), seed, library, doubleJump);

        [Test]
        public void HayTramosDeInicioNormalesYFinal()
        {
            Assert.That(library.Any(c => c.Type == "inicio"), "falta un tramo de inicio");
            Assert.That(library.Any(c => c.Type == "normal"), "faltan tramos normales");
            Assert.That(library.Any(c => c.Type == "final"), "falta un tramo final");
        }

        [Test]
        public void TodosLosSuenosTienenSolucion([Values(false, true)] bool doubleJump)
        {
            for (int seed = 0; seed < 150; seed++)
                Assert.That(DreamValidator.IsSolvable(Dream(seed, doubleJump), doubleJump), $"semilla {seed} sin solución");
        }

        [Test]
        public void ElSuenoDeRespaldoTieneSolucion() =>
            Assert.That(DreamValidator.IsSolvable(DreamGenerator.Fallback(library), false));

        [Test]
        public void LaMismaSemillaDaElMismoSueno() =>
            CollectionAssert.AreEqual(Dream(1234), Dream(1234));

        [Test]
        public void LasSemillasDistintasDanSuenosDistintos()
        {
            var distinct = new HashSet<string>(Enumerable.Range(0, 60).Select(s => string.Join("\n", Dream(s))));
            Assert.That(distinct.Count, Is.GreaterThan(50));
        }

        [Test]
        public void ElMapaTieneFormaCorrecta()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var map = Dream(seed);
                Assert.That(map.Length, Is.EqualTo(DreamGenerator.Height), $"semilla {seed}: alto");
                Assert.That(map.All(r => r.Length == map[0].Length), $"semilla {seed}: filas de distinto largo");
                string all = string.Concat(map);
                Assert.That(all.Count(ch => ch == 'P'), Is.EqualTo(1), $"semilla {seed}: inicio");
                Assert.That(all.Count(ch => ch == 'G'), Is.EqualTo(1), $"semilla {seed}: portal");
            }
        }

        // ---------- Recetas de la IA ----------

        [Test]
        public void LaRecetaRotaSeArreglaConLaDeRespaldo()
        {
            var fallback = DreamRecipe.Procedural(7, 0);
            var broken = new DreamRecipe
            {
                tema = "marte", titulo = "  ", intro = null, despertar = null,
                largo = 99, dificultad = -3, verticalidad = 9,
                enemigos = new DreamRecipe.EnemyMix { sombra = 0f, polilla = 0f, vigia = 0f },
                luz = "no es un color", niebla = 5f, tramo_extra = null,
            }.Sanitize(fallback);

            Assert.That(broken.tema, Is.EqualTo(fallback.tema));
            Assert.That(broken.titulo, Is.EqualTo(fallback.titulo));
            Assert.That(broken.intro, Is.EqualTo(fallback.intro));
            Assert.That(broken.largo, Is.InRange(3, 6));
            Assert.That(broken.dificultad, Is.InRange(1, 3));
            Assert.That(broken.verticalidad, Is.InRange(0, 2));
            Assert.That(broken.enemigos, Is.SameAs(fallback.enemigos));
            Assert.That(broken.luz, Is.EqualTo(fallback.luz));
            Assert.That(broken.niebla, Is.InRange(0f, 0.8f));
            Assert.That(broken.tramo_extra, Is.Not.Null);
        }

        [Test]
        public void LosTextosDeLaIASeLimpian()
        {
            var recipe = new DreamRecipe
            {
                titulo = "<color=red>Hola</color>\nmundo" + new string('a', 100),
                intro = new[] { "", "una", "dos", "tres", "cuatro" },
            }.Sanitize(DreamRecipe.Procedural(1, 0));

            Assert.That(recipe.titulo, Does.Not.Contain("<").And.Not.Contain("\n"));
            Assert.That(recipe.titulo.Length, Is.LessThanOrEqualTo(60));
            CollectionAssert.AreEqual(new[] { "una", "dos", "tres" }, recipe.intro);
        }

        [Test]
        public void LaLuzNuncaEsDemasiadoOscura()
        {
            var recipe = new DreamRecipe { luz = "#000000" }.Sanitize(DreamRecipe.Procedural(1, 0));
            Assert.That(ColorUtility.TryParseHtmlString(recipe.luz, out var c));
            Color.RGBToHSV(c, out _, out _, out float v);
            Assert.That(v, Is.GreaterThanOrEqualTo(0.64f));
        }

        // ---------- Semillas ----------

        [Test]
        public void LaSemillaDeCadaAldeanoEsEstable()
        {
            Assert.That(DreamSeed.ForDream("Aldeano_1"), Is.EqualTo(DreamSeed.ForDream("Aldeano_1")));
            Assert.That(DreamSeed.ForDream("Aldeano_1"), Is.Not.EqualTo(DreamSeed.ForDream("Aldeano_2")));
            Assert.That(DreamSeed.ForDream("Aldeano_1", 1), Is.Not.EqualTo(DreamSeed.ForDream("Aldeano_1", 0)));
            Assert.That(DreamSeed.Stable("panadera"), Is.EqualTo(DreamSeed.Stable("panadera")));
        }
    }
}
