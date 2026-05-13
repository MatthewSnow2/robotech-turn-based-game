using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Robotech.TBS.Inputs;
using Robotech.TBS.Core;
using Robotech.TBS.Units;
using Robotech.TBS.Systems;
using Robotech.TBS.Cities;
using Robotech.TBS.Data;
using Robotech.TBS.Hex;
using Robotech.TBS.Bootstrap;

namespace Robotech.TBS.UI
{
    public class UIShell : MonoBehaviour
    {
        SelectionController selector;
        TurnManager turnManager;
        TechManager techManager;
        ResourceManager resourceManager;
        CityManager cityManager;
        HexGrid grid;
        Map.MapGenerator mapGen;
        GameBootstrap bootstrap;

        Text unitInfoText;
        Toggle attackToggle;
        Button endTurnButton;
        Button foundCityButton;
        Text researchText;

        Button buildArmoredBtn;
        Button buildSuperBtn;
        Button buildZentPodBtn;
        Button buildZentOfficerBtn;
        Text buildHintText;
        Text cityPanelText;
        Dropdown cityDropdown;
        Text cityDropdownLabel;

        void Awake()
        {
            selector = FindObjectOfType<SelectionController>();
            if (selector == null)
            {
                // Attach to this GameObject if missing
                selector = gameObject.AddComponent<SelectionController>();
            }
            turnManager = FindObjectOfType<TurnManager>();
            if (turnManager == null)
            {
                turnManager = gameObject.AddComponent<TurnManager>();
            }
            techManager = FindObjectOfType<TechManager>();
            resourceManager = FindObjectOfType<ResourceManager>();
            cityManager = FindObjectOfType<CityManager>();
            grid = FindObjectOfType<HexGrid>();
            mapGen = FindObjectOfType<Map.MapGenerator>();
            bootstrap = FindObjectOfType<GameBootstrap>();

            EnsureEventSystem();
            BuildCanvas();

            SelectionController.OnUnitSelected += OnUnitSelected;
            SelectionController.OnSelectionCleared += OnSelectionCleared;
        }

        void OnDestroy()
        {
            SelectionController.OnUnitSelected -= OnUnitSelected;
            SelectionController.OnSelectionCleared -= OnSelectionCleared;
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var ev = new GameObject("EventSystem");
                ev.AddComponent<EventSystem>();
                ev.AddComponent<StandaloneInputModule>();
            }
        }

        void BuildCanvas()
        {
            var canvasGO = new GameObject("UI");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel
            var panelGO = new GameObject("BottomPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panel = panelGO.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.5f);
            var rt = panelGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0, 120);

            // Unit info text
            var textGO = new GameObject("UnitInfo");
            textGO.transform.SetParent(panelGO.transform, false);
            unitInfoText = textGO.AddComponent<Text>();
            unitInfoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            unitInfoText.alignment = TextAnchor.MiddleLeft;
            unitInfoText.color = Color.white;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(0.6f, 1);
            textRT.offsetMin = new Vector2(10, 10);
            textRT.offsetMax = new Vector2(-10, -10);
            unitInfoText.text = "Select a unit";

            // Attack toggle
            var toggleGO = new GameObject("AttackToggle");
            toggleGO.transform.SetParent(panelGO.transform, false);
            attackToggle = toggleGO.AddComponent<Toggle>();
            var tgRT = toggleGO.GetComponent<RectTransform>();
            tgRT.anchorMin = new Vector2(0.62f, 0.15f);
            tgRT.anchorMax = new Vector2(0.7f, 0.85f);
            tgRT.offsetMin = Vector2.zero; tgRT.offsetMax = Vector2.zero;
            // background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(toggleGO.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.2f,0.2f,0.2f,0.9f);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0,0); bgRT.anchorMax = new Vector2(1,1);
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            // checkmark
            var ckGO = new GameObject("Checkmark");
            ckGO.transform.SetParent(bgGO.transform, false);
            var ckImg = ckGO.AddComponent<Image>();
            ckImg.color = Color.red;
            var ckRT = ckGO.GetComponent<RectTransform>();
            ckRT.anchorMin = new Vector2(0.2f,0.2f); ckRT.anchorMax = new Vector2(0.8f,0.8f);
            ckRT.offsetMin = Vector2.zero; ckRT.offsetMax = Vector2.zero;
            attackToggle.targetGraphic = bgImg;
            attackToggle.graphic = ckImg;

            // Label
            var lblGO = new GameObject("AttackLabel");
            lblGO.transform.SetParent(panelGO.transform, false);
            var lbl = lblGO.AddComponent<Text>();
            lbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = Color.white;
            lbl.text = "Attack Mode";
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0.71f, 0.15f);
            lblRT.anchorMax = new Vector2(0.83f, 0.85f);
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

