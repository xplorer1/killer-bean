using System.IO;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Enemies;
using BeanBlaster.Level;
using BeanBlaster.Player;
using BeanBlaster.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeanBlaster.Editor
{
    /// <summary>Builds the disposable player vertical-slice scene used before full setup automation exists.</summary>
    public static class Phase5TestSceneBuilder
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/Phase5_PlayerTest.unity";
        private const string ResourceDirectory = "Assets/Resources";
        private const string PrefabDirectory = "Assets/Prefabs/Development";
        private const string PistolAssetPath = ResourceDirectory + "/DualPistols.asset";
        private const string DatabaseAssetPath = ResourceDirectory + "/WeaponDatabase.asset";
        private const string ProjectilePrefabPath = PrefabDirectory + "/StandardProjectile.prefab";

        /// <summary>Creates or replaces the Phase 5 movement and respawn verification scene.</summary>
        [MenuItem("Tools/Game Setup/Development/Create Phase 5 Test Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(SceneDirectory);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            CombatAssets combatAssets = CreateCombatAssets(square);
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
            ProjectilePool projectilePool = new GameObject("ProjectilePool").AddComponent<ProjectilePool>();
            PlayerBuild player = CreatePlayer(square, spawn.transform.position, manager);
            CreatePlayerWeaponManager(player, combatAssets, projectilePool);
            CreateCombatTarget(square, "Target A", new Vector2(4f, 0.25f));
            CreateCombatTarget(square, "Target B", new Vector2(6.5f, 0.25f));
            CreateCombatTarget(square, "Target C", new Vector2(9f, 0.25f));
            CreateTouchHud(square, player.input);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
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
            cameraObject.AddComponent<CameraShake>();
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

        private static PlayerBuild CreatePlayer(Sprite sprite, Vector3 position, LevelManager manager)
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
            return new PlayerBuild
            {
                gameObject = player,
                input = input,
                controller = controller,
                ownerCollider = collider,
                fireSocket = fireSocket.transform,
                gunRenderer = gunRenderer
            };
        }

        private static void CreateTouchHud(Sprite sprite, PlayerInputReader playerInput)
        {
            GameObject canvasObject = new GameObject("MobileHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeAreaObject = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter), typeof(MobileInputState));
            RectTransform safeArea = safeAreaObject.GetComponent<RectTransform>();
            safeArea.SetParent(canvasObject.transform, false);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            MobileInputState mobileInput = safeAreaObject.GetComponent<MobileInputState>();
            playerInput.SetMobileInput(mobileInput);

            CreateStick(safeArea, sprite, mobileInput, "Move Stick", new Vector2(180f, 180f), false, VirtualTouchStickRole.Move);
            CreateStick(safeArea, sprite, mobileInput, "Aim / Fire Stick", new Vector2(-180f, 180f), true, VirtualTouchStickRole.AimAndFire);
            CreateButton(safeArea, sprite, mobileInput, "JUMP", new Vector2(-470f, 130f), new Vector2(210f, 150f), false, TouchActionButtonRole.Jump);
            CreateButton(safeArea, sprite, mobileInput, "PREV", new Vector2(-340f, -95f), new Vector2(150f, 90f), true, TouchActionButtonRole.PreviousWeapon);
            CreateButton(safeArea, sprite, mobileInput, "NEXT", new Vector2(-170f, -95f), new Vector2(150f, 90f), true, TouchActionButtonRole.NextWeapon);
            CreateButton(safeArea, sprite, mobileInput, "II", new Vector2(-75f, -70f), new Vector2(100f, 100f), true, TouchActionButtonRole.Pause);
            CreateInstruction(safeArea);

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static CombatAssets CreateCombatAssets(Sprite sprite)
        {
            Directory.CreateDirectory(ResourceDirectory);
            Directory.CreateDirectory(PrefabDirectory);
            AssetDatabase.Refresh();

            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (projectilePrefab == null)
            {
                GameObject projectileObject = new GameObject("StandardProjectile");
                projectileObject.layer = 9;
                Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.12f;
                collider.isTrigger = true;
                SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = new Color(1f, 0.9f, 0.2f);
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(0.32f, 0.14f);
                renderer.sortingOrder = 2;
                projectileObject.AddComponent<Projectile>();
                projectilePrefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ProjectilePrefabPath);
                Object.DestroyImmediate(projectileObject);
            }

            WeaponData pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(PistolAssetPath);
            if (pistol == null)
            {
                pistol = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(pistol, PistolAssetPath);
            }

            SerializedObject pistolSerialized = new SerializedObject(pistol);
            pistolSerialized.FindProperty("id").stringValue = "pistol_dual";
            pistolSerialized.FindProperty("displayName").stringValue = "Dual Pistols";
            pistolSerialized.FindProperty("shopSlot").intValue = 1;
            pistolSerialized.FindProperty("cost").intValue = 0;
            pistolSerialized.FindProperty("fireRate").floatValue = 0.2f;
            pistolSerialized.FindProperty("damage").floatValue = 15f;
            pistolSerialized.FindProperty("projectileSpeed").floatValue = 22f;
            pistolSerialized.FindProperty("projectileGravityScale").floatValue = 0f;
            pistolSerialized.FindProperty("projectileLifetime").floatValue = 2f;
            pistolSerialized.FindProperty("knockbackForce").floatValue = 2f;
            pistolSerialized.FindProperty("ammoType").enumValueIndex = (int)AmmoType.Standard;
            pistolSerialized.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            pistolSerialized.FindProperty("projectileColor").colorValue = new Color(1f, 0.9f, 0.2f);
            pistolSerialized.FindProperty("usesAmmo").boolValue = false;
            pistolSerialized.FindProperty("ammoPerShot").intValue = 0;
            pistolSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pistol);

            WeaponDatabase database = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(DatabaseAssetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<WeaponDatabase>();
                AssetDatabase.CreateAsset(database, DatabaseAssetPath);
            }

            SerializedObject databaseSerialized = new SerializedObject(database);
            SerializedProperty weapons = databaseSerialized.FindProperty("weapons");
            weapons.arraySize = 1;
            weapons.GetArrayElementAtIndex(0).objectReferenceValue = pistol;
            databaseSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            return new CombatAssets
            {
                database = database
            };
        }

        private static void CreatePlayerWeaponManager(
            PlayerBuild player,
            CombatAssets assets,
            ProjectilePool projectilePool)
        {
            WeaponManager weaponManager = player.gameObject.AddComponent<WeaponManager>();
            SetObjectReference(weaponManager, "database", assets.database);
            SetObjectReference(weaponManager, "input", player.input);
            SetObjectReference(weaponManager, "fireSocket", player.fireSocket);
            SetObjectReference(weaponManager, "gunRenderer", player.gunRenderer);
            SetObjectReference(weaponManager, "player", player.controller);
            SetObjectReference(weaponManager, "projectilePool", projectilePool);
            SetObjectReference(weaponManager, "ownerCollider", player.ownerCollider);
        }

        private static void CreateCombatTarget(Sprite sprite, string name, Vector2 position)
        {
            GameObject target = new GameObject(name);
            target.layer = 8;
            target.transform.position = position;
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 1.5f);
            SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = collider.size;
            Health health = target.AddComponent<Health>();
            SetEnum(health, "team", (int)Team.Enemy);
            SetFloat(health, "maxHealth", 60f);

            GameObject barBackground = new GameObject("HealthBar", typeof(SpriteRenderer));
            barBackground.transform.SetParent(target.transform, false);
            barBackground.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            barBackground.transform.localScale = new Vector3(1.1f, 0.12f, 1f);
            SpriteRenderer backgroundRenderer = barBackground.GetComponent<SpriteRenderer>();
            backgroundRenderer.sprite = sprite;
            backgroundRenderer.color = new Color(0.1f, 0.1f, 0.1f);
            backgroundRenderer.sortingOrder = 3;

            GameObject fillObject = new GameObject("Fill", typeof(SpriteRenderer));
            fillObject.transform.SetParent(barBackground.transform, false);
            fillObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            SpriteRenderer fillRenderer = fillObject.GetComponent<SpriteRenderer>();
            fillRenderer.sprite = sprite;
            fillRenderer.color = new Color(0.2f, 1f, 0.2f);
            fillRenderer.sortingOrder = 4;
            target.AddComponent<CombatTargetDummy>().Configure(fillObject.transform);
        }

        private static void CreateInstruction(RectTransform parent)
        {
            GameObject textObject = new GameObject("Combat Instruction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -35f);
            rect.sizeDelta = new Vector2(760f, 70f);
            Text text = textObject.GetComponent<Text>();
            text.text = "DRAG ORANGE STICK TO AIM + FIRE";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private static void CreateStick(
            RectTransform parent,
            Sprite sprite,
            MobileInputState input,
            string name,
            Vector2 anchoredPosition,
            bool anchorRight,
            VirtualTouchStickRole role)
        {
            GameObject backgroundObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VirtualTouchStick));
            RectTransform background = backgroundObject.GetComponent<RectTransform>();
            background.SetParent(parent, false);
            background.anchorMin = anchorRight ? new Vector2(1f, 0f) : Vector2.zero;
            background.anchorMax = background.anchorMin;
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = anchoredPosition;
            background.sizeDelta = new Vector2(280f, 280f);
            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.sprite = sprite;
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.45f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform handle = handleObject.GetComponent<RectTransform>();
            handle.SetParent(background, false);
            handle.anchorMin = new Vector2(0.5f, 0.5f);
            handle.anchorMax = handle.anchorMin;
            handle.sizeDelta = new Vector2(110f, 110f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = sprite;
            handleImage.color = role == VirtualTouchStickRole.Move
                ? new Color(0.2f, 0.75f, 1f, 0.75f)
                : new Color(1f, 0.45f, 0.15f, 0.75f);
            handleImage.raycastTarget = false;

            backgroundObject.GetComponent<VirtualTouchStick>().Configure(input, background, handle, role);
        }

        private static void CreateButton(
            RectTransform parent,
            Sprite sprite,
            MobileInputState input,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            bool anchorTopRight,
            TouchActionButtonRole role)
        {
            GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TouchActionButton));
            RectTransform button = buttonObject.GetComponent<RectTransform>();
            button.SetParent(parent, false);
            button.anchorMin = anchorTopRight ? Vector2.one : new Vector2(1f, 0f);
            button.anchorMax = button.anchorMin;
            button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = anchoredPosition;
            button.sizeDelta = size;
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.65f);
            buttonObject.GetComponent<TouchActionButton>().Configure(input, role);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(button, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
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

        private struct PlayerBuild
        {
            public GameObject gameObject;
            public PlayerInputReader input;
            public PlayerController2D controller;
            public Collider2D ownerCollider;
            public Transform fireSocket;
            public SpriteRenderer gunRenderer;
        }

        private struct CombatAssets
        {
            public WeaponDatabase database;
        }
    }
}
