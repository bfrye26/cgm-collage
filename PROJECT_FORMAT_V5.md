# `.cgmcollage` Project Format v5

The current native source stores projects as JSON. Source images can be embedded directly as base64 data URLs inside frame records.

## Project

- `format` - integer, currently 5
- `appVersion` - app version string
- `name` - project name
- `canvasWidth` / `canvasHeight` - integer output canvas dimensions
- `background` - hex colour
- `gapColor` - hex colour
- `gap` - numeric panel gap
- `exportQuality` - JPEG-like quality integer
- `targetKB` - optional target file size, 0 disables
- `frames` - array of Frame objects
- `texts` - array of TextLayer objects

## Frame

- `id`, `name`
- `x`, `y`, `w`, `h` - normalized canvas geometry
- `shape` - `rect`, `ellipse`, or `poly`
- `points` - normalized polygon points when shape is `poly`
- `image` - optional EmbeddedImage
- `cropX`, `cropY` - normalized image offsets
- `zoom`
- `rotation`
- `brightness` - -1 to 1, 0 neutral
- `contrast` - 0 to 2, 1 neutral
- `saturation` - 0 to 2, 1 neutral
- `opacity`
- `borderWidth`, `borderColor`
- `shadow`, `shadowOpacity`, `shadowDistance`, `shadowAngle`
- `fitMode` - `fill` or `fit`
- `locked`, `hidden`

## EmbeddedImage

- `name`
- `mime`
- `data` - base64 payload / data URL content

## TextLayer

- `id`
- `text`
- `x`, `y`, `w`, `h`
- `fontSize`
- `fontFamily`
- `bold`
- `color`
- `align`
- `shadow`
- `hidden`

The exact Go definitions live in `current-native-go/src/model.go` and should be treated as the canonical implementation until a new desktop schema is introduced.
