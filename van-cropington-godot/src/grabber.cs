using Godot;
using System;

public class grabber : Panel
{

    [Export] public int Sector = 0;
    Viewer v;

    public override void _Ready() {
        Connect("gui_input", this, nameof(OnGuiInput));
        v = GetParent<Viewer>();

        Connect("mouse_entered", this, nameof(MouseEntered));
        Connect("mouse_exited", this, nameof(MouseExited));
    }

    void MouseEntered() {
        (Material as ShaderMaterial).SetShaderParam("glow", 1.0);
    }

    void MouseExited() {
        (Material as ShaderMaterial).SetShaderParam("glow", 0.0);
    }

    bool grabbing = false;
    Vector2 grabOffset = new Vector2();

    public void OnGuiInput(InputEvent e) {

        //Drag Viewer
        if (e is InputEventMouseButton) {
            if ((e as InputEventMouseButton).ButtonIndex == 1) {
                grabbing = (e as InputEventMouseButton).Pressed;
                if (grabbing) {
                    grabOffset = RectGlobalPosition - GetGlobalMousePosition();
                }
            }
        }
    }

    public override void _Process(float delta) {
        if (Input.IsMouseButtonPressed(1) && grabbing) {
             v.MoveCorner(this, grabOffset  );
        }
    }

}