            // Found City button
            var fcGO = new GameObject("FoundCityButton");
            fcGO.transform.SetParent(panelGO.transform, false);
            foundCityButton = fcGO.AddComponent<Button>();
            var fcImg = fcGO.AddComponent<Image>();
            fcImg.color = new Color(0.5f,0.4f,0.2f,0.95f);
            var fcRT = fcGO.GetComponent<RectTransform>();
            fcRT.anchorMin = new Vector2(0.7f, 0.15f);
            fcRT.anchorMax = new Vector2(0.83f, 0.85f);
            fcRT.offsetMin = Vector2.zero; fcRT.offsetMax = Vector2.zero;
            var fcLblGO = new GameObject("Text");
            fcLblGO.transform.SetParent(fcGO.transform, false);
            var fcLbl = fcLblGO.AddComponent<Text>();
            fcLbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            fcLbl.alignment = TextAnchor.MiddleCenter;
            fcLbl.color = Color.white;
            fcLbl.text = "Found City";
            var fcLblRT = fcLblGO.GetComponent<RectTransform>();
            fcLblRT.anchorMin = new Vector2(0,0); fcLblRT.anchorMax = new Vector2(1,1);
            fcLblRT.offsetMin = Vector2.zero; fcLblRT.offsetMax = Vector2.zero;

            // Build buttons container
            var buildPanel = new GameObject("BuildPanel");
            buildPanel.transform.SetParent(panelGO.transform, false);
            var bpRT = buildPanel.AddComponent<RectTransform>();
            bpRT.anchorMin = new Vector2(0.35f, 0.15f);
            bpRT.anchorMax = new Vector2(0.68f, 0.85f);
            bpRT.offsetMin = Vector2.zero; bpRT.offsetMax = Vector2.zero;

            // Hint text under build panel
            var hintGO = new GameObject("BuildHint");
            hintGO.transform.SetParent(panelGO.transform, false);
            buildHintText = hintGO.AddComponent<Text>();
            buildHintText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buildHintText.alignment = TextAnchor.UpperCenter;
            buildHintText.color = new Color(1f,1f,1f,0.85f);
            buildHintText.text = "";
            var hintRT = hintGO.GetComponent<RectTransform>();
            hintRT.anchorMin = new Vector2(0.35f, 0.05f);
            hintRT.anchorMax = new Vector2(0.68f, 0.14f);
            hintRT.offsetMin = Vector2.zero; hintRT.offsetMax = Vector2.zero;

            // City selection dropdown (above build panel)
            var ddLblGO = new GameObject("CitySelectLabel");
            ddLblGO.transform.SetParent(panelGO.transform, false);
            cityDropdownLabel = ddLblGO.AddComponent<Text>();
            cityDropdownLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            cityDropdownLabel.alignment = TextAnchor.MiddleCenter;
            cityDropdownLabel.color = Color.white;
            cityDropdownLabel.text = "Build in City:";
            var ddLblRT = ddLblGO.GetComponent<RectTransform>();
            ddLblRT.anchorMin = new Vector2(0.35f, 0.86f);
            ddLblRT.anchorMax = new Vector2(0.48f, 0.94f);
            ddLblRT.offsetMin = Vector2.zero; ddLblRT.offsetMax = Vector2.zero;

            var ddGO = new GameObject("CitySelect");
            ddGO.transform.SetParent(panelGO.transform, false);
            var ddImg = ddGO.AddComponent<Image>();
            ddImg.color = new Color(0.2f,0.2f,0.2f,0.8f);
            cityDropdown = ddGO.AddComponent<Dropdown>();
            cityDropdown.captionText = CreateInnerText(ddGO.transform, "Select City");
            cityDropdown.template = CreateDropdownTemplate(ddGO.transform);
            var ddRT = ddGO.GetComponent<RectTransform>();
            ddRT.anchorMin = new Vector2(0.5f, 0.86f);
            ddRT.anchorMax = new Vector2(0.68f, 0.94f);
            ddRT.offsetMin = Vector2.zero; ddRT.offsetMax = Vector2.zero;

