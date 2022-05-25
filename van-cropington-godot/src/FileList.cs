using Godot;
using System.Collections.Generic;
using ImageMagick;

public class FileList : ItemList {
    [Export] NodePath NPViewer;
    [Export] NodePath NPMaxWidth;
    [Export] NodePath NPMaxHeight;
    [Export] NodePath NPCBOverWrite;
    Viewer v;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        v = GetNode<Viewer>(NPViewer);
        GetTree().Connect("files_dropped", this, nameof(OnFilesDropped));
    }

    List<string> fileList = new List<string>();
    public List<CropOption> cropOptions = new List<CropOption>();
    string[] supportedFileTypes = { "png", "PNG", "jpg", "jpeg", "JPG", "JPEG" };

    public void OnFilesDropped(string[] files, int screen) {
        var d = new Directory();

        bool firstDrop = GetItemCount() == 0;

        foreach (string droppedFile in files) {
            if (System.IO.Directory.Exists(droppedFile)) {
                string[] dirFiles = System.IO.Directory.GetFiles(droppedFile);

                foreach (string droppedDirFile in dirFiles) {
                    if (!System.Array.Exists(supportedFileTypes, element => element == droppedDirFile.Extension()) || fileList.Contains(droppedDirFile)) {
                        continue;
                    }

                    cropOptions.Add(new CropOption(0, 0, 1, 1));
                    AddItem(droppedDirFile.GetFile());
                    fileList.Add(droppedDirFile);
                }

            } else {

                if (!System.Array.Exists(supportedFileTypes, element => element == droppedFile.Extension()) || fileList.Contains(droppedFile)) {
                    continue;
                }

                cropOptions.Add(new CropOption(0, 0, 1, 1));
                AddItem(droppedFile.GetFile());
                fileList.Add(droppedFile);
            }

            if (firstDrop) {
                Select(0);
                OnItemSelected(0);
            }


        }
    }


    public override void _Input(InputEvent e) {
        if (e.IsActionPressed("move_down")) {
            if (GetItemCount() == 0) return;
            int toSelect = GetSelectedItems()[0] + 1;
            if (toSelect > GetItemCount()-1) return;
            Select(toSelect);
            OnItemSelected(toSelect);
        }
        else if(e.IsActionPressed("move_up")) {
            if (GetItemCount() == 0) return;
            int toSelect = GetSelectedItems()[0] - 1;
            if (toSelect < 0) return;
            Select(toSelect);
            OnItemSelected(toSelect);
        }
    }


    int oldIdx = 0;
    public void OnItemSelected(int idx) {
        // Save revious Options
        cropOptions[oldIdx] = GetNode<Viewer>(NPViewer).cropOpt;

        GetNode<Viewer>(NPViewer).LoadImage(fileList[idx], cropOptions[idx]);
        oldIdx = idx;
    }

    public void ClearFileList() {
        Clear();
        fileList = new List<string>();
        cropOptions = new List<CropOption>();
        oldIdx = 0;
        v.Reset();
    }

    public void OnSaveAll() {
        if (cropOptions.Count == 0) return;
        cropOptions[oldIdx] = GetNode<Viewer>(NPViewer).cropOpt;
        int maxWidth = (int)GetNode<SpinBox>(NPMaxWidth).Value;
        int maxHeight = (int)GetNode<SpinBox>(NPMaxHeight).Value;

        int i = 0;
        foreach (string imgPath in fileList) {
            try {
                using (MagickImage img = new MagickImage(imgPath)) {
                    MagickGeometry geometry = new MagickGeometry();
                    geometry.X = (int)(cropOptions[i].b_ltx * img.Width);
                    geometry.Y = (int)(cropOptions[i].b_lty * img.Height);
                    geometry.Width = (int)(cropOptions[i].b_brx * img.Width - cropOptions[i].b_ltx * img.Width);
                    geometry.Height = (int)(cropOptions[i].b_bry * img.Height - cropOptions[i].b_lty * img.Height);
                    img.Crop(geometry);

                    if (geometry.Width > maxWidth) {
                        img.Resize(maxWidth, maxHeight);
                    }

                    img.Format = MagickFormat.Jpg;
                    img.Quality = 75;
                    string dir = GetNode<CheckBox>(NPCBOverWrite).Pressed ? imgPath.GetBaseDir() + "/" : imgPath.GetBaseDir() + "/cropped/";
                    System.IO.Directory.CreateDirectory(dir);
                    img.Write(dir + System.IO.Path.GetFileNameWithoutExtension(imgPath) + ".jpg");

                }
            } catch (System.Exception e) {
                GD.Print(e);
            }
            i++;
        }

        ClearFileList();
    }
}
