namespace CGMCollage;

public static class PanelPlacement
{
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