            // Build Armored
            var baGO = new GameObject("BuildArmored");
            baGO.transform.SetParent(buildPanel.transform, false);
            buildArmoredBtn = baGO.AddComponent<Button>();
            var baImg = baGO.AddComponent<Image>();
            baImg.color = new Color(0.2f,0.35f,0.55f,0.9f);
            var baRT = baGO.GetComponent<RectTransform>();
            baRT.anchorMin = new Vector2(0,0);
            baRT.anchorMax = new Vector2(0.24f,1);
            baRT.offsetMin = Vector2.zero; baRT.offsetMax = Vector2.zero;
            var baLblGO = new GameObject("Text");
            baLblGO.transform.SetParent(baGO.transform, false);
            var baLbl = baLblGO.AddComponent<Text>();
            baLbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            baLbl.alignment = TextAnchor.MiddleCenter;
            baLbl.color = Color.white;
            baLbl.text = "Build Armored";
            var baLblRT = baLblGO.GetComponent<RectTransform>();
            baLblRT.anchorMin = new Vector2(0,0); baLblRT.anchorMax = new Vector2(1,1);
            baLblRT.offsetMin = Vector2.zero; baLblRT.offsetMax = Vector2.zero;

            // Build Super
            var bsGO = new GameObject("BuildSuper");
            bsGO.transform.SetParent(buildPanel.transform, false);
            buildSuperBtn = bsGO.AddComponent<Button>();
            var bsImg = bsGO.AddComponent<Image>();
            bsImg.color = new Color(0.35f,0.2f,0.55f,0.9f);
            var bsRT = bsGO.GetComponent<RectTransform>();
            bsRT.anchorMin = new Vector2(0.26f,0);
            bsRT.anchorMax = new Vector2(0.5f,1);
            bsRT.offsetMin = Vector2.zero; bsRT.offsetMax = Vector2.zero;
            var bsLblGO = new GameObject("Text");
            bsLblGO.transform.SetParent(bsGO.transform, false);
            var bsLbl = bsLblGO.AddComponent<Text>();
            bsLbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            bsLbl.alignment = TextAnchor.MiddleCenter;
            bsLbl.color = Color.white;
            bsLbl.text = "Build Super";
            var bsLblRT = bsLblGO.GetComponent<RectTransform>();
            bsLblRT.anchorMin = new Vector2(0,0); bsLblRT.anchorMax = new Vector2(1,1);
            bsLblRT.offsetMin = Vector2.zero; bsLblRT.offsetMax = Vector2.zero;

            // Build Zent Tactical Pod
            var zpGO = new GameObject("BuildZentPod");
            zpGO.transform.SetParent(buildPanel.transform, false);
            buildZentPodBtn = zpGO.AddComponent<Button>();
            var zpImg = zpGO.AddComponent<Image>();
            zpImg.color = new Color(0.55f,0.25f,0.25f,0.9f);
            var zpRT = zpGO.GetComponent<RectTransform>();
            zpRT.anchorMin = new Vector2(0.52f,0);
            zpRT.anchorMax = new Vector2(0.76f,1);
            zpRT.offsetMin = Vector2.zero; zpRT.offsetMax = Vector2.zero;
            var zpLblGO = new GameObject("Text");
            zpLblGO.transform.SetParent(zpGO.transform, false);
            var zpLbl = zpLblGO.AddComponent<Text>();
            zpLbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            zpLbl.alignment = TextAnchor.MiddleCenter;
            zpLbl.color = Color.white;
            zpLbl.text = "Build Pod";
            var zpLblRT = zpLblGO.GetComponent<RectTransform>();
            zpLblRT.anchorMin = new Vector2(0,0); zpLblRT.anchorMax = new Vector2(1,1);
            zpLblRT.offsetMin = Vector2.zero; zpLblRT.offsetMax = Vector2.zero;

