## Suzerain Save Editor — Quick Start Guide

### Step 1: Find Your Save Files

Your Suzerain saves are located at:

    %LocalAppData%Low\Torpor Games\Suzerain\

To get there: press `Win+R`, paste that path, and hit Enter. You'll see `.json` files. Those are your saves.

### Step 2: Open a Save

Launch the editor and click the **Open Save File** button in the center (or `Ctrl+O`). Navigate to the folder above and pick the `.json` save you want to edit.

### Step 3: Browse & Edit

Once loaded, you'll see four tabs across the top:

| Tab | What's in it |
|---|---|
| **General** | Save metadata: campaign name, turn number, game version |
| **Sordland** | Base game variables: budget, faction opinions, diplomacy, regional economies |
| **Rizia** | DLC variables: military units, royal budget, house influence, character relations |
| **Advanced** | Full hierarchical tree of all ~12,000+ internal variables |

Each field has an appropriate editor:
- **Toggles** for Yes/No values
- **Text boxes** for numbers and strings (with min/max validation)
- **Dropdowns** for preset options

Modified fields get an **orange dot** so you can see what you've changed. The status bar at the bottom tracks your total unsaved changes.

### Step 4: Use Search

The search box (top-right) filters fields across all tabs in real-time. Just start typing and it will match on field names, IDs, and descriptions.

### Step 5: Save Your Changes

Hit **Save** (`Ctrl+S`). Here's what happens automatically:

1. All fields are validated. If anything is invalid, the save is blocked and you'll see the error in red
2. A **timestamped backup** of your original file is created in a `backups/` folder next to your save (e.g. `save.json.bak.20260224-143052`)
3. The edited file is written atomically so there is no save corruption risk (hopefully)

Backups are created **every time** you save, so you can always roll back.

### Step 6: Revert (If Needed)

Changed something you didn't mean to? Click **Revert All** to undo every edit and go back to the last saved state.

---

### Tips

- **Backups are automatic**. You don't need to manually copy your save file first, but it never hurts
- **The Advanced tab** has a tree sidebar on the left for navigating by category, with cards showing sub-categories. Use this if you want to dig into variables not exposed in the main tabs
- **Validation is live**. You'll see red error text immediately if you enter something invalid (like a string where a number should be)
- **Unknown fields are preserved**. The editor won't delete data it doesn't recognize, so your save stays intact