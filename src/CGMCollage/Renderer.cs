using SkiaSharp;
using System.IO;
namespace CGMCollage;

public static class Renderer
{
 public static void Draw(Project p, SKCanvas c, int width, int height, int selected=-1, bool guides=false,float handleSize=10)
 {
  c.Clear(Parse(p.Background)); float sx=width/(float)p.CanvasWidth,sy=height/(float)p.CanvasHeight;
  c.Save();c.Scale(sx,sy);
  for(int i=0;i<p.Frames.Count;i++) DrawFrame(p,c,p.Frames[i],i==selected);
  foreach(var t in p.Texts.Where(t=>!t.Hidden)) DrawText(p,c,t);
  if(guides){using var paint=new SKPaint{Color=new SKColor(255,255,255,115),Style=SKPaintStyle.Stroke,StrokeWidth=1,PathEffect=SKPathEffect.CreateDash([8,7],0)};c.DrawRect(p.CanvasWidth*.05f,p.CanvasHeight*.05f,p.CanvasWidth*.9f,p.CanvasHeight*.9f,paint);}
  if(selected>=0&&selected<p.Frames.Count&&p.Frames[selected] is {Hidden:false,Locked:false} frame)DrawHandles(p,c,frame,handleSize);
  c.Restore();
 }
 static void DrawHandles(Project p,SKCanvas c,Frame f,float size)
 {
  float left=(float)(f.X*p.CanvasWidth),top=(float)(f.Y*p.CanvasHeight),right=(float)((f.X+f.W)*p.CanvasWidth),bottom=(float)((f.Y+f.H)*p.CanvasHeight);
  using var outline=new SKPaint{Color=new SKColor(245,27,87),Style=SKPaintStyle.Stroke,StrokeWidth=size/6,IsAntialias=true,PathEffect=SKPathEffect.CreateDash([size,size/2],0)};
  if(f.Shape=="poly"){using var path=Path(p,f);c.DrawPath(path,outline);}else c.DrawRect(new SKRect(left,top,right,bottom),outline);
  using var fill=new SKPaint{Color=SKColors.White,IsAntialias=true};
  using var edge=new SKPaint{Color=new SKColor(245,27,87),Style=SKPaintStyle.Stroke,StrokeWidth=size/5,IsAntialias=true};
  if(f.Shape=="poly"&&f.Points.Count>2)
  {
   foreach(var index in Enumerable.Range(0,f.Points.Count))
   {
    var a=f.Points[index];var b=f.Points[(index+1)%f.Points.Count];
    bool outer=(Math.Abs(a.X-b.X)<1e-8&&(a.X<1e-8||a.X>1-1e-8))||(Math.Abs(a.Y-b.Y)<1e-8&&(a.Y<1e-8||a.Y>1-1e-8));
    if(outer)continue;
    foreach(var point in new[]{a,new PointD((a.X+b.X)/2,(a.Y+b.Y)/2),b})
    {
     float x=(float)(point.X*p.CanvasWidth),y=(float)(point.Y*p.CanvasHeight);
     var rect=new SKRect(x-size/2,y-size/2,x+size/2,y+size/2);c.DrawRect(rect,fill);c.DrawRect(rect,edge);
    }
   }
   return;
  }
  foreach(var point in new[]{new SKPoint(left,top),new SKPoint((left+right)/2,top),new SKPoint(right,top),new SKPoint(left,(top+bottom)/2),new SKPoint(right,(top+bottom)/2),new SKPoint(left,bottom),new SKPoint((left+right)/2,bottom),new SKPoint(right,bottom)})
  {
   bool movableX=(Math.Abs(point.X-left)<.01&&left>.01)||(Math.Abs(point.X-right)<.01&&right<p.CanvasWidth-.01);
   bool movableY=(Math.Abs(point.Y-top)<.01&&top>.01)||(Math.Abs(point.Y-bottom)<.01&&bottom<p.CanvasHeight-.01);
   if(!movableX&&!movableY)continue;
   var rect=new SKRect(point.X-size/2,point.Y-size/2,point.X+size/2,point.Y+size/2);c.DrawRect(rect,fill);c.DrawRect(rect,edge);
  }
 }
 static void DrawFrame(Project p,SKCanvas c,Frame f,bool selected)
 {
  if(f.Hidden)return;using var path=Path(p,f); var bounds=path.Bounds;
  if(f.Shadow){double a=f.ShadowAngle*Math.PI/180;float dx=(float)(Math.Cos(a)*f.ShadowDistance),dy=(float)(Math.Sin(a)*f.ShadowDistance);using var sp=new SKPaint{Color=new SKColor(0,0,0,(byte)(255*f.ShadowOpacity)),ImageFilter=SKImageFilter.CreateBlur((float)f.ShadowBlur,(float)f.ShadowBlur)};c.Save();c.Translate(dx,dy);c.DrawPath(path,sp);c.Restore();}
  c.Save();c.ClipPath(path,SKClipOperation.Intersect,true);
  if(f.Image is not null){try{using var bitmap=SKBitmap.Decode(Convert.FromBase64String(f.Image.Data));if(bitmap is not null)DrawBitmap(c,bitmap,f,bounds);}catch{DrawEmpty(c,bounds,"Image unavailable");}}
  else DrawEmpty(c,bounds,$"{p.Frames.IndexOf(f)+1}\nDrop image");
  c.Restore();
  if(p.Gap>0){using var gap=new SKPaint{Color=Parse(p.GapColor),Style=SKPaintStyle.Stroke,StrokeWidth=(float)p.Gap,IsAntialias=true};c.DrawPath(path,gap);}
  using var edge=new SKPaint{Color=selected?new SKColor(245,27,87):new SKColor(185,194,207,150),Style=SKPaintStyle.Stroke,StrokeWidth=selected?3:1.2f,IsAntialias=true};c.DrawPath(path,edge);
  if(f.BorderWidth>0){using var border=new SKPaint{Color=Parse(f.BorderColor),Style=SKPaintStyle.Stroke,StrokeWidth=(float)f.BorderWidth,IsAntialias=true};c.DrawPath(path,border);}
 }
 static void DrawBitmap(SKCanvas c,SKBitmap b,Frame f,SKRect dst)
 {
  float scale=f.FitMode=="fit"?Math.Min(dst.Width/b.Width,dst.Height/b.Height):Math.Max(dst.Width/b.Width,dst.Height/b.Height);scale*=(float)f.Zoom;
  float w=b.Width*scale,h=b.Height*scale,cx=dst.MidX+(float)f.CropX*Math.Max(0,w-dst.Width)/2,cy=dst.MidY+(float)f.CropY*Math.Max(0,h-dst.Height)/2;
  c.Save();c.Translate(dst.MidX,dst.MidY);c.RotateDegrees((float)f.Rotation);c.Scale(f.FlipX?-1:1,f.FlipY?-1:1);c.Translate(-dst.MidX,-dst.MidY);
  using var paint=new SKPaint{IsAntialias=true,FilterQuality=SKFilterQuality.High,Color=new SKColor(255,255,255,(byte)(255*f.Opacity)),ColorFilter=ColorFilter(f)};
  c.DrawBitmap(b,new SKRect(cx-w/2,cy-h/2,cx+w/2,cy+h/2),paint);c.Restore();
 }
 static SKColorFilter ColorFilter(Frame f)
 {
  float sat=(float)f.Saturation,con=(float)f.Contrast,br=(float)(f.Brightness*255),ir=.213f*(1-sat),ig=.715f*(1-sat),ib=.072f*(1-sat),t=128*(1-con)+br;
  return SKColorFilter.CreateColorMatrix([con*(ir+sat),con*ig,con*ib,0,t, con*ir,con*(ig+sat),con*ib,0,t, con*ir,con*ig,con*(ib+sat),0,t, 0,0,0,1,0]);
 }
 static void DrawEmpty(SKCanvas c,SKRect r,string text){using var bg=new SKPaint{Color=new SKColor(31,36,44)};c.DrawRect(r,bg);using var stripe=new SKPaint{Color=new SKColor(24,28,35),StrokeWidth=18};for(float x=r.Left-r.Height;x<r.Right;x+=42)c.DrawLine(x,r.Bottom,x+r.Height,r.Top,stripe);using var tp=new SKPaint{Color=new SKColor(174,182,194),TextSize=18,TextAlign=SKTextAlign.Center,IsAntialias=true};var lines=text.Split('\n');for(int i=0;i<lines.Length;i++)c.DrawText(lines[i],r.MidX,r.MidY+i*24,tp);}
 static void DrawText(Project p,SKCanvas c,TextLayer t){using var face=SKTypeface.FromFamilyName(t.FontFamily,t.Bold?SKFontStyle.Bold:SKFontStyle.Normal);using var paint=new SKPaint{Color=Parse(t.Color),TextSize=(float)t.FontSize,Typeface=face,IsAntialias=true};float x=(float)(t.X*p.CanvasWidth),y=(float)(t.Y*p.CanvasHeight+t.FontSize);if(t.Shadow){using var s=new SKPaint{Color=new SKColor(0,0,0,170),TextSize=(float)t.FontSize,Typeface=face,IsAntialias=true,ImageFilter=SKImageFilter.CreateBlur(3,3)};c.DrawText(t.Text,x+3,y+4,s);}c.DrawText(t.Text,x,y,paint);}
 public static SKPath Path(Project p,Frame f){var path=new SKPath();if(f.Shape=="ellipse")path.AddOval(new SKRect((float)(f.X*p.CanvasWidth),(float)(f.Y*p.CanvasHeight),(float)((f.X+f.W)*p.CanvasWidth),(float)((f.Y+f.H)*p.CanvasHeight)));else if(f.Shape=="poly"&&f.Points.Count>2){path.MoveTo((float)(f.Points[0].X*p.CanvasWidth),(float)(f.Points[0].Y*p.CanvasHeight));foreach(var q in f.Points.Skip(1))path.LineTo((float)(q.X*p.CanvasWidth),(float)(q.Y*p.CanvasHeight));path.Close();}else path.AddRect(new SKRect((float)(f.X*p.CanvasWidth),(float)(f.Y*p.CanvasHeight),(float)((f.X+f.W)*p.CanvasWidth),(float)((f.Y+f.H)*p.CanvasHeight)));return path;}
 public static SKColor Parse(string value){try{return SKColor.Parse(value);}catch{return new SKColor(16,18,22);}}
 public static SKBitmap Render(Project p,int width,int height){var b=new SKBitmap(width,height,SKColorType.Rgba8888,SKAlphaType.Premul);using var c=new SKCanvas(b);Draw(p,c,width,height);return b;}
 public static void Export(Project p,string path)
 {
  using var b=Render(p,p.CanvasWidth,p.CanvasHeight);using var image=SKImage.FromBitmap(b);var ext=System.IO.Path.GetExtension(path).ToLowerInvariant();var fmt=ext==".png"?SKEncodedImageFormat.Png:ext==".webp"?SKEncodedImageFormat.Webp:SKEncodedImageFormat.Jpeg;using var data=image.Encode(fmt,p.ExportQuality);using var stream=File.Open(path,FileMode.Create);data.SaveTo(stream);
 }
}