            // Build Zent Officer Pod
            var zoGO = new GameObject("BuildZentOfficer");
            zoGO.transform.SetParent(buildPanel.transform, false);
            buildZentOfficerBtn = zoGO.AddComponent<Button>();
            var zoImg = zoGO.AddComponent<Image>();
            zoImg.color = new Color(0.6f,0.2f,0.2f,0.9f);
            var zoRT = zoGO.GetComponent<RectTransform>();
            zoRT.anchorMin = new Vector2(0.78f,0);
            zoRT.anchorMax = new Vector2(1f,1);
            zoRT.offsetMin = Vector2.zero; zoRT.offsetMax = Vector2.zero;
            var zoLblGO = new GameObject("Text");
            zoLblGO.transform.SetParent(zoGO.transform, false);
            var zoLbl = zoLblGO.AddComponent<Text>();
            zoLbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            zoLbl.alignment = TextAnchor.MiddleCenter;
            zoLbl.color = Color.white;
            zoLbl.text = "Build Officer";
            var zoLblRT = zoLblGO.GetComponent<RectTransform>();
            zoLblRT.anchorMin = new Vector2(0,0); zoLblRT.anchorMax = new Vector2(1,1);
            zoLblRT.offsetMin = Vector2.zero; zoLblRT.offsetMax = Vector2.zero;

