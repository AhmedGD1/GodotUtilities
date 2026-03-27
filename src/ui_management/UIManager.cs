using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Godot;

namespace Utilities.UIManagement;

public partial class UIManager : CanvasLayer
{
    [Signal] public delegate void PanelOpenedEventHandler(string id);
    [Signal] public delegate void PanelClosedEventHandler(string id);
    [Signal] public delegate void BackRequestedEventHandler(string current);

    private const string REGISTRY_PATH       = "uid://bxl4ii6mfgdew";
    private const float DIMMER_FADE_DURATION = 0.15f;

    private record PendingPanel(string Id, TransitionType Transition, bool IsPopup, object Payload);

    public static UIManager Instance { get; private set; }

    private Dictionary<string, PackedScene> registry;
    private readonly Dictionary<string, UIView> pool = new();

    private readonly Stack<UIView> panelStack  = new();
    private readonly Queue<PendingPanel> queue = new();

    private UIRegistry config;
    private ColorRect dimmer;

    private Color dimmerColor = new Color(0f, 0f, 0f, 0.392f);
    private bool isBusy;

    public override void _EnterTree() => Initialize();

    public override async void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsPressed())
            return;

        foreach (var action in config.goBackActions)
        {
            if (@event.IsActionReleased(action))
            {   
                if (panelStack.TryPeek(out var current))
                    EmitSignal(SignalName.BackRequested, current.Id);
                await GoBack();
                break;
            }
        }
    }

    #region Show

    public async Task ShowPanel(string id, TransitionType transition = TransitionType.Fade, bool isPopup = false, object payload = null)
    {
        if (isBusy) { queue.Enqueue(new PendingPanel(id, transition, isPopup, payload)); return; }
        isBusy = true;

        try
        {
            if (panelStack.Count > 0 && !isPopup)
                await ConcealPanel(panelStack.Peek(), transition);

            var panel = GetPanel(id);
            if (panel == null) return;

            panelStack.Push(panel);
            panel.IsPopup = isPopup;
            panel.Id      = id;

            if (isPopup)
                ToggleDimmer(true);

            await RevealPanel(panel, transition, payload);
            panel.defaultFocus?.GrabFocus();
        }
        finally { isBusy = false; }

        if (queue.Count > 0)
        {
            var pendingPanel = queue.Dequeue();
            await ShowPanel(pendingPanel.Id, pendingPanel.Transition, pendingPanel.IsPopup, pendingPanel.Payload);
        }
    }

    #endregion

    #region GoBack

    public async Task GoBack(TransitionType transition = TransitionType.Fade)
    {
        if (isBusy || panelStack.Count == 0) return;
        isBusy = true;

        try
        {
            var current = panelStack.Pop();
            await ConcealPanel(current, transition);

            if (panelStack.Count == 0) return;

            if (!current.IsPopup)
                await RevealPanel(panelStack.Peek(), transition);
            else
            {
                var beneath = panelStack.Peek();
                beneath.OnShow();
                ToggleDimmer(false);
            }
        }
        finally { isBusy = false; }
    }

    #endregion

    #region Clear

    public void ClearPool(bool clearActive = false)
    {
        foreach (var (id, panel) in pool.ToList())
        {
            if (!clearActive && panelStack.Contains(panel)) continue;
            panel.QueueFree();
            pool.Remove(id);
        }
    }

    #endregion

    #region Dimmer Toggle

    public void ToggleDimmer(bool show)
    {
        ToggleDimmer(show, DIMMER_FADE_DURATION);
    }

    public void ToggleDimmer(bool show, float duration)
    {
        dimmer.MouseFilter = show ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;

        Color init         = show ? Colors.Transparent : dimmerColor;
        Color final        = show ? dimmerColor        : Colors.Transparent;

        CreateTween().TweenColor(dimmer, final, duration).From(init);
    }

    #endregion

    #region Internal

    private UIView GetPanel(string id)
    {
        if (!registry.TryGetValue(id, out PackedScene value))
        {
            GD.PushError($"UIManager: ID '{id}' not found in registry!");
            return null;
        }

        if (pool.TryGetValue(id, out var cachedPanel))
            return cachedPanel;
        
        var panel = value.Instantiate<UIView>();
        AddChild(panel);

        pool[id] = panel;
        return panel;
    }

    private void Initialize()
    {
        Instance = this;
        config   = ResourceLoader.Load<UIRegistry>(REGISTRY_PATH);
        registry = config.panels.ToDictionary();

        InitDimmer();
    }

    private void InitDimmer()
    {
        dimmer = new ColorRect() { Color = Colors.Transparent };
        AddChild(dimmer);

        dimmer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        dimmer.MouseFilter = Control.MouseFilterEnum.Ignore;

        dimmer.GuiInput += evt =>
        {
            if (evt is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                _ = GoBack();  
        };
    }

    private async Task RevealPanel(UIView panel, TransitionType transition, object payload = null)
    {
        panel.OnInitialize(payload);
        panel.Show();
        await panel.ShowPanel(panel.PreferredTransition ?? transition);
        panel.OnShow();
        EmitSignal(SignalName.PanelOpened, panel.Id);
    }

    private async Task ConcealPanel(UIView panel, TransitionType transition)
    {
        panel.OnFinalize();
        await panel.HidePanel(panel.PreferredTransition ?? transition);
        panel.Hide();
        panel.OnHide();
        EmitSignal(SignalName.PanelClosed, panel.Id);
    }

    #endregion

}

