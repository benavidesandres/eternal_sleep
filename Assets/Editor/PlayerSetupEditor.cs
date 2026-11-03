#if UNITY_EDITOR
using System.IO;
using System.Linq;
using EternalSleep.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Menú: Eternal Sleep > Configurar jugador.
    /// Crea Player.controller con los clips de Assets/sprites/player/animations (idle, run, jump_up, fall, land y 3 ataques).
    /// Crea la capa "Ground" si no existe.
    /// Crea en la escena un Player con todos los scripts y un suelo de prueba.
    /// </summary>
    public static class PlayerSetupEditor
    {
        private const string AnimFolder = "Assets/sprites/player/animations";
        private const string ControllerPath = AnimFolder + "/Player.controller";
        private const string GroundLayerName = "Ground";

        [MenuItem("Eternal Sleep/Configurar jugador")]
        public static void SetupPlayer()
        {
            var existing = Object.FindAnyObjectByType<PlayerMotor>();
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Eternal Sleep", "Ya hay un Player en la escena. ¿Reemplazarlo por uno nuevo?", "Reemplazar", "Cancelar"))
                    return;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            int groundLayer = EnsureLayer(GroundLayerName);
            if (groundLayer < 0) return;

            var controller = BuildController();
            if (controller == null) return;

            CreatePlayer(controller, groundLayer);
            if (GameObject.Find("Ground") == null) CreateGround(groundLayer);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Eternal Sleep] Jugador configurado. Guarda la escena con Ctrl+S y pulsa Play.");
        }

        // ---------- Animator Controller ----------

        private static AnimationClip FindClip(string name) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimFolder}/{name}.anim");

        private static AnimatorController BuildController()
        {
            string[] names = { "idle", "run", "jump_up", "fall", "land", "attack_forward", "attack_up", "attack_down" };
            var clips = names.ToDictionary(n => n, FindClip);
            var missing = names.Where(n => clips[n] == null).ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("Eternal Sleep",
                    $"Faltan clips en {AnimFolder}: {string.Join(", ", missing)}.\nCopia la carpeta Assets/sprites/player completa (con sus .meta).", "OK");
                return null;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            c.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            c.AddParameter("AttackForward", AnimatorControllerParameterType.Trigger);
            c.AddParameter("AttackUp", AnimatorControllerParameterType.Trigger);
            c.AddParameter("AttackDown", AnimatorControllerParameterType.Trigger);

            var sm = c.layers[0].stateMachine;
            AnimatorState State(string stateName, string clip, float x, float y)
            {
                var st = sm.AddState(stateName, new Vector3(x, y));
                st.motion = clips[clip];
                return st;
            }

            var idle   = State("Idle", "idle", 300, 0);
            var run    = State("Run", "run", 300, 100);
            var jumpUp = State("JumpUp", "jump_up", 560, 0);
            var fall   = State("Fall", "fall", 560, 100);
            var land   = State("Land", "land", 560, 200);
            var atkF   = State("AttackForward", "attack_forward", 300, 260);
            var atkU   = State("AttackUp", "attack_up", 300, 330);
            var atkD   = State("AttackDown", "attack_down", 300, 400);
            sm.defaultState = idle;

            const AnimatorConditionMode Gt = AnimatorConditionMode.Greater, Lt = AnimatorConditionMode.Less;
            const AnimatorConditionMode Yes = AnimatorConditionMode.If, No = AnimatorConditionMode.IfNot;

            // Suelo: Idle <-> Run (izquierda y derecha usan el mismo clip; el sprite se voltea por código)
            Add(idle, run, false, 0f, (Gt, "Speed", 0.1f));
            Add(run, idle, false, 0f, (Lt, "Speed", 0.1f));

            // Dejar el suelo: subiendo -> JumpUp, cayendo (p. ej. al salir de una plataforma) -> Fall
            foreach (var ground in new[] { idle, run, land })
            {
                Add(ground, jumpUp, false, 0f, (No, "IsGrounded", 0f), (Gt, "VerticalVelocity", 0.1f));
                Add(ground, fall, false, 0f, (No, "IsGrounded", 0f), (Lt, "VerticalVelocity", -0.1f));
            }

            // Aire
            Add(jumpUp, fall, false, 0f, (Lt, "VerticalVelocity", 0f));
            Add(jumpUp, land, false, 0f, (Yes, "IsGrounded", 0f));
            Add(fall, land, false, 0f, (Yes, "IsGrounded", 0f));

            // Aterrizaje: si ya te mueves pasa a Run, si no espera a que acabe el clip
            Add(land, run, false, 0f, (Gt, "Speed", 0.1f));
            Add(land, idle, true, 1f);

            // Ataques desde cualquier estado; al terminar vuelven a Idle
            AddAny(sm, atkF, "AttackForward");
            AddAny(sm, atkU, "AttackUp");
            AddAny(sm, atkD, "AttackDown");
            Add(atkF, idle, true, 1f);
            Add(atkU, idle, true, 1f);
            Add(atkD, idle, true, 1f);

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        private static void Add(AnimatorState from, AnimatorState to, bool exitTime, float exitAt,
            params (AnimatorConditionMode mode, string param, float threshold)[] conditions)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = exitTime;
            t.exitTime = exitAt;
            t.duration = 0f;
            foreach (var cond in conditions) t.AddCondition(cond.mode, cond.threshold, cond.param);
        }

        private static void AddAny(AnimatorStateMachine sm, AnimatorState to, string trigger)
        {
            var t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        // ---------- Capas ----------

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++) // 0-7 son capas reservadas por Unity
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            EditorUtility.DisplayDialog("Eternal Sleep", "No quedan capas libres para crear 'Ground'.", "OK");
            return -1;
        }

        // ---------- Objetos de escena ----------

        private static void CreatePlayer(AnimatorController controller, int groundLayer)
        {
            var go = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(go, "Crear Player");
            go.transform.position = new Vector3(0f, -3.99f, 0f); // los pies están en el pivote del sprite

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/player/idle_00.png");
            sr.sortingOrder = 10;

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.7f, 1.75f);   // el cuerpo mide ~1.85 unidades (PPU 200)
            col.offset = new Vector2(0f, 0.875f);  // el pivote del sprite está en los pies
            col.sharedMaterial = GetNoFrictionMaterial();

            go.AddComponent<PlayerInputReader>();
            var motor = go.AddComponent<PlayerMotor>();
            go.AddComponent<PlayerAttack>();
            var anim = go.AddComponent<PlayerAnimator>();

            var so = new SerializedObject(motor);
            so.FindProperty("groundLayer").intValue = 1 << groundLayer;
            so.ApplyModifiedProperties();

            var soAnim = new SerializedObject(anim);
            soAnim.FindProperty("animator").objectReferenceValue = animator;
            soAnim.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soAnim.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }

        private static void CreateGround(int groundLayer)
        {
            var go = new GameObject("Ground");
            Undo.RegisterCreatedObjectUndo(go, "Crear Ground");
            go.layer = groundLayer;
            go.transform.position = new Vector3(0f, -4.5f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(40f, 1f);

            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite != null)
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(40f, 1f);
                sr.color = new Color(0.2f, 0.2f, 0.25f);
            }
        }

        private static PhysicsMaterial2D GetNoFrictionMaterial()
        {
            const string folder = "Assets/Physics";
            const string path = folder + "/NoFriction.physicsMaterial2D";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (mat != null) return mat;

            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Physics");
            mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
#endif
