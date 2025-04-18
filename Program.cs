using Landfall.Haste;
using Landfall.Modding;
using System.Reflection;
using UnityEngine;
using TMPro;
using Zorro.Core;
using static Landfall.Haste.MetaProgression;

namespace CR;

[LandfallPlugin]
public class Program
{
    public static bool subtractMode = false;

    static Program()
    {
        On.PlayerCharacter.Awake += PlayerCharacter_Awake;
        On.Landfall.Haste.MetaProgressionRowUI.AddClicked += MetaProgressionRowUI_AddClicked;
        On.Landfall.Haste.MetaProgressionRowUI.RefreshUI += MetaProgressionRowUI_RefreshUI;
    }

    private static void MetaProgressionRowUI_RefreshUI(On.Landfall.Haste.MetaProgressionRowUI.orig_RefreshUI orig, MetaProgressionRowUI self)
    {
        if (!subtractMode)
        {
            orig(self);
            return;
        }

        Type metaProgressionRowUIType = typeof(MetaProgressionRowUI);
        FieldInfo entryInfo = metaProgressionRowUIType.GetField("_entry", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
        Entry _entry = (Entry)entryInfo.GetValue(self);

        FieldInfo hoveringInfo = metaProgressionRowUIType.GetField("_hovering", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
        bool _hovering = (bool)hoveringInfo.GetValue(self);

        FieldInfo nonHoverColorInfo = metaProgressionRowUIType.GetField("nonHoverColor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
        Color nonHoverColor = (Color)nonHoverColorInfo.GetValue(self);

        int? lastLevel = _entry.GetLastLevel();
        bool canRefund = (lastLevel != null);
        bool flag = (canRefund && _hovering);
        float num = (flag ? MetaProgressionHelper.GetLastLevelValue(self.kind) : GetCurrentValue(self.kind));
        int refundAmount = 0;
        if (canRefund)
        {
            refundAmount = _entry.GetRefundAmount(lastLevel);
            self.cost.gameObject.SetActive(value: true);
            self.costIcon.gameObject.SetActive(value: true);
            self.add.gameObject.SetActive(value: true);
        }
        else
        {
            self.cost.gameObject.SetActive(value: false);
            self.costIcon.gameObject.SetActive(value: false);
            self.add.gameObject.SetActive(value: false);
        }
        /*(int costValue, bool canBuy) costToUpgrade = entry.GetCostToUpgrade();
        int item = costToUpgrade.costValue;
        bool item2 = costToUpgrade.canBuy;
        bool flag = _hovering && item2;
        float num = (flag ? SingletonAsset<MetaProgression>.Instance.GetNextLevelValue(self.kind) : MetaProgression.GetCurrentValue(self.kind));*/
        if (self.kind == MetaProgression.Kind.MaxEnergy)
        {
            num *= 100f;
        }
        self.amount.String.Arguments = new List<object> { num };
        self.amount.String.RefreshString();
        self.amount.Text.color = (flag ? self.hoverColor : nonHoverColor);
        self.cost.text = (canRefund ? "+" : "") + refundAmount.ToString();
        self.cost.alpha = (canRefund ? 1f : self.alphaDisabled);
        Color color = self.costIcon.color;
        color.a = (canRefund ? 1f : self.alphaDisabled);
        self.costIcon.color = color;
    }

    private static void MetaProgressionRowUI_AddClicked(On.Landfall.Haste.MetaProgressionRowUI.orig_AddClicked orig, MetaProgressionRowUI self)
    {
        if (!subtractMode)
        {
            orig(self);
            return;
        }

        Type metaProgressionRowUIType = typeof(MetaProgressionRowUI);
        FieldInfo entryInfo = metaProgressionRowUIType.GetField("_entry", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
        Entry _entry = (Entry)entryInfo.GetValue(self);

        FieldInfo sfxInfo = metaProgressionRowUIType.GetField("sfx", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
        MetaProgressionSFX sfx = (MetaProgressionSFX)sfxInfo.GetValue(self);

        int? lastLevel = _entry.GetLastLevel();
        bool canRefund = (lastLevel != null);

        int refundAmount = _entry.GetRefundAmount(lastLevel);

        /*(int costValue, bool canBuy) costToUpgrade = _entry.GetCostToUpgrade();
        var (num, _) = costToUpgrade;*/
        if (!canRefund)
        {
            if ((bool)sfx && (bool)sfx.noMoney)
            {
                sfx.noMoney.Play();
            }
            Debug.Log($"Player clicked to downgrade {self.kind}, but cannot refund that upgrade!");
            return;
        }

        Assembly asm = typeof(MetaProgressionRowUI).Assembly;
        Type MetaUiType = asm.GetType("Landfall.Haste.MetaProgressionUI");
        if (MetaUiType == null)
        {
            Debug.Log("MetaUiOpen: MetaProgressionUI type not found, returning false.");
            return;
        }

        MethodInfo refreshUiInfo = MetaUiType.GetMethod("RefreshUI", BindingFlags.Instance | BindingFlags.Public);
        if (refreshUiInfo == null)
        {
            Debug.Log("MetaUiOpen: RefreshUI method not found, returning false.");
            return;
        }
        object metaUiInstance = GameObject.FindObjectOfType(MetaUiType);
        if (metaUiInstance == null)
        {
            Debug.Log("MetaUiOpen: MetaProgressionUI instance not found, returning false.");
            return;
        }

        if ((bool)sfx)
        {
            if ((bool)sfx.press)
            {
                sfx.press.Play();
            }
            if ((bool)sfx.increase)
            {
                sfx.increase.sfxs[0].settings.pitch = 1f + (float)refundAmount / 2000f;
                sfx.increase.Play();
            }
        }
        Debug.Log($"Downgrading meta progression stat {self.kind}");
        int? nextLevel = _entry.GetLastLevel();
        if (nextLevel.HasValue)
        {
            int valueOrDefault = nextLevel.GetValueOrDefault();
            _entry.CurrentLevel = valueOrDefault;
            entryInfo.SetValue(self, _entry);
        }
        else
        {
            Debug.LogError($"Error: could not get last level for {_entry.fact} (currernt level = {_entry.CurrentLevel}). This shouldn't happen due to checks above.");
        }
        FactSystem.AddToFact(MetaProgression.MetaProgressionResource, refundAmount);
        SaveSystem.Save();
        Player.localPlayer.ResetStats();
        refreshUiInfo.Invoke(metaUiInstance, null);
        //self.RefreshUI();
        MetaProgressionRowUI[] metaProgressionRowUIs = GameObject.FindObjectsOfType<MetaProgressionRowUI>();
        foreach (MetaProgressionRowUI metaProgressionRowUI in metaProgressionRowUIs)
        {
            metaProgressionRowUI.RefreshUI();
        }
    }

    private static void PlayerCharacter_Awake(On.PlayerCharacter.orig_Awake orig, PlayerCharacter self)
    {
        orig(self);
        Debug.Log("PlayerCharacter_Awake: Called.");
        GameObject inputGrabber = new GameObject("InputGrabber");
        inputGrabber.AddComponent<InputGrabber>();
    }

    public static bool MetaUiOpen()
    {
        if (!GM_Hub.isInHub)
        {
            Debug.Log("MetaUiOpen: Not in hub, returning false.");
            return false;
        }

        Assembly asm = typeof(GM_Hub).Assembly;
        Type MetaUiType = asm.GetType("Landfall.Haste.MetaProgressionUI");
        if (MetaUiType == null)
        {
            Debug.Log("MetaUiOpen: MetaProgressionUI type not found, returning false.");
            return false;
        }
        FieldInfo isOpenField = MetaUiType.GetField("IsOpen", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        bool isOpen = (bool)isOpenField.GetValue(MetaUiType);
        return isOpen;
    }

    public static void SetButtonsText(string text)
    {
        MetaProgressionRowUI[] metaProgressionRowUIs = GameObject.FindObjectsOfType<MetaProgressionRowUI>();
        foreach (MetaProgressionRowUI metaProgressionRowUI in metaProgressionRowUIs)
        {
            GameObject button = metaProgressionRowUI.transform.FindChildRecursive("Amount_1").gameObject;
            if (button != null)
            {
                TextMeshProUGUI textMeshPro = button.GetComponent<TextMeshProUGUI>();
                if (textMeshPro != null)
                {
                    textMeshPro.text = text;
                    metaProgressionRowUI.RefreshUI();
                }
            }
        }
    }

    public static void RefreshButtonsText()
    {
        string buttonText = subtractMode ? "-" : "+";
        SetButtonsText(buttonText);
    }
}