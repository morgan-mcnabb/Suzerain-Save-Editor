# Roadmap

## v1.1 — Cross-Platform


- [ ] Linux support (x64)
- [ ] macOS support (x64 + Apple Silicon)
- [ ] Platform-aware save file detection (game stores saves in different locations per OS)
- [ ] Platform-aware atomic writes 
- [ ] Update user guide with Linux/macOS save paths

## v1.2 — Quality of Life

- [ ] Undo/Redo. per-field undo instead of just "Revert All"
- [ ] Recent files. remember last opened saves, skip the file picker
- [ ] Drag-and-drop. drop a .json onto the window to open it
- [ ] Change summary before save. diff dialog showing what you changed before committing

## v1.3 — Power User Features

- [ ] Favorites/Pinned fields. pin frequently edited fields for quick access
- [ ] Presets/Profiles. save a named set of edits (e.g. "Max Economy") and apply in one click
- [ ] Bulk edit in Advanced tab. select multiple variables, set them all at once
- [ ] Export/Import edits as patch files. share your edits without sharing the whole save
- [ ] Compare two saves. side-by-side diff showing what changed between saves
- [ ] Raw JSON viewer. read-only view of the raw save file

## v1.4 — Community & Distribution

- [ ] Schema contributions. make it easy for people to submit new field labels and descriptions
- [ ] Localization. support for multiple languages
- [ ] Auto-updater. opt-in check for new versions on launch
- [ ] Portable mode. config and logs next to the exe instead of AppData

---

Have a feature request? [Open an issue](https://github.com/morgan-mcnabb/Suzerain-Save-Editor/issues).
