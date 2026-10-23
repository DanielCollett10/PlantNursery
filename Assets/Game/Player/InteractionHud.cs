using System.Collections.Generic;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Time;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PlantNursery.Game
{
    /// <summary>
    /// UI Toolkit HUD (Resources/UI/Hud.uxml + Hud.uss): day, season, clock, wallet, held item, interaction prompt,
    /// rejection message, neighbour-gift toast and the computer shop screen. Reads sim state only; Buy sends
    /// <see cref="BuyItem"/>. Text comes from <see cref="UiStrings"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InteractionHud : MonoBehaviour
    {
        private const float ToastSeconds = 4f;

        [SerializeField] private InteractionController interaction;

        private SimHost _host;
        private bool _subscribed;
        private Label _day, _season, _clock, _wallet, _held, _prompt, _rejection, _toast;
        private VisualElement _shopOverlay;
        private Label _shopWallet, _shopStatus;
        private ScrollView _shopList;
        private float _toastUntil;
        private bool _shopOpen;
        private InputAction _cancel;
        private FirstPersonController _player;

        /// <summary>True while the shop screen is open.</summary>
        public bool IsShopOpen => _shopOpen;

        /// <summary>Assign the controller (used by the scene generator).</summary>
        public void Configure(InteractionController controller) { interaction = controller; }

        private void OnEnable()
        {
            _cancel = new InputAction("Cancel", InputActionType.Button);
            _cancel.AddBinding("<Keyboard>/escape");
            _cancel.AddBinding("<Gamepad>/buttonEast");
            _cancel.Enable();
            if (interaction != null) interaction.ShopRequested += OpenShop;
            // The UI tree is built in Start, not here: UIDocument resets its root visual element after
            // our OnEnable runs, which wiped the tree and left every label null/blank (T-052).
        }

        private void Start() { BuildUi(); }

        private void BuildUi()
        {
            var doc = GetComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                Debug.LogError("[PlantNursery] InteractionHud: UIDocument has no PanelSettings. Assign Assets/Game/UI/GamePanelSettings.asset (run Plant Nursery/Rebuild Greybox World).", this);
                return;
            }
            var root = doc.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.pickingMode = PickingMode.Ignore;
            var tree = Resources.Load<VisualTreeAsset>("UI/Hud");
            var sheet = Resources.Load<StyleSheet>("UI/HudStyle");
            if (tree == null) { Debug.LogError("Resources/UI/Hud.uxml missing"); return; }
            if (sheet != null) root.styleSheets.Add(sheet);
            tree.CloneTree(root);
            // Make every wrapper (TemplateContainer) fill the screen so right/bottom anchors resolve on-screen.
            foreach (var child in root.Children())
            {
                child.style.flexGrow = 1;
                child.style.width = Length.Percent(100);
                child.style.height = Length.Percent(100);
            }

            _day = root.Q<Label>("day-label");
            _season = root.Q<Label>("season-label");
            _clock = root.Q<Label>("clock-label");
            _wallet = root.Q<Label>("wallet-label");
            _held = root.Q<Label>("held-label");
            _prompt = root.Q<Label>("prompt-label");
            _rejection = root.Q<Label>("rejection-label");
            _toast = root.Q<Label>("toast-label");
            _shopOverlay = root.Q("shop-overlay");
            _shopWallet = root.Q<Label>("shop-wallet");
            _shopStatus = root.Q<Label>("shop-status");
            _shopList = root.Q<ScrollView>("shop-list");
            root.Q<Label>("shop-title").text = UiStrings.Get("shop.title");
            var close = root.Q<Button>("shop-close");
            close.text = UiStrings.Get("shop.close");
            close.clicked += CloseShop;
        }

        private void OnDisable()
        {
            if (interaction != null) interaction.ShopRequested -= OpenShop;
            if (_subscribed && _host != null) _host.EventDispatched -= OnEvent;
            _subscribed = false;
            _cancel?.Disable();
            _cancel?.Dispose();
            _cancel = null;
            if (_shopOpen) CloseShop();
        }

        private void Update()
        {
            if (_prompt == null || _prompt.panel == null) { BuildUi(); if (_prompt == null) return; }
            if (_host == null) _host = FindAnyObjectByType<SimHost>();
            if (_host == null || !_host.IsReady) return;
            if (!_subscribed) { _host.EventDispatched += OnEvent; _subscribed = true; }

            var clock = _host.Clock;
            _day.text = UiStrings.Get("hud.day", clock.Day);
            _season.text = UiStrings.Get("season." + clock.Season);
            var m = clock.MinuteOfDay;
            _clock.text = (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
            var wallet = UiStrings.Get("hud.wallet", _host.Wallet.Balance);
            _wallet.text = wallet;

            if (interaction != null)
            {
                _prompt.text = interaction.Prompt;
                _rejection.text = interaction.Rejection;
                var held = interaction.HeldItemId;
                _held.text = string.IsNullOrEmpty(held) ? string.Empty : UiStrings.Get("hud.held", ItemName(held));
            }

            _toast.EnableInClassList("hud-toast--visible", Time.unscaledTime < _toastUntil);

            if (_shopOpen)
            {
                _shopWallet.text = wallet;
                if (_cancel != null && _cancel.WasPressedThisFrame()) CloseShop();
            }
        }

        private string ItemName(string id) =>
            _host.Content.ItemDefs.TryGetValue(id, out var def) ? def.Name : id;

        private void OnEvent(ISimEvent evt)
        {
            if (evt is NeighbourGift)
            {
                _toast.text = UiStrings.Get("toast.neighbourGift");
                _toastUntil = Time.unscaledTime + ToastSeconds;
            }
        }

        /// <summary>Open the shop screen and free the cursor.</summary>
        public void OpenShop()
        {
            if (_shopOpen || _host == null || !_host.IsReady) return;
            _shopOpen = true;
            BuildShopList();
            _shopStatus.text = string.Empty;
            _shopOverlay.AddToClassList("shop-overlay--open");
            _player = FindAnyObjectByType<FirstPersonController>();
            if (_player != null) _player.enabled = false; // its OnDisable frees the cursor and stops look/move
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        /// <summary>Close the shop screen and give control back to the player.</summary>
        public void CloseShop()
        {
            if (!_shopOpen) return;
            _shopOpen = false;
            _shopOverlay?.RemoveFromClassList("shop-overlay--open");
            if (_player != null) _player.enabled = true; // relocks the cursor
            _player = null;
        }

        private void BuildShopList()
        {
            _shopList.Clear();
            var items = new List<PlantNursery.Sim.Content.ItemDef>();
            foreach (var def in _host.Content.ItemDefs.Values) if (def.Price > 0) items.Add(def);
            items.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (var def in items)
            {
                var row = new VisualElement();
                row.AddToClassList("shop-row");
                var name = new Label(def.Name);
                name.AddToClassList("shop-row-name");
                var price = new Label(UiStrings.Get("shop.price", def.Price));
                price.AddToClassList("shop-row-price");
                var buy = new Button { text = UiStrings.Get("shop.buy") };
                buy.AddToClassList("shop-row-buy");
                var id = def.Id; var display = def.Name;
                buy.clicked += () => Buy(id, display);
                row.Add(name); row.Add(price); row.Add(buy);
                _shopList.Add(row);
            }
        }

        private void Buy(string itemId, string display)
        {
            var result = _host.Submit(new BuyItem(SimHost.LocalPlayer, itemId, 1));
            if (result.IsAccepted)
            {
                _shopStatus.text = UiStrings.Get("shop.bought", display);
                _shopStatus.RemoveFromClassList("shop-status--error");
                return;
            }
            var key = string.IsNullOrEmpty(result.Message) ? result.Reason.ToString() : result.Message;
            _shopStatus.text = UiStrings.Has("reject." + key) ? UiStrings.Get("reject." + key) : key;
            _shopStatus.AddToClassList("shop-status--error");
        }
    }
}
