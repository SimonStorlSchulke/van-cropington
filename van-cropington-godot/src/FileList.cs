using Godot;
using System.Collections.Generic;
using ImageMagick;

public class FileList : ItemList
{
    [Export] NodePath NPViewer;
    [Export] NodePath NPMaxWidth;
    [Export] NodePath NPMaxHeight;
    Viewer v;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        v = GetNode<Viewer>(NPViewer);
        GetTree().Connect("files_dropped", this, nameof(OnFilesDropped));
    }

    List<string> fileList = new List<string>();
    public List<CropOption> cropOptions = new List<CropOption>();
    string[] supportedFileTypes = {"png", "PNG", "jpg", "jpeg", "JPG", "JPEG"};

    public void OnFilesDropped(string[] files, int screen) {
        var d = new Directory();

        bool firstDrop = GetItemCount() == 0;
                
        foreach (var droppedFile in files) {
            if (!System.Array.Exists(supportedFileTypes, element => element == droppedFile.Extension())) {
                continue;
            }
            if (fileList.Contains(droppedFile)) {
                continue;
            }

            cropOptions.Add(new CropOption(0,0,1,1));
            AddItem(droppedFile.GetFile());
            fileList.Add(droppedFile);

            if (firstDrop) {
                Select(0);
                OnItemSelected(0);
            }
        }
    }
    int oldIdx = 0;
    public void OnItemSelected(int idx) {
        // Save revious Options
        cropOptions[oldIdx] = GetNode<Viewer>(NPViewer).cropOpt;

        GetNode<Viewer>(NPViewer).LoadImage(fileList[idx], cropOptions[idx]);
        oldIdx = idx;
    }

    public void OnSaveAll() {
        cropOptions[oldIdx] = GetNode<Viewer>(NPViewer).cropOpt;
        int maxWidth = (int)GetNode<SpinBox>(NPMaxWidth).Value;
        int maxHeight = (int)GetNode<SpinBox>(NPMaxHeight).Value;

        int i=0;
        foreach (string imgPath in fileList) {
            using (MagickImage img = new MagickImage(imgPath))
            {
                try {
                    MagickGeometry geometry = new MagickGeometry();
                    geometry.X = (int)(cropOptions[i].b_ltx * img.Width);
                    geometry.Y = (int)(cropOptions[i].b_lty * img.Height);
                    geometry.Width =  (int)(cropOptions[i].b_brx * img.Width - cropOptions[i].b_ltx * img.Width);
                    geometry.Height = (int)(cropOptions[i].b_bry * img.Height - cropOptions[i].b_lty * img.Height);
                    img.Crop(geometry);

                    if (geometry.Width > maxWidth) {
                        img.Resize(maxWidth, maxHeight);
                    }

                    img.Format = MagickFormat.Jpg;
                    img.Quality = 75;
                    System.IO.Directory.CreateDirectory(imgPath.GetBaseDir() + "/cropped");
                    img.Write(imgPath.GetBaseDir() + "/cropped/" + System.IO.Path.GetFileNameWithoutExtension(imgPath) + ".jpg");

                } catch (System.Exception e) {
                    GD.Print(e);
                }
            }
            i++;
        }
        
        Clear();
        v.Reset();
    }
}