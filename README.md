# ImageHandler ![workflow badge](../../actions/workflows/ci.yml/badge.svg)
Support for loading, saving, capturing and displaying images in FitNesse/FitSharp tests.

These are used by FitNesse fixtures such as FitNesseFitSharpUiAutomation to create screenshots and render images.

## Class Snapshot

This class is used to capture a screenshot of a window or control.

### Constructors

```csharp
public Snapshot(byte[] byteArray)
```
Create a snapshot from a byte array containing an image.

```csharp
public Snapshot(string input)
```
Create a snapshot by first trying to base64 decode the input, and if that doesn't work interpreting the string as a path to an image file to be opened.

### Properties

```csharp
public byte[] ByteArray { get; private set; }
```
auto-property returning the byte array containing the image.

```csharp
public string MimeType
```
The mime type of the image (e.g. `image/png`).

```csharp
public string Rendering
```
The rendering of the image for use in a web page: `<img src="data:image/png;base64,<base64 encoded byte array>" />`

```csharp
public string ToBase64
```
Base64 encoded image.

### Methods
```csharp
public static Snapshot CaptureScreen(Rectangle bounds)
```
Capture a screenshot of the rectangle.

```csharp
public static Snapshot Parse(string input)
```
Function to enable usage of Snapshot in FitNesse tables.

```csharp
public string Save(string path)
```
Save the image to the specified path.

```csharp
public double SimilarityTo(Snapshot other)
```
Calculate the similarity of this image to another image.
This is done by reducing the size of both images and then comparing the pixels.
Returns a number between 0 and 1, where 0 means the images are completely different and 1 means they are identical
up to a margin of 8 in color distance per pixel.

```csharp
public override string ToString()
```
return the label of the Snapshot class (used in FitNesse tables).
