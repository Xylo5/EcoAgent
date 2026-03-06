using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Building shop panel with keyboard navigation.
/// Tab cycles through buildings, Enter selects.
/// Disables input for 1 frame after selecting to prevent BuildingPlacer
/// from reading the same Enter press.
/// </summary>
public class BuildingUI : MonoBehaviour
{
    [Header("References")]
    public BuildingPlacer buildingPlacer;

    [Header("Building List")]
    public BuildingData[] buildings;

    [Header("UI")]
    public GameObject shopPanel;
    public Transform buttonContainer;
    public GameObject buttonPrefab;

    [Header("Selection Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0.1f, 0.6f, 0.1f, 1f);

    private int selectedIndex = 0;
    private GameObject[] buttonObjects;
    private bool shopActive = true;

    void Start()
    {
        CreateBuildingButtons();
        UpdateButtonHighlight();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Tab reopens the shop if closed, then cycles buildings
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // If shop is hidden, reopen it first
            if (!shopActive)
            {
                ShowShop();
                return;
            }

            // Cycle through buildings
            if (Keyboard.current.leftShiftKey.isPressed)
                selectedIndex = (selectedIndex - 1 + buildings.Length) % buildings.Length;
            else
                selectedIndex = (selectedIndex + 1) % buildings.Length;

            UpdateButtonHighlight();
        }

        // Enter = select the highlighted building (only when shop is open)
        if (shopActive &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            if (buildings.Length > 0)
            {
                buildingPlacer.StartPlacing(buildings[selectedIndex]);
                HideShop();
            }
        }
    }

    void CreateBuildingButtons()
    {
        buttonObjects = new GameObject[buildings.Length];

        for (int i = 0; i < buildings.Length; i++)
        {
            BuildingData building = buildings[i];
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            btnObj.name = "Btn_" + building.buildingName;

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = building.buildingName + "\n(" + building.sizeInCells + "x" + building.sizeInCells + ")";

            buttonObjects[i] = btnObj;
        }
    }

    void UpdateButtonHighlight()
    {
        for (int i = 0; i < buttonObjects.Length; i++)
        {
            Image img = buttonObjects[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == selectedIndex) ? selectedColor : normalColor;

            buttonObjects[i].transform.localScale = (i == selectedIndex)
                ? Vector3.one * 1.15f
                : Vector3.one;
        }
    }

    public void HideShop()
    {
        shopActive = false;
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    public void ShowShop()
    {
        shopActive = true;
        if (shopPanel != null)
            shopPanel.SetActive(true);
        UpdateButtonHighlight();
    }
}
