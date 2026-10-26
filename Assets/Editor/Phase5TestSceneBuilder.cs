using System.IO;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Level;
using BeanBlaster.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeanBlaster.Editor
{
    /// <summary>Builds the disposable player vertical-slice scene used before full setup automation exists.</summary>
    public static class Phase5TestSceneBuilder
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/Phase5_PlayerTest.unity";

        /// <summary>Creates or replaces the Phase 5 movement and respawn verification scene.</summary>
        [MenuItem("Tools/Game Setup/Development/Create Phase 5 Test Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(SceneDirectory);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            CreateCamera();
            GameObject managerObject = new GameObject("LevelManager");
            LevelManager manager = managerObject.AddComponent<LevelManager>();

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(0f, 1.5f, 0f);
            SetObjectReference(manager, "playerSpawnPoint", spawn.transform);

            CreateGround(square, new Vector2(0f, -1f), new Vector2(24f, 1f));
            CreateGround(square, new Vector2(7f, 1.5f), new Vector2(3f, 0.5f));
            CreateGround(square, new Vector2(-7f, 3f), new Vector2(3f, 0.5f));
            CreateKillPlane(manager);
            CreatePlayer(square, spawn.transform.position, manager);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created Phase 5 test scene at '{ScenePath}'.");
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateGround(Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject ground = new GameObject("Ground");
            ground.layer = 6;
            ground.transform.position = position;
            ground.transform.localScale = size;
            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.2f, 0.25f, 0.2f);
            ground.AddComponent<BoxCollider2D>();
        }

        private static void CreateKillPlane(LevelManager manager)
        {
            GameObject killPlane = new GameObject("KillPlane");
            killPlane.layer = 12;
            killPlane.transform.position = new Vector3(0f, manager.KillPlaneY, 0f);
            BoxCollider2D collider = killPlane.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(100f, 2f);
            killPlane.AddComponent<KillPlane>();
        }

        private static void CreatePlayer(Sprite sprite, Vector3 position, LevelManager manager)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = 7;
            player.transform.position = position;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3.5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.2f, 0.75f, 1f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.8f, 1.6f);

            Health health = player.AddComponent<Health>();
            SetEnum(health, "team", (int)Team.Player);
            SetBoolean(health, "useInvulnerability", true);
            SetFloat(health, "invulnerabilityDuration", 1f);

            PlayerInputReader input = player.AddComponent<PlayerInputReader>();
            GameObject pivotObject = new GameObject("WeaponPivot");
            pivotObject.transform.SetParent(player.transform, false);
            pivotObject.transform.localPosition = new Vector3(0.45f, 0.15f, 0f);
            GameObject gun = new GameObject("GunVisual");
            gun.transform.SetParent(pivotObject.transform, false);
            gun.transform.localPosition = new Vector3(0.3f, 0f, 0f);
            SpriteRenderer gunRenderer = gun.AddComponent<SpriteRenderer>();
            gunRenderer.sprite = sprite;
            gunRenderer.color = new Color(1f, 0.85f, 0.2f);
            gunRenderer.sortingOrder = 1;
            gunRenderer.drawMode = SpriteDrawMode.Sliced;
            gunRenderer.size = new Vector2(0.75f, 0.18f);
            GameObject fireSocket = new GameObject("FireSocket");
            fireSocket.transform.SetParent(pivotObject.transform, false);
            fireSocket.transform.localPosition = new Vector3(0.6f, 0f, 0f);

            PlayerController2D controller = player.AddComponent<PlayerController2D>();
            SetObjectReference(controller, "weaponPivot", pivotObject.transform);
            SetObjectReference(controller, "levelManager", manager);
            SetLayerMask(controller, "groundLayerMask", 1 << 6);
            input.SetAimOrigin(pivotObject.transform);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerMask(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
