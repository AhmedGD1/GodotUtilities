using System.Threading.Tasks;
using Godot;

namespace Utilities.UIManagement;

[GlobalClass]
public abstract partial class UIView : Control
{
    [Export] public Control defaultFocus;

    public bool IsPopup { get; set; }
    public string Id    { get; set; }

    private TransitionType? preferredTransition = null;

    public TransitionType? PreferredTransition => preferredTransition;

    public virtual void OnInitialize(object payload)  { }
    public virtual void OnShow()                      { }
    public virtual void OnHide()                      { }
    public virtual void OnFinalize()                  { }

    public virtual async Task ShowPanel(TransitionType transition)
    {
        await UITransition.Play(this, true, transition).AwaitAsync();
    }

    public virtual async Task HidePanel(TransitionType transition)
    {
        await UITransition.Play(this, false, transition).AwaitAsync();
    }

    public void SetPreferredTransition(TransitionType transition)
    {
        preferredTransition = transition;
    }
        
}