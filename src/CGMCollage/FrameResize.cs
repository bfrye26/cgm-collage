namespace CGMCollage;

[Flags]
public enum ResizeEdges { None=0, Left=1, Right=2, Top=4, Bottom=8 }

public static class FrameResize
{
 public static bool Apply(Frame frame,Frame original,ResizeEdges edges,double dx,double dy)
 {
  if(edges==ResizeEdges.None||original.W<=0||original.H<=0)return false;
  double left=original.X,top=original.Y,right=left+original.W,bottom=top+original.H;
  double minWidth=Math.Min(.02,original.W),minHeight=Math.Min(.02,original.H);
  if(edges.HasFlag(ResizeEdges.Left))left=Math.Clamp(left+dx,0,right-minWidth);
  if(edges.HasFlag(ResizeEdges.Right))right=Math.Clamp(right+dx,left+minWidth,1);
  if(edges.HasFlag(ResizeEdges.Top))top=Math.Clamp(top+dy,0,bottom-minHeight);
  if(edges.HasFlag(ResizeEdges.Bottom))bottom=Math.Clamp(bottom+dy,top+minHeight,1);
  frame.X=left;frame.Y=top;
  frame.W=(edges&(ResizeEdges.Left|ResizeEdges.Right))!=0?right-left:original.W;
  frame.H=(edges&(ResizeEdges.Top|ResizeEdges.Bottom))!=0?bottom-top:original.H;
  frame.Points=original.Points.Select(p=>new PointD(left+(p.X-original.X)/original.W*frame.W,top+(p.Y-original.Y)/original.H*frame.H)).ToList();
  return frame.X!=original.X||frame.Y!=original.Y||Math.Abs(frame.W-original.W)>1e-10||Math.Abs(frame.H-original.H)>1e-10;
 }
}
