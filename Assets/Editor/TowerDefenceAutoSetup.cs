using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TowerDefenceAutoSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string DamageTowerPath = "Assets/prefabs/Damaging tower.prefab";
    private const string HealingTowerPath = "Assets/prefabs/Healing tower.prefab";
    private const string ManagerPath = "Assets/prefabs/Gamemanager.prefab";
    private static string SetupKey =>
        "TowerDefenceAutoSetupV2_" + Application.dataPath;

    static TowerDefenceAutoSetup()
    {
        EditorApplication.delayCall += RunAutomaticSetup;
    }

    private static void RunAutomaticSetup()
    {
        if (!EditorPrefs.GetBool(SetupKey, false))
        {
            RunSetup();
        }
    }

    [MenuItem("Tools/Tower Defence/Run Complete Setup")]
    public static void RunSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            return;

        ConfigureTowerPrefab(DamageTowerPath, false);
        ConfigureTowerPrefab(HealingTowerPath, true);
        ConfigureScene(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorPrefs.SetBool(SetupKey, true);
        Debug.Log("Tower defence scene and prefabs were configured successfully.");
    }

    private static void ConfigureTowerPrefab(string path, bool isHealingTower)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.tag = "Player";

            Tower tower = root.GetComponent<Tower>();
            Health health = root.GetComponent<Health>();
            if (health == null)
            {
                health = root.AddComponent<Health>();
            }

            if (root.GetComponent<TeamMember>() == null)
            {
                root.AddComponent<TeamMember>();
            }

            foreach (TowerplacementAndbuying misplacedManager in
                root.GetComponents<TowerplacementAndbuying>())
            {
                Object.DestroyImmediate(misplacedManager);
            }

            SerializedObject towerObject = new SerializedObject(tower);
            SerializedProperty targetType = towerObject.FindProperty("targetType");
            if (targetType != null)
            {
                targetType.enumValueIndex = isHealingTower ? 1 : 0;
            }

            SerializedProperty placementRadius =
                towerObject.FindProperty("placementRadius");
            if (placementRadius != null && placementRadius.floatValue <= 0f)
            {
                placementRadius.floatValue = 0.65f;
            }

            towerObject.ApplyModifiedPropertiesWithoutUndo();
            ConfigureTowerHealthDisplay(root, health);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureTowerHealthDisplay(
        GameObject root,
        Health health)
    {
        TowerHealthDisplay display = root.GetComponent<TowerHealthDisplay>();
        if (display == null)
        {
            display = root.AddComponent<TowerHealthDisplay>();
        }

        Transform existingDisplay = root.transform.Find("Health Display");
        GameObject canvasObject;

        if (existingDisplay == null)
        {
            canvasObject = new GameObject(
                "Health Display",
                typeof(RectTransform),
                typeof(Canvas)
            );
            canvasObject.transform.SetParent(root.transform, false);
        }
        else
        {
            canvasObject = existingDisplay.gameObject;
        }

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(120f, 24f);
        canvasRect.localPosition = new Vector3(0f, 0.8f, 0f);
        canvasRect.localScale = Vector3.one * 0.01f;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        TMP_Text healthText = canvasObject.GetComponentInChildren<TMP_Text>();
        if (healthText == null)
        {
            healthText = CreateText(
                canvasObject.transform,
                "Health Text",
                Vector2.zero,
                new Vector2(120f, 24f),
                "10 / 10",
                20f,
                TextAlignmentOptions.Center
            );
        }

        healthText.raycastTarget = false;

        SerializedObject displayObject = new SerializedObject(display);
        displayObject.FindProperty("healthText").objectReferenceValue = healthText;
        displayObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureScene(Scene scene)
    {
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager == null)
        {
            GameObject managerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPath);
            GameObject managerObject = PrefabUtility.InstantiatePrefab(
                managerPrefab,
                scene
            ) as GameObject;
            managerObject.name = "GameManager";
            manager = managerObject.GetComponent<GameManager>();
        }

        SerializedObject managerData = new SerializedObject(manager);
        SerializedProperty phase = managerData.FindProperty(
            "<CurrentPhase>k__BackingField"
        );
        if (phase != null)
        {
            phase.enumValueIndex = 0;
        }

        SerializedProperty startingHealth =
            managerData.FindProperty("startingPlayerHealth");
        if (startingHealth != null)
        {
            startingHealth.intValue = 10;
        }

        SerializedProperty startingCoins =
            managerData.FindProperty("startingCoins");
        if (startingCoins != null)
        {
            startingCoins.intValue = 1000;
        }

        managerData.ApplyModifiedPropertiesWithoutUndo();

        GameObject systems = GameObject.Find("Game Systems");
        if (systems == null)
        {
            systems = new GameObject("Game Systems");
            SceneManager.MoveGameObjectToScene(systems, scene);
        }

        GameObject existingPlacementObject = GameObject.Find("TowerPlacement");
        TowerplacementAndbuying placement = existingPlacementObject != null
            ? existingPlacementObject.GetComponent<TowerplacementAndbuying>()
            : null;

        if (placement == null)
        {
            placement = systems.GetComponent<TowerplacementAndbuying>();
            if (placement == null)
            {
                placement = systems.AddComponent<TowerplacementAndbuying>();
            }
        }
        else
        {
            TowerplacementAndbuying duplicatePlacement =
                systems.GetComponent<TowerplacementAndbuying>();
            if (duplicatePlacement != null && duplicatePlacement != placement)
            {
                Object.DestroyImmediate(duplicatePlacement);
            }
        }

        PlayerHealth hudController = systems.GetComponent<PlayerHealth>();
        if (hudController == null)
        {
            hudController = systems.AddComponent<PlayerHealth>();
        }

        EnsureEventSystem(scene);
        Canvas canvas = EnsureHudCanvas(scene);
        ConfigureHud(canvas, placement, hudController);
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
            return;

        GameObject eventObject = new GameObject(
            "EventSystem",
            typeof(EventSystem)
        );
        SceneManager.MoveGameObjectToScene(eventObject, scene);

