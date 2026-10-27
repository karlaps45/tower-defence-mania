using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentDebugPanel : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Optional: drag in the same .ttf your HUD text uses (IMGUI can't use TextMeshPro font assets)")]
    [SerializeField] private Font font;
    [SerializeField] private int fontSize = 12;
    [SerializeField] private float panelWidth = 290f;
    [Tooltip("The UI scales with screen height relative to this")]
    [SerializeField] private float referenceHeight = 540f;
    [SerializeField] private bool startOpen = false;
    [Tooltip("Off = black item names. On = names in their rarity color")]
    [SerializeField] private bool colorRarityNames = false;

    [Header("Position")]
    [Tooltip("Where the panel starts. Drag its title bar to move it in play mode")]
    [SerializeField] private Vector2 startPosition = new Vector2(8f, 98f);

    private const float HandleHeight = 26f;

    // Palette: white borders, gray insides, black text.
    private static readonly Color Border = Color.white;
    private static readonly Color Text = Color.black;
    private static readonly Color PanelFill = new Color(0.62f, 0.62f, 0.62f, 0.96f);
    private static readonly Color CardFill = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color EmptyFill = new Color(0.67f, 0.67f, 0.67f, 1f);
    private static readonly Color ButtonFill = new Color(0.80f, 0.80f, 0.80f, 1f);
    private static readonly Color SelectedFill = new Color(0.48f, 0.48f, 0.48f, 1f);
    private static readonly Color ScrollTrack = new Color(0.45f, 0.45f, 0.45f, 1f);

    private EquipmentSlots selectedTower;
    private ItemData selectedItem;
    private Vector2 scroll;
    private bool open;
    private bool dragging;
    private Vector2 panelPos;
    private Rect toggleRect;
    private Rect panelRect;
    private Action pending;

    private GUISkin skin;
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private GUIStyle panel, card, slotEmpty, line;
    private GUIStyle label, title, section, statName, statValue;
    private GUIStyle button, row, rowSelected;

    private void Start()
    {
        open = startOpen;
        panelPos = startPosition;
    }

    private void OnDestroy()
    {
        foreach (Texture2D texture in textures)
        {
            if (texture != null) Destroy(texture);
        }
        if (skin != null) Destroy(skin);
    }

    // ---------- Main ----------

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.height / referenceHeight);
        Vector2 virtualSize = new Vector2(Screen.width / scale, Screen.height / scale);
        float panelHeight = Mathf.Clamp(virtualSize.y - 210f, 200f, 400f);

        HandleDrag(scale, virtualSize);

        // The toggle sits under the Health / Coins text.
        toggleRect = new Rect(8f, 66f, 78f, 26f);
        panelRect = new Rect(panelPos.x, panelPos.y, panelWidth, panelHeight);

        HandleTowerClick(scale);   // uses raw screen coordinates, so it runs before the scale is applied

        GUISkin previousSkin = GUI.skin;
        Matrix4x4 previousMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        EnsureStyles();
        GUI.skin = skin;

        try
        {
            Draw();
        }
        finally
        {
            GUI.skin = previousSkin;
            GUI.matrix = previousMatrix;
        }

        // Changes are applied after drawing so the layout never changes mid-pass.
        if (pending != null)
        {
            Action action = pending;
            pending = null;
            action();
        }
    }

    // Drag the title bar to move the panel. It can go almost fully off-screen.
    private void HandleDrag(float scale, Vector2 virtualSize)
    {
        Event e = Event.current;
        Rect handle = new Rect(panelPos.x, panelPos.y, panelWidth, HandleHeight);

        if (open && e.type == EventType.MouseDown && e.button == 0 &&
            handle.Contains(e.mousePosition / scale))
        {
            dragging = true;
            e.Use();
        }
        else if (dragging && e.type == EventType.MouseDrag)
        {
            panelPos += e.delta / scale;
            e.Use();
        }

        // rawType also catches releasing the mouse outside the window.
        if (dragging && e.rawType == EventType.MouseUp)
        {
            dragging = false;
        }

        // Always keep a strip of the title bar on screen so it can be grabbed again.
        panelPos.x = Mathf.Clamp(panelPos.x, 40f - panelWidth, virtualSize.x - 40f);
        panelPos.y = Mathf.Clamp(panelPos.y, 0f, virtualSize.y - HandleHeight);
    }

    private void Draw()
    {
        PlayerInventory inventory = PlayerInventory.Instance;

        if (GUI.Button(toggleRect, open ? "Close" : "Items", button))
        {
            pending = () => { open = !open; dragging = false; };
        }

        if (!open) return;

        GUILayout.BeginArea(panelRect, panel);
        GUILayout.Label("Equipment  (drag here to move)", title);
        GUILayout.Box(GUIContent.none, line);

        if (inventory == null)
        {
            GUILayout.Label("Add a PlayerInventory to the scene.", label);
            GUILayout.EndArea();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, skin.verticalScrollbar);
        DrawTower(inventory);
        GUILayout.Space(6f);
        GUILayout.Box(GUIContent.none, line);
        DrawInventory(inventory);
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void HandleTowerClick(float scale)
    {
        Event e = Event.current;
        if (!open || e.type != EventType.MouseDown || e.button != 0)
            return;

        Vector2 uiPoint = e.mousePosition / scale;
        if (toggleRect.Contains(uiPoint) || panelRect.Contains(uiPoint))
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y, 0f);
        Vector2 world = cam.ScreenToWorldPoint(screen);

        foreach (Collider2D col in Physics2D.OverlapPointAll(world))
        {
            Tower tower = col.GetComponentInParent<Tower>();
            if (tower == null || tower.IsDetectionCollider(col)) continue;

            EquipmentSlots slots = tower.GetComponent<EquipmentSlots>();
            if (slots == null) slots = tower.gameObject.AddComponent<EquipmentSlots>();

            selectedTower = slots;
            return;
        }
    }

    private string NameOf(ItemData item)
    {
        return colorRarityNames ? item.ColoredName : item.itemName;
    }

    // ---------- Sections ----------

    private void DrawTower(PlayerInventory inventory)
    {
        GUILayout.Label("Tower", section);

        if (selectedTower == null)
        {
            GUILayout.Label("Tap a tower on the map to select it.", label);
            return;
        }

        GUILayout.Label($"<b>{selectedTower.name}</b>", label);

        TowerStats stats = selectedTower.GetComponent<TowerStats>();
        if (stats != null)
        {
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(StatLabel(type), statName);
                GUILayout.FlexibleSpace();
                GUILayout.Label(StatValue(type, stats.Get(type)), statValue);
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(4f);
        GUILayout.Label("Slots", section);

        for (int i = 0; i < selectedTower.SlotCount; i++)
        {
            int slot = i;
            ItemData item = selectedTower.GetItem(slot);

            GUILayout.BeginHorizontal(item != null ? card : slotEmpty);
            GUILayout.Label($"<b>{selectedTower.GetSlotType(slot)}</b>", statName, GUILayout.Width(50f));

            GUILayout.Label(
                item != null ? NameOf(item) : "<i>empty</i>",
                label, GUILayout.ExpandWidth(true));

            if (item != null)
            {
                if (GUILayout.Button("Info", button, GUILayout.Width(38f)))
                {
                    pending = () => selectedItem = item;
                }
                if (GUILayout.Button("Unequip", button, GUILayout.Width(62f)))
                {
                    pending = () => inventory.UnequipToInventory(selectedTower, slot);
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawInventory(PlayerInventory inventory)
    {
        GUILayout.Label($"Inventory ({inventory.Items.Count})", section);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1", button))
            pending = () => inventory.Add(ItemGenerator.GenerateRandom());
        if (GUILayout.Button("+5", button))
            pending = () => { for (int n = 0; n < 5; n++) inventory.Add(ItemGenerator.GenerateRandom()); };
        if (GUILayout.Button("+Leg", button))
            pending = () => inventory.Add(ItemGenerator.Generate(ItemRarity.Legendary));
        if (GUILayout.Button("+Myth", button))
            pending = () => inventory.Add(ItemGenerator.Generate(ItemRarity.Mythical));
        GUILayout.EndHorizontal();

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemData item = inventory.Items[i];
            bool isSelected = item == selectedItem;

            if (GUILayout.Button($"{NameOf(item)}  ({item.rarity} {item.slot})",
                    isSelected ? rowSelected : row))
            {
                pending = () => selectedItem = item;
            }
        }

        if (selectedItem == null) return;

        GUILayout.Space(6f);
        GUILayout.BeginVertical(card);
        GUILayout.Label($"<b>{NameOf(selectedItem)}</b>", label);
        GUILayout.Label($"{selectedItem.rarity} {selectedItem.slot}", label);
        GUILayout.Space(2f);
        GUILayout.Label(selectedItem.GetDescription(), label);

        bool inInventory = inventory.Items.Contains(selectedItem);
        bool canEquip = inInventory && selectedTower != null && selectedTower.CanEquip(selectedItem);

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();

        GUI.enabled = canEquip;
        if (GUILayout.Button("Equip", button))
        {
            ItemData item = selectedItem;
            pending = () => inventory.EquipFromInventory(item, selectedTower);
        }

        GUI.enabled = inInventory;
        if (GUILayout.Button("Discard", button))
        {
            ItemData item = selectedItem;
            pending = () => { inventory.Remove(item); selectedItem = null; };
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    // ---------- Text helpers ----------

    private static string StatLabel(StatType type)
    {
        switch (type)
        {
            case StatType.PhysicalPower: return "Physical";
            case StatType.MagicPower: return "Magic";
            case StatType.DetectionRange: return "Range";
            case StatType.MaxHealth: return "Health";
            case StatType.AttackSpeed: return "Speed";
            case StatType.CriticalChance: return "Crit chance";
            default: return "Crit damage";
        }
    }

    private static string StatValue(StatType type, float value)
    {
        switch (type)
        {
            case StatType.CriticalChance: return $"{value:0.#}%";
            case StatType.CriticalDamage: return $"{value:0.##}x";
            case StatType.AttackSpeed: return $"{value:0.##}";
            default: return $"{value:0.#}";
        }
    }

    // ---------- Styles ----------

    private Texture2D MakeBox(Color fill, Color outline)
    {
        // 3x3 texture: a 1px outline around a fill, stretched as a 9-slice.
        Texture2D texture = new Texture2D(3, 3, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                texture.SetPixel(x, y, x == 1 && y == 1 ? fill : outline);
            }
        }

        texture.Apply();
        textures.Add(texture);
        return texture;
    }

    private GUIStyle MakeBoxStyle(Color fill, Color outline, int padding)
    {
        GUIStyle style = new GUIStyle(skin.box)
        {
            border = new RectOffset(1, 1, 1, 1),
            padding = new RectOffset(padding, padding, padding, padding),
            margin = new RectOffset(0, 0, 2, 2)
        };
        style.normal.background = MakeBox(fill, outline);
        return style;
    }

    private GUIStyle MakeButton(Color fill)
    {
        GUIStyle style = new GUIStyle(skin.button)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            border = new RectOffset(1, 1, 1, 1),
            padding = new RectOffset(6, 6, 4, 4),
            margin = new RectOffset(2, 2, 2, 2)
        };

        // No hover effect: hovering looks the same as normal. Pressing darkens slightly.
        Texture2D normal = MakeBox(fill, Border);
        style.normal.background = normal;
        style.hover.background = normal;
        style.focused.background = normal;
        style.active.background = MakeBox(Color.Lerp(fill, Color.black, 0.2f), Border);

        style.normal.textColor = Text;
        style.hover.textColor = Text;
        style.active.textColor = Text;
        style.focused.textColor = Text;

        if (font != null) style.font = font;
        return style;
    }

    private GUIStyle MakeLabel(int size, FontStyle fontStyle = FontStyle.Normal)
    {
        GUIStyle style = new GUIStyle(skin.label)
        {
            fontSize = size,
            fontStyle = fontStyle,
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            margin = new RectOffset(2, 2, 1, 1),
            padding = new RectOffset(0, 0, 0, 0)
        };
        style.normal.textColor = Text;

        if (font != null) style.font = font;
        return style;
    }

    private void EnsureStyles()
    {
        if (skin != null) return;

        skin = Instantiate(GUI.skin);

        // Thin white scrollbar thumb on a gray track.
        skin.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
        skin.verticalScrollbar.fixedWidth = 8f;
        skin.verticalScrollbar.normal.background = MakeBox(ScrollTrack, ScrollTrack);
        skin.verticalScrollbarThumb.border = new RectOffset(1, 1, 1, 1);
        skin.verticalScrollbarThumb.fixedWidth = 8f;
        skin.verticalScrollbarThumb.normal.background = MakeBox(Border, Border);
        skin.verticalScrollbarUpButton.fixedHeight = 0f;
        skin.verticalScrollbarUpButton.fixedWidth = 0f;
        skin.verticalScrollbarDownButton.fixedHeight = 0f;
        skin.verticalScrollbarDownButton.fixedWidth = 0f;

        panel = MakeBoxStyle(PanelFill, Border, 8);
        panel.margin = new RectOffset(0, 0, 0, 0);
        card = MakeBoxStyle(CardFill, Border, 5);
        slotEmpty = MakeBoxStyle(EmptyFill, Border, 5);

        label = MakeLabel(fontSize);
        title = MakeLabel(fontSize + 3, FontStyle.Bold);
        section = MakeLabel(fontSize, FontStyle.Bold);
        statName = MakeLabel(fontSize);
        statValue = MakeLabel(fontSize, FontStyle.Bold);
        statValue.alignment = TextAnchor.UpperRight;
        statValue.wordWrap = false;

        button = MakeButton(ButtonFill);
        row = MakeButton(CardFill);
        row.alignment = TextAnchor.MiddleLeft;
        rowSelected = MakeButton(SelectedFill);
        rowSelected.alignment = TextAnchor.MiddleLeft;

        line = new GUIStyle(skin.box)
        {
            border = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 4, 4),
            fixedHeight = 2f
        };
        line.normal.background = MakeBox(Border, Border);
    }
}