using Godot;
using System;

public struct CropOption
{
    public float b_ltx;
    public float b_lty;
    public float b_brx;
    public float b_bry;

    public CropOption(float b_ltx = 0f, float b_lty = 0f, float b_brx = 1f, float b_bry = 1f) {
        this.b_ltx = b_ltx;
        this.b_lty = b_lty;
        this.b_brx = b_brx;
        this.b_bry = b_bry;
    }
}
