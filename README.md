# SimplePhotos

SimplePhotos is a small desktop photo browser built with Avalonia and .NET 9.

It is designed for a simple workflow:

- open a folder
- browse the images in that folder
- move into subfolders
- open a photo in a larger overlay view
- navigate quickly with mouse or keyboard

## What The App Does

When you open a folder, the app shows:

- direct subfolders as cards at the top
- image files from the current folder in a grid below
- the current path in the top bar

The app does not build a library database and it does not scan folders recursively. It works directly on the folder you open and lets you move through the folder structure from there.

Preview images are loaded for the grid first. The full image is loaded only when you open a photo.

## Supported Image Formats

The current implementation supports:

- `.png`
- `.jpg`
- `.jpeg`

## How To Use

### Open A Folder

Use `Open Library` from the top menu or the `Open Directory` button on the empty state screen.

### Browse Folders

Subfolders appear as cards above the photo grid. Click a folder card to open it.

To go up one level:

- click the current path in the top bar
- or press `Backspace`

### View A Photo

Click a photo thumbnail to open it in a larger overlay.

To close the overlay:

- click the photo or the dark background
- or press `Escape`

### Close The Current Library

Use `Close Library` in the top menu to clear the current folder view.

## Keyboard Controls

The app includes keyboard navigation for the photo grid.

- `Enter` or `Space`: open the folder picker if no folder is loaded, or open the currently selected photo
- `Enter` again: close the currently open photo
- `Escape`: close the currently open photo
- `Left` and `Right`: move the current selection by one photo
- `Up` and `Down`: move the current selection by one row
- `Backspace`: open the parent folder

Keyboard navigation currently applies to photos, not folder cards.

## Installation

At the moment I provide executables for easy access.

## Notes

This is a lightweight folder-based image viewer. It is meant for quick local browsing rather than advanced photo management.