#if ENABLE_INPUT_SYSTEM
        eventObject.AddComponent<
            UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static Canvas EnsureHudCanvas(Scene scene)
    {
        GameObject canvasObject = GameObject.Find("Game HUD");
        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                "Game HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void ConfigureHud(
        Canvas canvas,
        TowerplacementAndbuying placement,
        PlayerHealth hudController)
    {
        TMP_Text healthText = EnsureHudText(
            canvas.transform,
            "Player Health Text",
            new Vector2(0f, 1f),
            new Vector2(20f, -20f),
            new Vector2(280f, 50f),
            "Health: 10",
            TextAlignmentOptions.Left
        );

        TMP_Text coinText = EnsureHudText(
            canvas.transform,
            "Coin Text",
            new Vector2(0f, 1f),
            new Vector2(20f, -75f),
            new Vector2(280f, 50f),
            "Coins: 1000",
            TextAlignmentOptions.Left
        );

        SerializedObject hudObject = new SerializedObject(hudController);
        hudObject.FindProperty("hearttext").objectReferenceValue = healthText;
        hudObject.FindProperty("cointext").objectReferenceValue = coinText;
        hudObject.ApplyModifiedPropertiesWithoutUndo();

        bool hasExistingReadyButton = HasSceneComponentOutside<ReadyButton>(
            canvas.transform
        );

        if (hasExistingReadyButton)
        {
            DestroyChildIfPresent(canvas.transform, "Ready Button");
        }
        else
        {
            EnsureReadyButton(canvas.transform);
        }

        int existingTowerButtons = CountSceneComponentsOutside<TowerDragButton>(
            canvas.transform
        );

        if (existingTowerButtons >= 2)
        {
            DestroyChildIfPresent(canvas.transform, "Damage Tower Button");
            DestroyChildIfPresent(canvas.transform, "Healing Tower Button");
        }
        else
        {
            EnsureTowerButton(
                canvas,
                placement,
                DamageTowerPath,
                "Damage Tower Button",
                new Vector2(-120f, 55f),
                "Damage Tower"
            );
            EnsureTowerButton(
                canvas,
                placement,
                HealingTowerPath,
                "Healing Tower Button",
                new Vector2(120f, 55f),
                "Healing Tower"
            );
        }
    }

    private static bool HasSceneComponentOutside<T>(Transform excludedRoot)
        where T : Component
    {
        return CountSceneComponentsOutside<T>(excludedRoot) > 0;
    }

    private static int CountSceneComponentsOutside<T>(Transform excludedRoot)
        where T : Component
    {
        int count = 0;

        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (!component.gameObject.scene.IsValid())
                continue;

            if (!component.transform.IsChildOf(excludedRoot))
            {
                count++;
            }
        }

        return count;
    }

    private static void DestroyChildIfPresent(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static TMP_Text EnsureHudText(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        string value,
        TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(name);
        TMP_Text text = existing != null
            ? existing.GetComponent<TMP_Text>()
            : CreateText(parent, name, anchor, size, value, 30f, alignment);

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        text.text = value;
        return text;
    }

    private static void EnsureReadyButton(Transform parent)
    {
        Transform existing = parent.Find("Ready Button");
        GameObject buttonObject;

        if (existing == null)
        {
            buttonObject = CreateButtonObject(
                parent,
                "Ready Button",
                new Color(0.2f, 0.7f, 0.25f, 0.95f)
            );
        }
        else
        {
            buttonObject = existing.gameObject;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-25f, -25f);
        rect.sizeDelta = new Vector2(200f, 70f);

        if (buttonObject.GetComponent<ReadyButton>() == null)
        {
            buttonObject.AddComponent<ReadyButton>();
        }

        EnsureButtonLabel(buttonObject.transform, "READY", 30f);
    }

    private static void EnsureTowerButton(
        Canvas canvas,
        TowerplacementAndbuying placement,
        string prefabPath,
        string objectName,
        Vector2 position,
        string label)
    {
        GameObject towerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Transform existing = canvas.transform.Find(objectName);
        GameObject buttonObject;

        if (existing == null)
        {
            buttonObject = CreateButtonObject(
                canvas.transform,
                objectName,
                new Color(0.15f, 0.2f, 0.3f, 0.95f)
            );
        }
        else
        {
            buttonObject = existing.gameObject;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(210f, 80f);

        SpriteRenderer towerSprite = towerPrefab.GetComponent<SpriteRenderer>();
        if (towerSprite != null)
        {
            buttonObject.GetComponent<Image>().sprite = towerSprite.sprite;
            buttonObject.GetComponent<Image>().preserveAspect = true;
        }

        Tower tower = towerPrefab.GetComponent<Tower>();
        EnsureButtonLabel(
            buttonObject.transform,
            $"{label}\n{tower.Cost} coins",
            22f
        );

        TowerDragButton dragButton =
            buttonObject.GetComponent<TowerDragButton>();
        if (dragButton == null)
        {
            dragButton = buttonObject.AddComponent<TowerDragButton>();
        }

        SerializedObject dragData = new SerializedObject(dragButton);
        dragData.FindProperty("towerPrefab").objectReferenceValue = towerPrefab;
        dragData.FindProperty("placementManager").objectReferenceValue = placement;
        dragData.FindProperty("canvas").objectReferenceValue = canvas;
        dragData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateButtonObject(
        Transform parent,
        string name,
        Color color)
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = color;
        return buttonObject;
    }

    private static void EnsureButtonLabel(
        Transform button,
        string label,
        float fontSize)
    {
        Transform existing = button.Find("Label");
        TMP_Text text = existing != null
            ? existing.GetComponent<TMP_Text>()
            : CreateText(
                button,
                "Label",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                label,
                fontSize,
                TextAlignmentOptions.Center
            );

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.text = label;
        text.fontSize = fontSize;
        text.raycastTarget = false;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 size,
        string value,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
