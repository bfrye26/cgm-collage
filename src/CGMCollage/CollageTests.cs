namespace CGMCollage;

// Executed by --smoke-test before the Windows installer is packaged.
public static class CollageTests
{
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static EmbeddedImage Image(string name)=>new(name,"image/png","test-data");
 public static void Run()
 {
  foreach(string layout in Templates.FlexibleNames)
  foreach(double aspect in new[]{1d,16d/9,4d/5})
  for(int count=1;count<=Templates.MaxPanels;count++)
  {
   var frames=Templates.CreateFlexible(layout,count,aspect);
   Check(frames.Count==count,$"{layout}: wrong count at {count}");
   Check(frames.All(f=>f.X>=0&&f.Y>=0&&f.W>0&&f.H>0&&f.X+f.W<=1.000001&&f.Y+f.H<=1.000001),"Panel outside canvas");
   Check(Math.Abs(frames.Sum(f=>f.W*f.H)-1)<.000001,"Layout does not fill canvas");
   for(int i=0;i<frames.Count;i++)for(int j=i+1;j<frames.Count;j++)
    Check(Math.Min(frames[i].X+frames[i].W,frames[j].X+frames[j].W)-Math.Max(frames[i].X,frames[j].X)<.000001||
          Math.Min(frames[i].Y+frames[i].H,frames[j].Y+frames[j].H)-Math.Max(frames[i].Y,frames[j].Y)<.000001,"Panels overlap");
  }
  foreach(int count in new[]{0,65})
  {
   bool rejected=false;try{Templates.CreateFlexible("Auto Grid",count);}catch(ArgumentOutOfRangeException){rejected=true;}
   Check(rejected,"Invalid count accepted");
  }
  var p=new Project();var first=Image("first");var second=Image("second");
  p.Frames[0].Image=first;p.Frames[0].CropX=.3;p.Frames[0].Brightness=.4;p.Frames[0].FlipX=true;
  p.Frames[0].BorderWidth=7;p.Frames[1].BorderWidth=2;
  var id=p.Frames[0].Id;p.SetPanelCount(12);
  Check(p.Frames[0].Image==first&&p.Frames[0].CropX==.3&&p.Frames[0].Id==id,"Resize lost image edits");
  p.ApplyTemplate("Columns");Check(p.Frames.Count==12&&p.Frames.All(f=>f.H==1),"Flexible layout lost count");
  p.Frames[1].Image=second;p.Frames[1].CropY=-.5;p.Frames[1].FitMode="fit";
  var geometry=p.Frames.Select(f=>(f.Id,f.X,f.Y,f.W,f.H,f.BorderWidth)).ToArray();
  var history=new History();history.Reset(p);
  Check(p.SwapImages(0,1),"Swap rejected");
  Check(p.Frames[1].Image==first&&p.Frames[1].CropX==.3&&p.Frames[1].Brightness==.4&&p.Frames[1].FlipX,"Swap lost source edits");
  Check(p.Frames[0].Image==second&&p.Frames[0].CropY==-.5&&p.Frames[0].FitMode=="fit","Swap lost destination edits");
  Check(geometry.SequenceEqual(p.Frames.Select(f=>(f.Id,f.X,f.Y,f.W,f.H,f.BorderWidth))),"Swap moved panel geometry");
  history.Push(p);Check(history.Undo().Frames[0].Image==first,"Undo swap failed");
  Check(history.Redo().Frames[0].Image==second,"Redo swap failed");
  Check(p.SwapImages(1,2)&&p.Frames[1].Image is null&&p.Frames[2].Image==first,"Move into empty panel failed");
  Check(!p.SwapImages(1,0)&&!p.SwapImages(2,2)&&!p.SwapImages(-1,0),"Invalid swap accepted");
  p=new Project();p.SetPanelCount(1);
  var batch=Enumerable.Range(0,20).Select(i=>Image($"image-{i}")).ToArray();
  PanelPlacement.Place(p,batch);
  Check(p.Frames.Count==20&&p.Frames.Select(f=>f.Image).SequenceEqual(batch),"Batch dropped images");
  PanelPlacement.Place(p,new[]{first});
  Check(p.Frames.Count==21&&p.Frames[0].Image==batch[0]&&p.Frames[20].Image==first,"Append overwrote an image");
  PanelPlacement.Place(p,new[]{second,first},3);
  Check(p.Frames.Count==22&&p.Frames[3].Image==second&&p.Frames[21].Image==first,"Targeted import did not grow correctly");
  p.SetPanelCount(64);foreach(var frame in p.Frames)frame.Image=first;
  bool full=false;try{PanelPlacement.Place(p,new[]{second});}catch(InvalidOperationException){full=true;}
  Check(full&&p.Frames.Count==64&&p.Frames.All(f=>f.Image==first),"Over-limit import changed project");
  PanelPlacement.Place(p,new[]{second},63);Check(p.Frames[63].Image==second,"Replacement at limit failed");
  history.Reset(p);p.SetPanelCount(1);history.Push(p);
  Check(history.Undo().Frames.Count==64&&history.Redo().Frames.Count==1,"Undo/redo count failed");
  var original=new Frame{X=.2,Y=.2,W=.5,H=.5,Image=first,CropX=.4,Zoom=1.6};
  var resized=original.Clone();
  FrameResize.Apply(resized,original,ResizeEdges.Right,.1,0);
  Check(Math.Abs(resized.W-.6)<1e-10&&resized.X==.2&&resized.H==.5,"Right edge resize failed");
  FrameResize.Apply(resized,original,ResizeEdges.Left|ResizeEdges.Top,.1,-.1);
  Check(Math.Abs(resized.X-.3)<1e-10&&Math.Abs(resized.Y-.1)<1e-10&&Math.Abs(resized.W-.4)<1e-10&&Math.Abs(resized.H-.6)<1e-10,"Corner resize failed");
  FrameResize.Apply(resized,original,ResizeEdges.Right|ResizeEdges.Bottom,10,10);
  Check(Math.Abs(resized.X+resized.W-1)<1e-10&&Math.Abs(resized.Y+resized.H-1)<1e-10,"Resize escaped canvas");
  FrameResize.Apply(resized,original,ResizeEdges.Left|ResizeEdges.Top,10,10);
  Check(resized.W>=.019999&&resized.H>=.019999,"Resize crossed opposite edge");
  Check(resized.Image==first&&resized.CropX==.4&&resized.Zoom==1.6,"Resize changed image edits");
  var polygon=Templates.Create("Angled Triptych")[0];var polygonCopy=polygon.Clone();
  FrameResize.Apply(polygon,polygonCopy,ResizeEdges.Right,.1,0);
  Check(Math.Abs(polygon.Points.Max(q=>q.X)-(polygon.X+polygon.W))<1e-10&&polygonCopy.Points.Max(q=>q.X)!=polygon.Points.Max(q=>q.X),"Polygon resize failed");
  double previousHeight=resized.H;
  p=new Project{Frames=[resized],Template="Custom"};history.Reset(p);
  FrameResize.Apply(p.Frames[0],resized.Clone(),ResizeEdges.Bottom,0,.05);history.Push(p);
  Check(history.Undo().Frames[0].H==previousHeight,"Resize undo failed");
  string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),$"cgm-resize-{Guid.NewGuid():N}.cgmcollage");
  try
  {
   ProjectIO.Save(path,p);var loaded=ProjectIO.Load(path);
   Check(loaded.Template=="Custom"&&loaded.Frames.Count==1&&loaded.Frames[0].W==p.Frames[0].W&&loaded.Frames[0].H==p.Frames[0].H,"Saved resize was not restored");
   Check(loaded.Frames[0].Image==first&&loaded.Frames[0].CropX==.4,"Saved resize lost image edits");
   p.SetPanelCount(37);ProjectIO.Save(path,p);loaded=ProjectIO.Load(path);
   Check(loaded.Frames.Count==37&&loaded.Template=="Auto Grid","Flexible layout save/load failed");
  }
  finally{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
 }
}
