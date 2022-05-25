using Godot;
using System;
using ImageMagick;

public class Viewer : TextureRect
{
    public Vector2 corner_tl = new Vector2();
    public Vector2 corner_br = new Vector2();

    Control[] grabbers = new Control[8];
    
    public override void _Ready()
    {
        for (int i = 0; i < grabbers.Length; i++) {
            grabbers[i] = GetChild(i) as Control;
        }

        OnResized();
        GetTree().Root.Connect("size_changed", this, nameof(OnResized));
        UpdateCorners();
    }

    float aspectTexture;
    float aspectViewport;

    Vector2 actualTexSize;
    Vector2 corner;

    public CropOption cropOpt = new CropOption(0,0,1,1);
    
    Image img = new Image();
    ImageTexture tex = new ImageTexture();
    public void LoadImage(string path, CropOption options) {
        Error e = img.Load(path);
        
        // Godot for some Reason can't import certain jpg files.
        // Stupid, but now I have to re-save them first with ImageMagick before using them in Godot. Why do I have to do this?
        if (e == Godot.Error.FileCorrupt) {
            GD.Print("Corrupt File - init workaround (Saving and reloading from temp)");
            using (MagickImage magickImg = new MagickImage(path)) {
                magickImg.Format = MagickFormat.Jpg;
                magickImg.Write(System.IO.Path.GetTempPath() + "fix_corrupt_file.jpg");
                img.Load(System.IO.Path.GetTempPath() + "fix_corrupt_file.jpg");
                System.IO.File.Delete(System.IO.Path.GetTempPath() + "fix_corrupt_file.jpg");
            }
        }

        tex.CreateFromImage(img);
        
        Texture = tex;
        cropOpt = options;
        OnResized();
        UpdateShader();
    }

    public void Reset() {
        img.Load("res://drag&drop.png");
        tex.CreateFromImage(img);
        Texture = tex;
        OnResized();
        UpdateShader();
    }

    public void OnResized() {

        aspectTexture = Texture.GetWidth() / (float)Texture.GetHeight();
        aspectViewport = RectSize.x / RectSize.y;

        if (aspectTexture > aspectViewport) {
            actualTexSize = new Vector2(RectSize.x, RectSize.x * (1f / aspectTexture));
            float dist = (RectSize.y - actualTexSize.y) / 2f; 
            corner = new Vector2(RectGlobalPosition.x, RectGlobalPosition.y + dist);

        } else {
            actualTexSize = new Vector2(RectSize.y * aspectTexture, RectSize.y);
            float dist = (RectSize.x - actualTexSize.x) / 2f; 
            corner = new Vector2(RectGlobalPosition.x + dist, RectGlobalPosition.y);
        }

        GetNode<Control>("grabber_tl").RectGlobalPosition = UVToGlobalPos(new Vector2(0.5f, 0.5f));
        UpdateCorners();
    }

    bool grabbing = false;

    //kinda ugly..
    Vector2 viewerGrabOffsetLT = new Vector2();
    Vector2 viewerGrabOffsetRT = new Vector2();
    Vector2 viewerGrabOffsetLB = new Vector2();
    Vector2 viewerGrabOffsetRB = new Vector2();

    public void OnInput(InputEvent e) {
        //Drag Viewer
        if (e is InputEventMouseButton) {
            if ((e as InputEventMouseButton).ButtonIndex == 1) {
                grabbing = (e as InputEventMouseButton).Pressed;
                if (grabbing) { 
                    viewerGrabOffsetLT = grabbers[4].RectGlobalPosition - GetGlobalMousePosition();
                    viewerGrabOffsetRT = grabbers[5].RectGlobalPosition - GetGlobalMousePosition();
                    viewerGrabOffsetLB = grabbers[6].RectGlobalPosition - GetGlobalMousePosition();
                    viewerGrabOffsetRB = grabbers[7].RectGlobalPosition - GetGlobalMousePosition();
                }
            }
        }
    }

    public override void _Input(InputEvent e) {
        if (e is InputEventMouseButton) {
            if ((e as InputEventMouseButton).ButtonIndex == (int)ButtonList.WheelUp) {
                cropOpt.b_ltx = Mathf.Clamp(cropOpt.b_ltx + 0.03f, 0, 1);
                cropOpt.b_lty = Mathf.Clamp(cropOpt.b_lty + 0.03f, 0, 1);
                cropOpt.b_brx = Mathf.Clamp(cropOpt.b_brx - 0.03f, 0, 1);
                cropOpt.b_bry = Mathf.Clamp(cropOpt.b_bry - 0.03f, 0, 1);
                checkLogicness();
                UpdateCorners();
                UpdateShader();
            }
            else if ((e as InputEventMouseButton).ButtonIndex == (int)ButtonList.WheelDown) {
                cropOpt.b_ltx = Mathf.Clamp(cropOpt.b_ltx - 0.03f, 0, 1);
                cropOpt.b_lty = Mathf.Clamp(cropOpt.b_lty - 0.03f, 0, 1);
                cropOpt.b_brx = Mathf.Clamp(cropOpt.b_brx + 0.03f, 0, 1);
                cropOpt.b_bry = Mathf.Clamp(cropOpt.b_bry + 0.03f, 0, 1);
                checkLogicness();
                UpdateCorners();
                UpdateShader();
            }
        }
    }

    public void checkLogicness() {
        if(cropOpt.b_ltx > cropOpt.b_brx) {
            float middle = (cropOpt.b_ltx + cropOpt.b_brx) / 2f;
            cropOpt.b_ltx = middle;
            cropOpt.b_brx = middle;
        }
        if(cropOpt.b_lty > cropOpt.b_bry) {
            float middle = (cropOpt.b_lty + cropOpt.b_bry) / 2f;
            cropOpt.b_lty = middle;
            cropOpt.b_bry = middle;
        }
    }

