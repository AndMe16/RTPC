# Random Track Piece (RTPC)

A gameplay and editor mod for [**Zeepkist**](https://store.steampowered.com/app/1440670/Zeepkist/) that introduces controlled randomness into the track-building process.

Designed to challenge creativity and adaptability, RTPC forces builders to work with unexpected track pieces, turning chaos into fun and unique creations.

---

## 🎮 About the Game

**Zeepkist** is a Unity-based racing game that includes a powerful track editor, allowing players to design and share custom tracks.

Random Track Piece extends this editor by adding a randomization mechanic that changes how players approach building.

---

## ✨ Features

- 🎲 **One-key randomization**
  - Generate a random track piece with a single button press.

- 🧠 **Creativity-focused challenge**
  - Adapt your build to unexpected pieces instead of choosing them manually.

- 🧰 **Layered filtering system**
  - Full control over what pieces can (or cannot) appear.

- ⚙️ **Configurable controls**
  - Bind the randomizer key directly from the mod settings.

- 🔧 **Customizable piece pool**
  - Default set included, fully overrideable by the user.

---

## 🧩 Filtering System

The randomizer uses a **layered filtering pipeline**, applied in the following order:

### 1. Included Folders
Base folders that define the initial pool of available pieces.

### 2. Excluded Folders
Folders (and all subfolders) removed from the pool.

### 3. Excluded Blocks
Specific individual blocks removed after folder filtering.

### 4. Included Blocks
Blocks that are *always* included, even if excluded by previous filters.

This approach allows for precise and predictable control over the randomization process.

---

## 🛠️ Technical Overview

- **Language:** C#
- **Engine:** Unity
- **Type:** Editor / Gameplay Mod
- **Distribution:** mod.io
- **License:** MIT

The project focuses on clean integration with the existing editor workflow and avoids modifying core game logic.

---

## 📦 Installation

The mod is distributed via [**mod.io**](https://mod.io/g/zeepkist/m/rtpc#description).

1. Install the mod through the [ModkistRevamped](https://github.com/donderjoekel/ModkistRevamped) or mod.io.
2. Launch the game.
3. Open the track editor.
4. Bind the Randomizer key in the mod settings.
5. Start building 🚀

---

## 🤝 Contributing

Contributions, suggestions, and improvements are welcome.

If you want to:
- add new filtering options
- improve UX
- optimize selection logic

Feel free to open an issue or pull request.

---

## 📄 License

This project is licensed under the **MIT License**.
