namespace CGMCollage;

public static class PanelPlacement
{
    public static int Place(Project project, IReadOnlyList<EmbeddedImage> images, int? preferredPanel=null)
    {
        if(images.Count==0)return -1;
        if(preferredPanel is int target&&(target<0||target>=project.Frames.Count))throw new ArgumentOutOfRangeException(nameof(preferredPanel));
        int available=project.Frames.Count(f=>f.Image is null);
        if(preferredPanel is int occupied&&project.Frames[occupied].Image is not null)available++;
        int needed=project.Frames.Count+Math.Max(0,images.Count-available);
        if(needed>Templates.MaxPanels)throw new InvalidOperationException($"These images need {needed} panels. A collage can contain up to {Templates.MaxPanels}. No images were added.");
        if(needed>project.Frames.Count)project.SetPanelCount(needed);
        int first=preferredPanel??project.Frames.FindIndex(f=>f.Image is null);
        var panels=Plan(project,first,images.Count);
        for(int i=0;i<images.Count;i++)
        {
            var frame=project.Frames[panels[i]];
            frame.Image=images[i];frame.ResetCrop();
            frame.Brightness=0;frame.Contrast=frame.Saturation=frame.Opacity=1;frame.FitMode="fill";
        }
        return panels[^1];
    }
    public static IReadOnlyList<int> Plan(Project project, int firstPanel, int imageCount)
    {
        if (project.Frames.Count == 0 || imageCount <= 0 || firstPanel < 0 || firstPanel >= project.Frames.Count)
            return [];

        List<int> panels = [firstPanel];
        for (int offset = 1; offset < project.Frames.Count && panels.Count < imageCount; offset++)
        {
            int candidate = (firstPanel + offset) % project.Frames.Count;
            if (project.Frames[candidate].Image is null)
                panels.Add(candidate);
        }
        return panels;
    }
}