    public override void _Process(float delta) {
        if (Input.IsMouseButtonPressed(1) && grabbing) {
            Vector2 p1 = GlobalToUVPos(GetGlobalMousePosition() + viewerGrabOffsetLT);
            Vector2 p2 = GlobalToUVPos(GetGlobalMousePosition() + viewerGrabOffsetRB);
            p1.x = Mathf.Clamp(p1.x, 0, 1);
            p1.y = Mathf.Clamp(p1.y, 0, 1);
            p2.x = Mathf.Clamp(p2.x, 0, 1);
            p2.y = Mathf.Clamp(p2.y, 0, 1);
            
            cropOpt.b_ltx = p1.x;
            cropOpt.b_lty = p1.y;
            cropOpt.b_brx = p2.x;
            cropOpt.b_bry = p2.y;

            UpdateShader();
            UpdateCorners();
        }
    }

    public void MoveCorner(grabber gr, Vector2 grabOffset) {
        Vector2 moveTo = GetGlobalMousePosition() + grabOffset;
        Vector2 p = GlobalToUVPos(moveTo);
        p.x = Mathf.Clamp(p.x, 0, 1);
        p.y = Mathf.Clamp(p.y, 0, 1);
        moveTo = UVToGlobalPos(p);

        if (p.x > 1 || p.x < 0 || p.y > 1 || p.y < 0) return;

        gr.RectGlobalPosition = moveTo;
        switch (gr.Sector) {
            case 0:
                cropOpt.b_ltx = p.x;
                cropOpt.b_lty = p.y;
                break;
            case 1:
                cropOpt.b_lty = p.y;
                break;
            case 2:
                cropOpt.b_brx = p.x;
                cropOpt.b_lty = p.y;
                break;
            case 3:
                cropOpt.b_brx = p.x;
                break;
            case 4:
                cropOpt.b_brx = p.x;
                cropOpt.b_bry = p.y;
                break;
            case 5:
                cropOpt.b_bry = p.y;
                break;
            case 6:
                cropOpt.b_ltx = p.x;
                cropOpt.b_bry = p.y;
                break;
            case 7:
                cropOpt.b_ltx = p.x;
                break;
        }
        checkLogicness();
        UpdateCorners();
        UpdateShader();
    }

    public void UpdateShader() {
        (Material as ShaderMaterial).SetShaderParam("b_ltx", cropOpt.b_ltx);
        (Material as ShaderMaterial).SetShaderParam("b_lty", cropOpt.b_lty);
        (Material as ShaderMaterial).SetShaderParam("b_brx", cropOpt.b_brx);
        (Material as ShaderMaterial).SetShaderParam("b_bry", cropOpt.b_bry);
    }

    public void UpdateCorners() {

        grabbers[4].RectGlobalPosition = UVToGlobalPos(new Vector2(cropOpt.b_ltx, cropOpt.b_lty));
        grabbers[5].RectGlobalPosition = UVToGlobalPos(new Vector2(cropOpt.b_brx, cropOpt.b_lty));
        grabbers[6].RectGlobalPosition = UVToGlobalPos(new Vector2(cropOpt.b_ltx, cropOpt.b_bry));
        grabbers[7].RectGlobalPosition = UVToGlobalPos(new Vector2(cropOpt.b_brx, cropOpt.b_bry));

        grabbers[0].RectSize = new Vector2((grabbers[5].RectGlobalPosition.x - grabbers[4].RectGlobalPosition.x), 70);
        grabbers[1].RectSize = new Vector2((grabbers[6].RectGlobalPosition.y - grabbers[4].RectGlobalPosition.y), 70);
        grabbers[2].RectSize = new Vector2((grabbers[5].RectGlobalPosition.x - grabbers[4].RectGlobalPosition.x), 70);
        grabbers[3].RectSize = new Vector2((grabbers[6].RectGlobalPosition.y - grabbers[4].RectGlobalPosition.y), 70);

        grabbers[0].RectGlobalPosition = UVToGlobalPos(new Vector2((cropOpt.b_ltx + cropOpt.b_brx) / 2f, cropOpt.b_lty)) - new Vector2(grabbers[0].RectSize.x / 2f, 0);
        grabbers[1].RectGlobalPosition = UVToGlobalPos(new Vector2(cropOpt.b_brx, (cropOpt.b_lty + cropOpt.b_bry) / 2f)) - new Vector2(0, grabbers[1].RectSize.x / 2f);
        grabbers[2].RectGlobalPosition = UVToGlobalPos(new Vector2((cropOpt.b_ltx + cropOpt.b_brx) / 2f, cropOpt.b_bry)) + new Vector2(grabbers[2].RectSize.x / 2f, 0);
        grabbers[3].RectGlobalPosition = UVToGlobalPos(new Vector2(cropOpt.b_ltx, (cropOpt.b_lty + cropOpt.b_bry) / 2f)) + new Vector2(0, grabbers[3].RectSize.x / 2f);
    }

    
    // Returns a Global Posiition from a 0-1 Coordinate on the Texture
    public Vector2 GlobalToUVPos(Vector2 p) {
        return (p-corner) / actualTexSize;
    }

    // Returns a Global Posiition from a 0-1 Coordinate on the Texture
    public Vector2 UVToGlobalPos(Vector2 p) {
        return corner + p * actualTexSize;
    }
}