            // End Turn button
            var btnGO = new GameObject("EndTurnButton");
            btnGO.transform.SetParent(panelGO.transform, false);
            endTurnButton = btnGO.AddComponent<Button>();
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.25f,0.5f,0.25f,0.95f);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.85f, 0.15f);
            btnRT.anchorMax = new Vector2(0.98f, 0.85f);
            btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
            // Button label
            var btnLblGO = new GameObject("Text");
            btnLblGO.transform.SetParent(btnGO.transform, false);
            var btnLbl = btnLblGO.AddComponent<Text>();
            btnLbl.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnLbl.alignment = TextAnchor.MiddleCenter;
            btnLbl.color = Color.white;
            btnLbl.text = "End Turn";
            var btnLblRT = btnLblGO.GetComponent<RectTransform>();
            btnLblRT.anchorMin = new Vector2(0,0); btnLblRT.anchorMax = new Vector2(1,1);
            btnLblRT.offsetMin = Vector2.zero; btnLblRT.offsetMax = Vector2.zero;

            // Research text (top-left)
            var rGO = new GameObject("ResearchText");
            rGO.transform.SetParent(canvasGO.transform, false);
            researchText = rGO.AddComponent<Text>();
            researchText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            researchText.alignment = TextAnchor.UpperLeft;
            researchText.color = Color.white;
            researchText.text = "Research: None";
            var resRT = rGO.GetComponent<RectTransform>();
            resRT.anchorMin = new Vector2(0.01f, 0.85f);
            resRT.anchorMax = new Vector2(0.33f, 0.98f);
            resRT.offsetMin = Vector2.zero; resRT.offsetMax = Vector2.zero;

            // City panel (top-left under research)
            var cpGO = new GameObject("CityPanelText");
            cpGO.transform.SetParent(canvasGO.transform, false);
            cityPanelText = cpGO.AddComponent<Text>();
            cityPanelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            cityPanelText.alignment = TextAnchor.UpperLeft;
            cityPanelText.color = new Color(0.9f,0.95f,1f,0.95f);
            cityPanelText.text = "Cities: none";
            var cpRT = cpGO.GetComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.01f, 0.65f);
            cpRT.anchorMax = new Vector2(0.33f, 0.83f);
            cpRT.offsetMin = Vector2.zero; cpRT.offsetMax = Vector2.zero;

            // Wire events
            attackToggle.onValueChanged.AddListener(OnAttackToggle);
            endTurnButton.onClick.AddListener(() => turnManager.EndPhase());
            foundCityButton.onClick.AddListener(() => { if (selector != null) selector.TryFoundCity(); });
            buildArmoredBtn.onClick.AddListener(() => QueueCityBuild(bootstrap?.rdfArmoredVeritech));
            buildSuperBtn.onClick.AddListener(() => QueueCityBuild(bootstrap?.rdfSuperVeritech));
            buildZentPodBtn.onClick.AddListener(() => QueueCityBuild(bootstrap?.zentTacticalPod));
            buildZentOfficerBtn.onClick.AddListener(() => QueueCityBuild(bootstrap?.zentOfficerPod));

            AttachTooltip(buildArmoredBtn, bootstrap?.rdfArmoredVeritech);
            AttachTooltip(buildSuperBtn, bootstrap?.rdfSuperVeritech);
            AttachTooltip(buildZentPodBtn, bootstrap?.zentTacticalPod);
            AttachTooltip(buildZentOfficerBtn, bootstrap?.zentOfficerPod);
        }

        void OnAttackToggle(bool on)
        {
            if (selector != null)
                selector.SetAttackMode(on);
        }

        void OnUnitSelected(Unit u)
        {
            string baseLine = $"{u.definition.displayName} | HP {u.currentHP}/{u.definition.maxHP} | MV {u.movesLeft}";
            string abilityLine = "";
            if (u.definition.canOverwatch)
            {
                abilityLine = u.isOverwatching ? "  [OVERWATCH ACTIVE]" : "  [O] Overwatch";
            }
            unitInfoText.text = baseLine + abilityLine;
        }

        void OnSelectionCleared()
        {
            unitInfoText.text = "Select a unit";
            if (attackToggle != null) attackToggle.isOn = false;
        }

        void Update()
        {
            // Keep unit info line in sync with mutable state (movement spent, overwatch toggled).
            if (selector != null && selector.SelectedUnit != null && unitInfoText != null)
            {
                OnUnitSelected(selector.SelectedUnit);
            }

            // Enable/disable Found City based on selection
            if (foundCityButton != null && selector != null)
            {
                bool can = selector.CanFoundCityHere();
                foundCityButton.interactable = can;
            }
            // Update research label
            if (researchText != null && techManager != null)
            {
                var tech = techManager.currentResearch;
                if (tech != null)
                {
                    researchText.text = $"Research: {tech.displayName} {techManager.scienceProgress}/{tech.costScience}";
                }
                else
                {
                    researchText.text = "Research: None";
                }
            }

            // Enable/disable build buttons based on tech unlock and city availability
            if (buildArmoredBtn != null)
                buildArmoredBtn.interactable = techManager != null && techManager.IsUnitUnlocked(bootstrap?.rdfArmoredVeritech) && HasCity(Faction.RDF);
            if (buildSuperBtn != null)
                buildSuperBtn.interactable = techManager != null && techManager.IsUnitUnlocked(bootstrap?.rdfSuperVeritech) && HasCity(Faction.RDF);
            if (buildZentPodBtn != null)
                buildZentPodBtn.interactable = HasCity(Faction.Zentradi);
            if (buildZentOfficerBtn != null)
                buildZentOfficerBtn.interactable = HasCity(Faction.Zentradi);

            // Build hint string
            if (buildHintText != null)
            {
                string hint = "";
                if (!HasCity(Faction.RDF))
                {
                    hint += "RDF: Found a city to build advanced units.\n";
                }
                else
                {
                    if (techManager != null && !techManager.IsUnitUnlocked(bootstrap?.rdfArmoredVeritech)) hint += "RDF: Unlock Armored Veritech tech.\n";
                    if (techManager != null && !techManager.IsUnitUnlocked(bootstrap?.rdfSuperVeritech)) hint += "RDF: Unlock Super Veritech tech.\n";
                }
                if (!HasCity(Faction.Zentradi))
                {
                    hint += "Zentradi: Found a city to build pods.\n";
                }
                buildHintText.text = hint.TrimEnd();
            }

            // City panel showing queues and progress
            if (cityPanelText != null)
            {
                cityPanelText.text = GetCityPanelSummary();
            }

            // Keep city dropdown populated
            PopulateCityDropdown();
        }

        void QueueCityBuild(UnitDefinition def)
        {
            if (def == null || cityManager == null) return;
            var city = GetSelectedCity(def.faction) ?? GetFirstCity(def.faction);
            if (city == null) return;
            city.EnqueueUnit(def);
            Debug.Log($"Enqueued {def.displayName} in {city.cityName}");
        }

        bool HasCity(Faction f)
        {
            if (cityManager == null) return false;
            foreach (var c in cityManager.Cities)
                if (c != null && c.faction == f) return true;
            return false;
        }

        City GetFirstCity(Faction f)
        {
            if (cityManager == null) return null;
            foreach (var c in cityManager.Cities)
                if (c != null && c.faction == f) return c;
            return null;
        }

        bool IsPassable(UnitDefinition def, HexCoord c)
        {
            var t = mapGen.GetTerrain(c);
            return MapRules.IsPassable(def, t);
        }

        bool IsTileFree(HexCoord c)
        {
            // Use UnitRegistry for O(1) lookup instead of FindObjectsOfType
            if (UnitRegistry.Instance != null)
            {
                return !UnitRegistry.Instance.IsOccupied(c);
            }
            return true;
        }

        string GetCityPanelSummary()
        {
            if (cityManager == null) return "Cities: none";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var c in cityManager.Cities)
            {
                if (c == null) continue;
                var yields = c.GetYields();
                int prodPerTurn = 2 + yields.prod; // base 2 matches CityManager
                if (c.CurrentItem != null)
                {
                    int remaining = Mathf.Max(0, c.CurrentItem.Cost - c.CurrentItem.progress);
                    int turns = prodPerTurn > 0 ? Mathf.CeilToInt(remaining / (float)prodPerTurn) : -1;
                    sb.AppendLine($"{c.cityName} ({c.faction}) prod:{prodPerTurn} -> {c.CurrentItem.unit.displayName} ({remaining} left, {turns} turns)");
                }
                else
                {
                    sb.AppendLine($"{c.cityName} ({c.faction}) prod:{prodPerTurn} -> Idle");
                }
            }
            var text = sb.ToString();
            return string.IsNullOrEmpty(text) ? "Cities: none" : text;
        }

        void PopulateCityDropdown()
        {
            if (cityDropdown == null || cityManager == null) return;
            var opts = cityDropdown.options;
            bool needsRebuild = false;
            if (opts.Count != cityManager.Cities.Count)
            {
                needsRebuild = true;
            }
            if (!needsRebuild) return;
            cityDropdown.ClearOptions();
            foreach (var c in cityManager.Cities)
            {
                if (c == null) continue;
                cityDropdown.options.Add(new Dropdown.OptionData($"{c.faction}: {c.cityName}"));
            }
            cityDropdown.RefreshShownValue();
        }

        City GetSelectedCity(Faction forFaction)
        {
            if (cityDropdown == null || cityManager == null) return null;
            var sel = cityDropdown.captionText != null ? cityDropdown.captionText.text : null;
            if (string.IsNullOrEmpty(sel)) return null;
            // Expect format "Faction: Name"
            foreach (var c in cityManager.Cities)
            {
                if (c == null) continue;
                var label = $"{c.faction}: {c.cityName}";
                if (label == sel && c.faction == forFaction) return c;
            }
            return null;
        }

        void AttachTooltip(Button btn, UnitDefinition def)
        {
            if (btn == null || def == null) return;
            var et = btn.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (buildHintText != null) buildHintText.text = GetBuildTooltip(def); });
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (buildHintText != null) buildHintText.text = GetDefaultHint(); });
            et.triggers.Add(enter);
            et.triggers.Add(exit);
        }

        string GetDefaultHint()
        {
            string hint = "";
            if (!HasCity(Faction.RDF)) hint += "RDF: Found a city to build advanced units.\n";
            else {
                if (techManager != null && !techManager.IsUnitUnlocked(bootstrap?.rdfArmoredVeritech)) hint += "RDF: Unlock Armored Veritech tech.\n";
                if (techManager != null && !techManager.IsUnitUnlocked(bootstrap?.rdfSuperVeritech)) hint += "RDF: Unlock Super Veritech tech.\n";
            }
            if (!HasCity(Faction.Zentradi)) hint += "Zentradi: Found a city to build pods.\n";
            return hint.TrimEnd();
        }

        string GetBuildTooltip(UnitDefinition def)
        {
            if (def == null) return "";
            var city = GetSelectedCity(def.faction) ?? GetFirstCity(def.faction);
            int prodPerTurn = 0;
            if (city != null)
            {
                var y = city.GetYields();
                prodPerTurn = 2 + y.prod;
            }
            int turns = prodPerTurn > 0 ? Mathf.CeilToInt(def.buildCostMaterials / (float)prodPerTurn) : -1;
            string req = "";
            if (def.faction == Faction.RDF)
            {
                if (def == bootstrap?.rdfArmoredVeritech && techManager != null && !techManager.IsUnitUnlocked(bootstrap.rdfArmoredVeritech)) req = "Requires Armored Veritech tech.";
                if (def == bootstrap?.rdfSuperVeritech && techManager != null && !techManager.IsUnitUnlocked(bootstrap.rdfSuperVeritech)) req = "Requires Super Veritech tech.";
                if (!HasCity(Faction.RDF)) req = "Requires an RDF city.";
            }
            else
            {
                if (!HasCity(Faction.Zentradi)) req = "Requires a Zentradi city.";
            }
            string eta = turns >= 0 ? $"ETA: {turns} turns" : "ETA: N/A";
            return $"Cost: {def.buildCostMaterials} production. {eta} {(string.IsNullOrEmpty(req)?"":"\n"+req)}";
        }

        // Helpers to construct a minimal Dropdown template programmatically
        Text CreateInnerText(Transform parent, string placeholder)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = placeholder;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0);
            rt.anchorMax = new Vector2(0.95f, 1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return txt;
        }

        RectTransform CreateDropdownTemplate(Transform parent)
        {
            // Template root
            var templateGO = new GameObject("Template");
            templateGO.SetActive(false);
            templateGO.transform.SetParent(parent, false);
            var templateRT = templateGO.AddComponent<RectTransform>();
            templateRT.pivot = new Vector2(0.5f, 1f);
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0.6f);
            var img = templateGO.AddComponent<Image>();
            img.color = new Color(0,0,0,0.8f);
            templateGO.AddComponent<ScrollRect>();

            // Viewport
            var vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(templateGO.transform, false);
            var vpRT = vpGO.AddComponent<RectTransform>();
            vpRT.anchorMin = new Vector2(0,0);
            vpRT.anchorMax = new Vector2(1,1);
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
            var maskImg = vpGO.AddComponent<Image>();
            maskImg.color = new Color(0,0,0,0.2f);
            var mask = vpGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content with Toggle items
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(vpGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0,1);
            contentRT.anchorMax = new Vector2(1,1);
            contentRT.pivot = new Vector2(0.5f,1);
            contentRT.offsetMin = Vector2.zero; contentRT.offsetMax = Vector2.zero;

            var scroll = templateGO.GetComponent<ScrollRect>();
            scroll.viewport = vpRT;
            scroll.content = contentRT;

            // Item prefab
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);
            var toggle = itemGO.AddComponent<Toggle>();
            var itemRT = itemGO.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0,1);
            itemRT.anchorMax = new Vector2(1,1);
            itemRT.pivot = new Vector2(0.5f,1);
            itemRT.sizeDelta = new Vector2(0, 24);

            var bg = new GameObject("Item Background");
            bg.transform.SetParent(itemGO.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f,0.1f,0.1f,0.8f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0,0);
            bgRT.anchorMax = new Vector2(1,1);
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            var check = new GameObject("Item Checkmark");
            check.transform.SetParent(bg.transform, false);
            var ckImg = check.AddComponent<Image>();
            ckImg.color = new Color(0.8f,0.8f,0.2f,1);
            var ckRT = check.GetComponent<RectTransform>();
            ckRT.anchorMin = new Vector2(0,0);
            ckRT.anchorMax = new Vector2(0,1);
            ckRT.sizeDelta = new Vector2(20, 0);

            var label = new GameObject("Item Label");
            label.transform.SetParent(bg.transform, false);
            var labelTxt = label.AddComponent<Text>();
            labelTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelTxt.color = Color.white;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0,0);
            labelRT.anchorMax = new Vector2(1,1);
            labelRT.offsetMin = new Vector2(24,0); labelRT.offsetMax = Vector2.zero;

            toggle.targetGraphic = bgImg;
            toggle.graphic = ckImg;

            // Hook into Dropdown expected references
            var dd = parent.GetComponent<Dropdown>();
            if (dd != null)
            {
                dd.itemText = labelTxt;
            }
            return templateRT;
        }
    }
}
