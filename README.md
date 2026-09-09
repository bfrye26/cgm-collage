# CGM Collage

CGM Collage is a native Windows editor for building editorial headers, feature images, roundups, rankings, and social artwork from reusable multi-panel layouts.

![CGM Collage editor](reference-images/web-v1-ui.png)

## Highlights

- Native WPF desktop interface with a responsive SkiaSharp canvas
- 18 built-in editorial layouts
- Drag-and-drop image placement, panel selection, pan, zoom, rotate, flip, fill, and fit
- Brightness, contrast, saturation, opacity, borders, and shadows
- JPEG, PNG, and WebP export at common editorial and social sizes
- Undo/redo, autosave recovery, embedded images, and `.cgmcollage` project files
- Self-contained 64-bit Windows installer; no separate .NET or browser runtime required

## Install

Download `CGM-Collage-Setup.exe` from the latest GitHub release and run it. Installation is per user and does not require administrator access.

## Build

Install the .NET 10 SDK and Inno Setup, then run:

```powershell
./build.ps1
```

The self-contained application and installer are written to `artifacts/`.

## Keyboard and mouse

- Drop image files on the canvas to fill panels
- Drag an image to reposition its crop
- Use the mouse wheel over the canvas to zoom the selected image
- `Delete` or `Backspace` clears the selected panel
- `Ctrl+S` saves the project
- `Ctrl+Z` / `Ctrl+Y` undo and redo

## Privacy

Images and project data remain on the computer. The application has no telemetry or cloud processing.

## License

MIT
