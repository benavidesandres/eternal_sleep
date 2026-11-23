#if UNITY_EDITOR
using System.IO;
using System.Linq;
using EternalSleep.CameraSystem;
using EternalSleep.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Menú: Eternal Sleep > Configurar jugador.
    /// Crea Player.controller con los clips de Assets/sprites/player/animations (los genera Eternal Sleep > Importar reaper).
    /// Crea la capa "Ground" si no existe.
    /// Crea en la escena un Player con todos los scripts y un suelo de prueba.
    /// </summary>
    public static class PlayerSetupEditor
    {
        private const string AnimFolder = "Assets/sprites/player/animations";
        private const string ControllerPath = AnimFolder + "/Player.controller";
        private const string GroundLayerName = "Ground";
        private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Eternal Sleep/Configurar jugador")]
        public static void SetupPlayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Eternal Sleep", "Detén el juego (botón Play) antes de usar esta opción.", "OK");
                return;
            }

            // El jugador siempre va en la escena del juego (la que carga el botón Jugar).
            if (EditorSceneManager.GetActiveScene().path != GameScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(GameScenePath);
            }

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

            var player = CreatePlayer(controller, groundLayer);
            if (GameObject.Find("Ground") == null) CreateGround(groundLayer);
            SetupCameraFollow(player.transform);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Eternal Sleep] Jugador configurado y guardado en " + GameScenePath + ". Pulsa Play.");
        }

        // ---------- Animator Controller ----------

        private static AnimationClip FindClip(string name) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimFolder}/{name}.anim");

        internal static AnimatorController BuildController()
        {
            string[] names = { "idle", "run", "jump_start", "jump_up", "fall", "land", "dash", "hurt",
                               "attack_forward", "attack_up", "attack_down", "slam" };
            var clips = names.ToDictionary(n => n, FindClip);
            var missing = names.Where(n => clips[n] == null).ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("Eternal Sleep",
                    $"Faltan clips en {AnimFolder}: {string.Join(", ", missing)}.\nUsa Eternal Sleep > Importar reaper.", "OK");
                return null;
            }

            // Se vacía en vez de borrarlo: las escenas lo referencian por GUID.
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            else ClearController(c);

            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            c.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            c.AddParameter("AttackForward", AnimatorControllerParameterType.Trigger);
            c.AddParameter("AttackUp", AnimatorControllerParameterType.Trigger);
            c.AddParameter("AttackDown", AnimatorControllerParameterType.Trigger);
            c.AddParameter("ComboStep", AnimatorControllerParameterType.Int);
            c.AddParameter("IsWallSliding", AnimatorControllerParameterType.Bool);
            c.AddParameter("IsSlamming", AnimatorControllerParameterType.Bool);
            c.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
            c.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

            var sm = c.layers[0].stateMachine;
            AnimatorState State(string stateName, string clip, float x, float y)
            {
                var st = sm.AddState(stateName, new Vector3(x, y));
                st.motion = clips[clip];
                return st;
            }

            var idle   = State("Idle", "idle", 300, 0);
            var run    = State("Run", "run", 300, 100);
            var jumpStart = State("JumpStart", "jump_start", 560, -80);
            var jumpUp = State("JumpUp", "jump_up", 560, 0);
            var fall   = State("Fall", "fall", 560, 100);
            var land   = State("Land", "land", 560, 200);
            var atkF   = State("AttackForward", "attack_forward", 300, 260);
            var atkU   = State("AttackUp", "attack_up", 300, 330);
            var atkD   = State("AttackDown", "attack_down", 300, 400);
            // Sin sprites propios: el combo reusa el golpe al frente y la pared la caída.
            var atkF2  = State("AttackForward2", "attack_forward", 60, 260);
            var atkF3  = State("AttackForward3", "attack_forward", 60, 330);
            var wall   = State("WallSlide", "fall", 800, 100);
            var slam   = State("GroundSlam", "slam", 800, 200);
            var dash   = State("Dash", "dash", 800, 300);
            var hurt   = State("Hurt", "hurt", 800, 400);
            atkF2.speed = 1.3f;
            atkF3.speed = 1.15f;
            sm.defaultState = idle;

            const AnimatorConditionMode Gt = AnimatorConditionMode.Greater, Lt = AnimatorConditionMode.Less;
            const AnimatorConditionMode Yes = AnimatorConditionMode.If, No = AnimatorConditionMode.IfNot;
            const AnimatorConditionMode Eq = AnimatorConditionMode.Equals;

            // Suelo: Idle <-> Run (izquierda y derecha usan el mismo clip; el sprite se voltea por código)
            Add(idle, run, false, 0f, (Gt, "Speed", 0.1f));
            Add(run, idle, false, 0f, (Lt, "Speed", 0.1f));

            // Dejar el suelo: subiendo -> JumpStart (impulso) -> JumpUp, cayendo (p. ej. al salir de una plataforma) -> Fall
            foreach (var ground in new[] { idle, run, land })
            {
                Add(ground, jumpStart, false, 0f, (No, "IsGrounded", 0f), (Gt, "VerticalVelocity", 0.1f));
                Add(ground, fall, false, 0f, (No, "IsGrounded", 0f), (Lt, "VerticalVelocity", -0.1f));
            }

            // Aire
            Add(jumpStart, jumpUp, true, 1f);
            Add(jumpStart, fall, false, 0f, (Lt, "VerticalVelocity", 0f));
            Add(jumpStart, land, false, 0f, (Yes, "IsGrounded", 0f));
            Add(jumpUp, fall, false, 0f, (Lt, "VerticalVelocity", 0f));
            Add(jumpUp, land, false, 0f, (Yes, "IsGrounded", 0f));
            Add(fall, land, false, 0f, (Yes, "IsGrounded", 0f));

            // Aterrizaje: si ya te mueves pasa a Run, si no espera a que acabe el clip
            Add(land, run, false, 0f, (Gt, "Speed", 0.1f));
            Add(land, idle, true, 1f);

            // Pared: se agarra desde el aire; al soltarse sube (salto de pared), cae o aterriza
            Add(jumpStart, wall, false, 0f, (Yes, "IsWallSliding", 0f));
            Add(jumpUp, wall, false, 0f, (Yes, "IsWallSliding", 0f));
            Add(fall, wall, false, 0f, (Yes, "IsWallSliding", 0f));
            Add(wall, land, false, 0f, (Yes, "IsGrounded", 0f));
            Add(wall, jumpUp, false, 0f, (No, "IsWallSliding", 0f), (Gt, "VerticalVelocity", 0.1f));
            Add(wall, fall, false, 0f, (No, "IsWallSliding", 0f));

            // Golpe al suelo desde cualquier estado; termina al aterrizar
            AddAny(sm, slam, (Yes, "IsSlamming", 0f));
            Add(slam, land, false, 0f, (No, "IsSlamming", 0f));

            // Recibir daño y dash interrumpen cualquier cosa; al terminar vuelven al suelo o al aire.
            AddAny(sm, hurt, (Yes, "Hurt", 0f));
            AddAny(sm, dash, (Yes, "IsDashing", 0f));
            foreach (var st in new[] { hurt, dash })
            {
                Add(st, idle, true, 1f, (Yes, "IsGrounded", 0f));
                Add(st, fall, true, 1f, (No, "IsGrounded", 0f));
            }

            // Ataques desde cualquier estado; al terminar vuelven a Idle.
            // Hacia delante hay un estado por golpe del combo para poder encadenarlos sin esperar al clip.
            // Pueden reiniciarse a sí mismos: el clip dura más que la espera entre ataques, y el
            // trigger solo se activa cuando el ataque sale de verdad.
            AddAny(sm, atkF, true, (Yes, "AttackForward", 0f), (Eq, "ComboStep", 0f));
            AddAny(sm, atkF2, true, (Yes, "AttackForward", 0f), (Eq, "ComboStep", 1f));
            AddAny(sm, atkF3, true, (Yes, "AttackForward", 0f), (Eq, "ComboStep", 2f));
            AddAny(sm, atkU, true, (Yes, "AttackUp", 0f));
            AddAny(sm, atkD, true, (Yes, "AttackDown", 0f));
            // Si te mueves o estás en el aire, sale al desvanecerse el corte y se salta la recuperación.
            float fadeEnd = ReaperSetupEditor.AttackFadeEnd;
            foreach (var atk in new[] { atkF, atkF2, atkF3, atkU })
            {
                Add(atk, run, true, fadeEnd, (Gt, "Speed", 0.1f), (Yes, "IsGrounded", 0f));
                Add(atk, fall, true, fadeEnd, (No, "IsGrounded", 0f));
                Add(atk, idle, true, 1f);
            }
            Add(atkD, fall, true, 1f, (No, "IsGrounded", 0f));
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

        private static void AddAny(AnimatorStateMachine sm, AnimatorState to,
            params (AnimatorConditionMode mode, string param, float threshold)[] conditions) =>
            AddAny(sm, to, false, conditions);

        private static void AddAny(AnimatorStateMachine sm, AnimatorState to, bool toSelf,
            params (AnimatorConditionMode mode, string param, float threshold)[] conditions)
        {
            var t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = toSelf;
            foreach (var cond in conditions) t.AddCondition(cond.mode, cond.threshold, cond.param);
        }

        private static void ClearController(AnimatorController c)
        {
            while (c.parameters.Length > 0) c.RemoveParameter(0);
            while (c.layers.Length > 1) c.RemoveLayer(1);
            var sm = c.layers[0].stateMachine;
            foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
            foreach (var t in sm.entryTransitions) sm.RemoveEntryTransition(t);
            foreach (var s in sm.states) sm.RemoveState(s.state);
            foreach (var sub in sm.stateMachines) sm.RemoveStateMachine(sub.stateMachine);
        }

        // ---------- Capas ----------

        internal static int EnsureLayer(string layerName)
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

        private static void SetupCameraFollow(Transform player)
        {
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = Undo.AddComponent<CameraFollow2D>(cam.gameObject);

            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player;
            so.ApplyModifiedProperties();
        }

        internal static GameObject CreatePlayer(AnimatorController controller, int groundLayer)
        {
            var go = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(go, "Crear Player");
            go.transform.position = new Vector3(0f, -3.99f, 0f); // los pies están en el pivote del sprite

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAllAssetsAtPath(ReaperSetupEditor.IdleSpritePath)
                .OfType<Sprite>().FirstOrDefault(s => s.name == "reposo_0");
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
            go.AddComponent<PlayerAbilityFeedback>();

            var so = new SerializedObject(motor);
            so.FindProperty("groundLayer").intValue = 1 << groundLayer;
            so.ApplyModifiedProperties();

            var soAnim = new SerializedObject(anim);
            soAnim.FindProperty("animator").objectReferenceValue = animator;
            soAnim.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soAnim.ApplyModifiedProperties();

            Selection.activeGameObject = go;
            return go;
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
