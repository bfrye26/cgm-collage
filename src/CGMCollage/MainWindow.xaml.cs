using Microsoft.Win32;
using SkiaSharp.Views.Desktop;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace CGMCollage;

public partial class MainWindow : Window
{
 Project project=new();readonly History history=new();int selected,dragTarget=-1;bool syncing=true,dirty,dragging,panning; const string PanelDragFormat="CGMCollage.Panel";Point dragStart;double dragCropX,dragCropY;string? currentPath;readonly DispatcherTimer autosave=new(){Interval=TimeSpan.FromSeconds(25)};
 Frame? Selected=>selected>=0&&selected<project.Frames.Count?project.Frames[selected]:null;
 Frame? resizeOriginal;ResizeEdges resizeEdges;string? resizeTemplate;
 public MainWindow()
 {
  InitializeComponent();BuildTemplates();history.Reset(project);SyncUi();KeyDown+=OnKeyDown;Closing+=OnClosing;autosave.Tick+=(_,_)=>Autosave();autosave.Start();
 }
 void BuildTemplates()
 {
  const double width=84,height=36;
  Color[] palette=[Color.FromRgb(207,214,224),Color.FromRgb(145,155,170),Color.FromRgb(86,96,111)];
  foreach(var name in Templates.FlexibleNames.Concat(Templates.Names))
  {
   var preview=new System.Windows.Controls.Canvas{Width=width,Height=height,Background=new SolidColorBrush(Color.FromRgb(14,17,21)),ClipToBounds=true};
   int panelIndex=0;
   foreach(var frame in Templates.IsFlexible(name)?Templates.CreateFlexible(name,6):Templates.Create(name))
   {
    var fill=new SolidColorBrush(palette[panelIndex++%palette.Length]);
    var stroke=new SolidColorBrush(Color.FromRgb(31,36,44));
    if(frame.Shape=="poly"&&frame.Points.Count>2)
    {
     var points=new PointCollection(frame.Points.Select(point=>new Point(point.X*width,point.Y*height)));
     preview.Children.Add(new System.Windows.Shapes.Polygon{Points=points,Fill=fill,Stroke=stroke,StrokeThickness=1});
    }
    else if(frame.Shape=="ellipse")
    {
     var ellipse=new System.Windows.Shapes.Ellipse{Fill=fill,Stroke=stroke,StrokeThickness=1,Width=Math.Max(3,frame.W*width),Height=Math.Max(3,frame.H*height)};
     System.Windows.Controls.Canvas.SetLeft(ellipse,frame.X*width);System.Windows.Controls.Canvas.SetTop(ellipse,frame.Y*height);preview.Children.Add(ellipse);
    }
    else
    {
     var rectangle=new Border{Background=fill,BorderBrush=stroke,BorderThickness=new Thickness(1),Width=Math.Max(3,frame.W*width),Height=Math.Max(3,frame.H*height)};
     System.Windows.Controls.Canvas.SetLeft(rectangle,frame.X*width);System.Windows.Controls.Canvas.SetTop(rectangle,frame.Y*height);preview.Children.Add(rectangle);
    }
   }
   var stack=new StackPanel{Width=width};stack.Children.Add(preview);stack.Children.Add(new TextBlock{Text=name,FontSize=10,Margin=new Thickness(0,4,0,0),TextWrapping=TextWrapping.Wrap,Height=23,LineHeight=11,TextTrimming=TextTrimming.CharacterEllipsis});
   var button=new Button{Tag=name,Content=stack,Padding=new Thickness(5),Margin=new Thickness(0,0,7,6),HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Stretch,ToolTip=$"Apply {name} layout"};
   button.Click+=Template_Click;TemplateList.Items.Add(button);
  }
 }
 void Changed(string message,bool push=true){dirty=true;if(push)history.Push(project);StatusText.Text=message;SyncUi();Canvas.InvalidateVisual();}
 void SyncUi()
 {
  syncing=true;ImageCountBox.Text=project.Frames.Count.ToString();AddPanelButton.IsEnabled=DuplicatePanelButton.IsEnabled=project.Frames.Count<Templates.MaxPanels;DeletePanelButton.IsEnabled=project.Frames.Count>1;CanvasHost.Width=project.CanvasWidth;CanvasHost.Height=project.CanvasHeight;var f=Selected;PanelNumber.Text=(selected+1).ToString();PanelBadge.Visibility=f is null?Visibility.Collapsed:Visibility.Visible;ImageName.Text=f?.Image?.Name??"Drop or choose an image";FitButton.Content=f?.FitMode=="fit"?"Fit image":"Fill frame";
  if(f is not null){ZoomSlider.Value=f.Zoom;CropXSlider.Value=f.CropX;CropYSlider.Value=f.CropY;RotationSlider.Value=f.Rotation;BrightnessSlider.Value=f.Brightness;ContrastSlider.Value=f.Contrast;SaturationSlider.Value=f.Saturation;OpacitySlider.Value=f.Opacity;BorderSlider.Value=f.BorderWidth;ShadowToggle.IsChecked=f.Shadow;FlipXButton.Background=f.FlipX?(Brush)FindResource("AccentBrush"):(Brush)FindResource("RaisedBrush");FlipYButton.Background=f.FlipY?(Brush)FindResource("AccentBrush"):(Brush)FindResource("RaisedBrush");}
  GapSlider.Value=project.Gap;GapValue.Text=$"{project.Gap:0} px";ProjectStatus.Text=project.Name+(dirty?" � Unsaved":"");UndoButton.IsEnabled=history.CanUndo;RedoButton.IsEnabled=history.CanRedo;foreach(Button b in TemplateList.Items){bool active=Equals(b.Tag,project.Template);b.BorderBrush=active?(Brush)FindResource("AccentBrush"):(Brush)FindResource("StrokeBrush");b.Background=active?new SolidColorBrush(Color.FromRgb(67,20,39)):(Brush)FindResource("RaisedBrush");}syncing=false;
 }
 void Template_Click(object sender,RoutedEventArgs e){if(sender is Button{Tag:string n}){int count=Templates.IsFlexible(n)?project.Frames.Count:Templates.Create(n).Count;if(!ConfirmReduction(count))return;project.ApplyTemplate(n);selected=Math.Min(selected,project.Frames.Count-1);Changed($"Applied {n}");}}
 bool ConfirmReduction(int count)=>!project.Frames.Skip(count).Any(f=>f.Image is not null)||MessageBox.Show(this,"This layout removes panels containing images. Continue? You can undo this change.","Reduce image count",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
 void ImageCount_Click(object s,RoutedEventArgs e)
 {
  if(!int.TryParse(ImageCountBox.Text,out int count)||count<1||count>Templates.MaxPanels){StatusText.Text=$"Enter an image count from 1 to {Templates.MaxPanels}.";ImageCountBox.Focus();ImageCountBox.SelectAll();return;}
  if(count==project.Frames.Count)return;
  if(!ConfirmReduction(count)){SyncUi();return;}
  project.SetPanelCount(count);selected=Math.Min(selected,count-1);Changed($"Collage now has {count} image panels");
 }
 void ImageCount_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.Enter){ImageCount_Click(s,e);e.Handled=true;}}
 void AddPanel_Click(object s,RoutedEventArgs e){if(project.Frames.Count>=Templates.MaxPanels)return;project.SetPanelCount(project.Frames.Count+1);selected=project.Frames.Count-1;Changed("Added an empty panel");}
 void DeletePanel_Click(object s,RoutedEventArgs e){if(project.Frames.Count<=1)return;project.Frames.RemoveAt(selected);project.SetPanelCount(project.Frames.Count);selected=Math.Min(selected,project.Frames.Count-1);Changed("Panel deleted � Undo to restore");}
 void New_Click(object s,RoutedEventArgs e){if(!CanDiscard())return;project=new();selected=0;currentPath=null;dirty=false;history.Reset(project);StatusText.Text="New project";SyncUi();Canvas.InvalidateVisual();}
 void Open_Click(object s,RoutedEventArgs e){if(!CanDiscard())return;var d=new OpenFileDialog{Filter="CGM Collage project (*.cgmcollage)|*.cgmcollage",Title="Open collage project"};if(d.ShowDialog()==true)OpenPath(d.FileName);}
 public void OpenPath(string path){try{project=ProjectIO.Load(path);currentPath=path;project.Name=Path.GetFileNameWithoutExtension(path);selected=0;dirty=false;history.Reset(project);StatusText.Text=$"Opened {project.Name}";SyncUi();Canvas.InvalidateVisual();}catch(Exception ex){MessageBox.Show(this,$"This project could not be opened.\n\n{ex.Message}","Open project",MessageBoxButton.OK,MessageBoxImage.Error);}}
 void Save_Click(object s,RoutedEventArgs e)=>SaveProject(false);
 bool SaveProject(bool copy)
 {
  var path=copy?null:currentPath;if(path is null){var d=new SaveFileDialog{Filter="CGM Collage project (*.cgmcollage)|*.cgmcollage",DefaultExt=".cgmcollage",FileName=project.Name=="Untitled project"?"My collage":project.Name};if(d.ShowDialog()!=true)return false;path=d.FileName;}
  try{project.Name=Path.GetFileNameWithoutExtension(path);ProjectIO.Save(path,project);currentPath=path;dirty=false;StatusText.Text="Project saved";SyncUi();return true;}catch(Exception ex){MessageBox.Show(this,$"The project could not be saved.\n\n{ex.Message}","Save project",MessageBoxButton.OK,MessageBoxImage.Error);return false;}
 }
 bool CanDiscard(){if(!dirty)return true;var r=MessageBox.Show(this,"Save changes to this collage?","CGM Collage",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);return r==MessageBoxResult.No||(r==MessageBoxResult.Yes&&SaveProject(false));}
 void Undo_Click(object s,RoutedEventArgs e){if(history.CanUndo){project=history.Undo();selected=Math.Min(selected,project.Frames.Count-1);dirty=true;StatusText.Text="Undid last change";SyncUi();Canvas.InvalidateVisual();}}
 void Redo_Click(object s,RoutedEventArgs e){if(history.CanRedo){project=history.Redo();selected=Math.Min(selected,project.Frames.Count-1);dirty=true;StatusText.Text="Redid change";SyncUi();Canvas.InvalidateVisual();}}
 void AddImages_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif;*.tif;*.tiff",Multiselect=true,Title="Add images"};if(d.ShowDialog()==true)AddImages(d.FileNames);}
 void Replace_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif;*.tif;*.tiff",Title="Choose an image"};if(d.ShowDialog()==true&&Selected is { } f){try{var image=ProjectIO.ReadImage(d.FileName);PanelPlacement.Place(project,new[]{image},selected);Changed("Image replaced");}catch(Exception ex){MessageBox.Show(this,ex.Message,"Image error",MessageBoxButton.OK,MessageBoxImage.Error);}}}
 void AddImages(IEnumerable<string> files,int? preferredPanel=null)
 {
  var paths=files.Where(IsImage).ToList();if(paths.Count==0){StatusText.Text="Choose supported image files.";return;}
  try
  {
   if(paths.Count>Templates.MaxPanels)throw new InvalidOperationException($"Choose at most {Templates.MaxPanels} images at a time.");
   // Read and validate the full batch before changing the project.
   var images=paths.Select(ProjectIO.ReadImage).ToList();
   selected=PanelPlacement.Place(project,images,preferredPanel);
   Changed(images.Count==1?$"Placed image in panel {selected+1}":$"Placed all {images.Count} images");
  }
  catch(Exception ex){MessageBox.Show(this,$"Images could not be added.\n\n{ex.Message}","Add images",MessageBoxButton.OK,MessageBoxImage.Error);}
 }
 void Remove_Click(object s,RoutedEventArgs e){if(Selected is { } f&&f.Image is not null){f.Image=null;Changed("Image removed");}}
 void Fit_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.FitMode=f.FitMode=="fit"?"fill":"fit";Changed(f.FitMode=="fit"?"Image fitted inside panel":"Image fills panel");}}
 void Centre_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.CropX=f.CropY=0;Changed("Image centred");}}
 void ResetCrop_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.ResetCrop();Changed("Crop reset");}}
 void FlipX_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.FlipX=!f.FlipX;Changed("Horizontal flip updated");}}
 void FlipY_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.FlipY=!f.FlipY;Changed("Vertical flip updated");}}
 void Duplicate_Click(object s,RoutedEventArgs e)
 {
  if(Selected is not { } f||project.Frames.Count>=Templates.MaxPanels)return;
  var n=f.Clone();n.Id=Guid.NewGuid().ToString("N");n.Name=$"Panel {project.Frames.Count+1}";
  project.Frames.Add(n);project.SetPanelCount(project.Frames.Count);selected=project.Frames.Count-1;Changed("Panel duplicated");
 }
 void PropertySlider_Changed(object s,RoutedPropertyChangedEventArgs<double> e){if(syncing||Selected is not { } f)return;f.Zoom=ZoomSlider.Value;f.CropX=CropXSlider.Value;f.CropY=CropYSlider.Value;f.Rotation=RotationSlider.Value;f.Brightness=BrightnessSlider.Value;f.Contrast=ContrastSlider.Value;f.Saturation=SaturationSlider.Value;f.Opacity=OpacitySlider.Value;f.BorderWidth=BorderSlider.Value;dirty=true;history.Push(project);UndoButton.IsEnabled=true;ProjectStatus.Text=project.Name+" � Unsaved";Canvas.InvalidateVisual();}
 void Gap_Changed(object s,RoutedPropertyChangedEventArgs<double> e){if(syncing)return;project.Gap=GapSlider.Value;GapValue.Text=$"{project.Gap:0} px";dirty=true;history.Push(project);UndoButton.IsEnabled=true;Canvas.InvalidateVisual();}
 void FrameOption_Changed(object s,RoutedEventArgs e){if(syncing||Selected is not { } f)return;f.Shadow=ShadowToggle.IsChecked==true;Changed("Frame style updated");}
 void CanvasOption_Changed(object s,RoutedEventArgs e)=>Canvas.InvalidateVisual();
 void CanvasPreset_Changed(object s,SelectionChangedEventArgs e){if(syncing||CanvasPreset.SelectedItem is not ComboBoxItem{Tag:string tag})return;var a=tag.Split(',');if(a.Length==2){project.CanvasWidth=int.Parse(a[0]);project.CanvasHeight=int.Parse(a[1]);Changed($"Canvas set to {project.CanvasWidth} x {project.CanvasHeight}");}}
 void Canvas_PaintSurface(object? s,SKPaintSurfaceEventArgs e)
 {
  float handleSize=10;
  if(Canvas.IsLoaded)
  {
   double displayedWidth=Math.Abs(Canvas.PointToScreen(new Point(Canvas.ActualWidth,0)).X-Canvas.PointToScreen(new Point(0,0)).X);
   handleSize=(float)(10*project.CanvasWidth/Math.Max(1,displayedWidth));
  }
  Renderer.Draw(project,e.Surface.Canvas,e.Info.Width,e.Info.Height,dragTarget>=0?dragTarget:selected,GuidesToggle.IsChecked==true,handleSize);
 }
 void Canvas_MouseDown(object s,MouseButtonEventArgs e)
 {
  if(e.ChangedButton!=MouseButton.Left)return;
  Canvas.Focus();var p=e.GetPosition(Canvas);resizeEdges=ResizeHit(p);
  if(resizeEdges!=ResizeEdges.None&&Selected is { } frame)
  {
   resizeOriginal=frame.Clone();resizeTemplate=project.Template;dragStart=p;dragging=true;panning=false;
   Canvas.CaptureMouse();e.Handled=true;return;
  }
  int hit=Hit(p);
  if(hit>=0)selected=hit;SyncUi();Canvas.InvalidateVisual();
  if(hit<0)return;
  if(e.ClickCount==2){Replace_Click(s,e);return;}
  if(Selected?.Image is null)return;
  dragging=true;panning=PanModeToggle.IsChecked==true||Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
  dragStart=p;dragCropX=Selected.CropX;dragCropY=Selected.CropY;Canvas.CaptureMouse();e.Handled=true;
 }
 void Canvas_MouseMove(object s,MouseEventArgs e)
 {
  var p=e.GetPosition(Canvas);
  if(!dragging){UpdateCursor(ResizeHit(p));return;}
  if(Selected is not { } f)return;
  if(e.LeftButton!=MouseButtonState.Pressed){FinishPan();return;}
  if(resizeOriginal is not null)
  {
   FrameResize.Apply(f,resizeOriginal,resizeEdges,(p.X-dragStart.X)/Math.Max(1,Canvas.ActualWidth),(p.Y-dragStart.Y)/Math.Max(1,Canvas.ActualHeight));
   StatusText.Text=$"Frame size: {f.W*project.CanvasWidth:0} x {f.H*project.CanvasHeight:0} px";
   Canvas.InvalidateVisual();return;
  }
  if(panning)
  {
   f.CropX=Math.Clamp(dragCropX+(p.X-dragStart.X)/Math.Max(1,Canvas.ActualWidth)*2,-1,1);
   f.CropY=Math.Clamp(dragCropY+(p.Y-dragStart.Y)/Math.Max(1,Canvas.ActualHeight)*2,-1,1);Canvas.InvalidateVisual();return;
  }
  // Measure in screen coordinates so the threshold stays consistent at any canvas zoom.
  var screenStart=Canvas.PointToScreen(dragStart);var screenNow=Canvas.PointToScreen(p);
  if(Math.Abs(screenNow.X-screenStart.X)<SystemParameters.MinimumHorizontalDragDistance&&Math.Abs(screenNow.Y-screenStart.Y)<SystemParameters.MinimumVerticalDragDistance)return;
  dragging=false;Canvas.ReleaseMouseCapture();
  try{DragDrop.DoDragDrop(Canvas,new DataObject(PanelDragFormat,f.Id),DragDropEffects.Move);}
  finally{dragTarget=-1;Canvas.InvalidateVisual();}
 }
 void FinishPan()
 {
  if(resizeOriginal is { } original)
  {
   bool resized=Selected is { } frame&&(Math.Abs(frame.X-original.X)>1e-10||Math.Abs(frame.Y-original.Y)>1e-10||Math.Abs(frame.W-original.W)>1e-10||Math.Abs(frame.H-original.H)>1e-10);
   resizeOriginal=null;resizeEdges=ResizeEdges.None;dragging=false;panning=false;Canvas.ReleaseMouseCapture();Canvas.Cursor=Cursors.Arrow;
   if(resized){project.Template="Custom";Changed("Frame resized");}return;
  }
  bool changed=dragging&&panning&&Selected is { } f&&(f.CropX!=dragCropX||f.CropY!=dragCropY);
  dragging=false;panning=false;Canvas.ReleaseMouseCapture();
  if(changed)Changed("Image repositioned");
 }
 void Canvas_MouseUp(object s,MouseButtonEventArgs e){if(e.ChangedButton==MouseButton.Left)FinishPan();}
 void Canvas_LostMouseCapture(object s,MouseEventArgs e){if(dragging)FinishPan();}
 ResizeEdges ResizeHit(Point p)
 {
  if(Selected is not {Hidden:false,Locked:false} f)return ResizeEdges.None;
  var a=Canvas.PointToScreen(new Point(f.X*Canvas.ActualWidth,f.Y*Canvas.ActualHeight));
  var b=Canvas.PointToScreen(new Point((f.X+f.W)*Canvas.ActualWidth,(f.Y+f.H)*Canvas.ActualHeight));var point=Canvas.PointToScreen(p);
  const double tolerance=7;
  if(point.X<a.X-tolerance||point.X>b.X+tolerance||point.Y<a.Y-tolerance||point.Y>b.Y+tolerance)return ResizeEdges.None;
  ResizeEdges edges=ResizeEdges.None;
  if(Math.Abs(point.X-a.X)<=tolerance)edges|=ResizeEdges.Left;else if(Math.Abs(point.X-b.X)<=tolerance)edges|=ResizeEdges.Right;
  if(Math.Abs(point.Y-a.Y)<=tolerance)edges|=ResizeEdges.Top;else if(Math.Abs(point.Y-b.Y)<=tolerance)edges|=ResizeEdges.Bottom;
  return edges;
 }
 void UpdateCursor(ResizeEdges edges)
 {
  bool horizontal=(edges&(ResizeEdges.Left|ResizeEdges.Right))!=0,vertical=(edges&(ResizeEdges.Top|ResizeEdges.Bottom))!=0;
  Canvas.Cursor=horizontal&&vertical?((edges.HasFlag(ResizeEdges.Left)==edges.HasFlag(ResizeEdges.Top))?Cursors.SizeNWSE:Cursors.SizeNESW):horizontal?Cursors.SizeWE:vertical?Cursors.SizeNS:Cursors.Arrow;
 }
 void Canvas_MouseWheel(object s,MouseWheelEventArgs e){if(Selected is { } f){f.Zoom=Math.Clamp(f.Zoom+(e.Delta>0?.08:-.08),1,4);Changed("Image zoom updated");e.Handled=true;}}
 int Hit(Point p){double nx=p.X/Math.Max(1,Canvas.ActualWidth),ny=p.Y/Math.Max(1,Canvas.ActualHeight);for(int i=project.Frames.Count-1;i>=0;i--){var f=project.Frames[i];if(f.Hidden)continue;if(nx<f.X||ny<f.Y||nx>f.X+f.W||ny>f.Y+f.H)continue;if(f.Shape=="ellipse"){double dx=(nx-f.X)/f.W*2-1,dy=(ny-f.Y)/f.H*2-1;if(dx*dx+dy*dy<=1)return i;}else if(f.Shape!="poly"||Inside(nx,ny,f.Points))return i;}return -1;}
 static bool Inside(double x,double y,List<PointD> pts){bool inside=false;for(int i=0,j=pts.Count-1;i<pts.Count;j=i++)if((pts[i].Y>y)!=(pts[j].Y>y)&&x<(pts[j].X-pts[i].X)*(y-pts[i].Y)/(pts[j].Y-pts[i].Y)+pts[i].X)inside=!inside;return inside;}
 void Canvas_DragEnter(object s,DragEventArgs e)=>Canvas_DragOver(s,e);
 void Canvas_DragOver(object s,DragEventArgs e)
 {
  int source=DragSource(e);bool internalDrag=source>=0;
  bool files=e.Data.GetData(DataFormats.FileDrop) is string[] paths&&paths.Any(IsImage);
  dragTarget=Hit(e.GetPosition(Canvas));
  bool allowed=dragTarget>=0&&(internalDrag&&source!=dragTarget||files);
  e.Effects=allowed?(internalDrag?DragDropEffects.Move:DragDropEffects.Copy):DragDropEffects.None;
  StatusText.Text=allowed?(internalDrag?$"Drop to swap with panel {dragTarget+1}":$"Drop into panel {dragTarget+1}"):"Move over another panel to place the image";
  Canvas.InvalidateVisual();e.Handled=true;
 }
 int DragSource(DragEventArgs e)=>e.Data.GetData(PanelDragFormat) is string id?project.Frames.FindIndex(f=>f.Id==id):-1;
 void Canvas_DragLeave(object s,DragEventArgs e){dragTarget=-1;StatusText.Text="Ready � Drag images between panels to rearrange";Canvas.InvalidateVisual();e.Handled=true;}
 void Canvas_Drop(object s,DragEventArgs e)
 {
  int target=Hit(e.GetPosition(Canvas)),source=DragSource(e);dragTarget=-1;e.Effects=DragDropEffects.None;
  if(target>=0)
  {
   if(source>=0&&project.SwapImages(source,target)){selected=target;Changed($"Moved image to panel {target+1}");e.Effects=DragDropEffects.Move;}
   else if(source<0&&e.Data.GetData(DataFormats.FileDrop) is string[] files){AddImages(files,target);e.Effects=DragDropEffects.Copy;}
  }
  Canvas.InvalidateVisual();e.Handled=true;
 }
 static bool IsImage(string p)=>new[]{".jpg",".jpeg",".png",".webp",".bmp",".gif",".tif",".tiff"}.Contains(Path.GetExtension(p).ToLowerInvariant());
 void Export_Click(object s,RoutedEventArgs e){var d=new SaveFileDialog{Filter="JPEG image (*.jpg)|*.jpg|PNG image (*.png)|*.png|WebP image (*.webp)|*.webp",DefaultExt=".jpg",FileName=project.Name};if(d.ShowDialog()==true)Export(d.FileName);}
 void QuickExport_Click(object s,RoutedEventArgs e){var dir=currentPath is null?Environment.GetFolderPath(Environment.SpecialFolder.MyPictures):Path.GetDirectoryName(currentPath)!;Export(Path.Combine(dir,$"{project.Name}.jpg"));}
 void Export(string path){try{StatusText.Text="Exporting.";Renderer.Export(project,path);StatusText.Text=$"Exported {Path.GetFileName(path)}";}catch(Exception ex){MessageBox.Show(this,$"The collage could not be exported.\n\n{ex.Message}","Export",MessageBoxButton.OK,MessageBoxImage.Error);}}
 void OnKeyDown(object s,KeyEventArgs e){if(e.Key==Key.Escape&&resizeOriginal is { } original){project.Frames[selected]=original;project.Template=resizeTemplate??project.Template;resizeOriginal=null;resizeEdges=ResizeEdges.None;dragging=false;panning=false;Canvas.ReleaseMouseCapture();Canvas.Cursor=Cursors.Arrow;StatusText.Text="Resize cancelled";SyncUi();Canvas.InvalidateVisual();e.Handled=true;return;}if(dragging)return;if(e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase)return;if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&e.Key==Key.S){SaveProject(false);e.Handled=true;}else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&e.Key==Key.Z){Undo_Click(s,e);e.Handled=true;}else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&e.Key==Key.Y){Redo_Click(s,e);e.Handled=true;}else if((e.Key==Key.Delete||e.Key==Key.Back)&&Selected is { } f&&f.Image is not null){f.Image=null;Changed("Image removed");e.Handled=true;}}
 void Autosave(){if(!dirty)return;try{Directory.CreateDirectory(Path.GetDirectoryName(ProjectIO.AutosavePath)!);ProjectIO.Save(ProjectIO.AutosavePath,project);}catch{}}
 public void CheckRecovery(){if(!File.Exists(ProjectIO.AutosavePath))return;if(MessageBox.Show(this,"CGM Collage found recovered work from an earlier session. Open it?","Recover project",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)OpenPath(ProjectIO.AutosavePath);else try{File.Delete(ProjectIO.AutosavePath);}catch{}}
 void OnClosing(object? s,CancelEventArgs e){if(!CanDiscard())e.Cancel=true;else{autosave.Stop();if(!dirty)try{File.Delete(ProjectIO.AutosavePath);}catch{}}}
}
