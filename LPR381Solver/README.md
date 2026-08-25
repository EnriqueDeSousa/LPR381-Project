# LPR381 Solver (Primal Simplex + Revised Primal Simplex + Sensitivity Analysis)

## What this covers
This build implements three of the pieces from the project brief:
1. **Primal Simplex Algorithm** (tableau method, Big-M technique) — shows the canonical form and every tableau iteration.
2. **Revised Primal Simplex Algorithm** (product form of the inverse / price-out) — shows B⁻¹, the price vector, and priced-out reduced costs at every iteration.
3. **Sensitivity Analysis** — shadow prices, ranging of basic/non-basic variables, RHS ranging, adding a new activity, adding a new constraint, and duality (builds and solves the dual, checks strong/weak duality).

It reads the input `.txt` file format from the brief, and writes the canonical form + all iterations + the result to `output.txt`.

**Not included in this build** (out of scope of what was asked for this pass): Branch & Bound Simplex, Cutting Plane, Branch & Bound Knapsack, and the non-linear bonus. Integer/binary variables are currently solved as their **LP relaxation** (a binary variable gets an `x <= 1` upper-bound row added automatically) — so `sample_input.txt` (the Knapsack example) will solve and give you real numbers, just without the branch-and-bound integer-feasibility step. If your group needs those algorithms too, say so and they can be added the same way as everything else here.

## Project layout
```
LPR381Solver/
  LPR381Solver.csproj      <- the project file, builds "solve.exe"
  Program.cs                <- the menu you see when you run it
  Models/                   <- data classes: parsed model, standard form, results
  IO/                        <- reading the input file, writing the output text
  Solvers/                   <- Primal Simplex, Revised Simplex, Sensitivity Analysis, Duality
  Utils/                     <- small matrix-math helpers
  sample_input.txt           <- the Knapsack example straight from the brief
  sample_input_lp.txt        <- a plain LP (no bin/int) — better for demoing sensitivity analysis
```

---

## Software you need to install (do this first)

1. **Visual Studio 2022 (Community edition is free)**
   Download: https://visualstudio.microsoft.com/downloads/
   During install, tick the **".NET desktop development"** workload — that's the checkbox that gives you C# console apps.

2. **.NET 8 SDK** — Visual Studio 2022's installer usually offers to install this automatically when you tick the workload above. If not, get it separately here: https://dotnet.microsoft.com/download/dotnet/8.0 (choose the SDK, not just the runtime).

3. **Git** (to push to your GitHub repo)
   Download: https://git-scm.com/downloads
   During install you can leave everything on the default options.

4. **A GitHub Desktop app is optional but makes life much easier if typing git commands is intimidating**: https://desktop.github.com/ — with this you can just click "Add existing repository" and "Push" instead of typing commands. I'll give you both ways below.

You do **not** need to install anything else — no extra libraries, no NuGet packages. Everything here uses only the .NET base class library.

---

## How to open and run it in Visual Studio (step by step)

1. Unzip the folder you downloaded from me somewhere sensible, e.g. `C:\Users\<you>\Documents\LPR381Solver`.
2. Open **Visual Studio 2022**.
3. Click **"Open a project or solution"**, browse to the folder, and double-click `LPR381Solver.csproj`.
4. Visual Studio will load it as a project. Give it a few seconds to restore/index.
5. Press the green **▶ Start** button (or hit F5) at the top. A black console window will pop up — that's `solve.exe` running.
6. In the console:
   - Type `1` and press Enter → it asks for a file path. Type the full path to `sample_input.txt` (Visual Studio copies it next to the exe automatically if you set "Copy to Output Directory", but easiest is to just type the full path, e.g. `sample_input_lp.txt` if you're running from the project folder, or the full `C:\...\sample_input_lp.txt` path).
   - Type `2` and press Enter → runs Primal Simplex, prints every tableau, and writes `output.txt` next to the exe.
   - Type `4` → opens the Sensitivity Analysis sub-menu (shadow prices, ranging, duality, etc.) — only works after you've solved once.
   - Type `0` → exits.

If you'd rather not click through Visual Studio's UI, you can also do it from a terminal (Command Prompt / PowerPoint terminal / VS Code terminal) once the .NET SDK is installed:
```
cd path\to\LPR381Solver
dotnet run -- sample_input_lp.txt
```
`dotnet run` compiles and runs it in one go. `dotnet build` just compiles without running (useful to check for errors quickly).

---

## Getting this onto your group's GitHub repository

You said your group already created a repo for this project. Here's the plain-English version of getting these files into it.

### Option A — using GitHub Desktop (easiest if you're not comfortable with the command line)
1. Install GitHub Desktop (link above), sign in with the GitHub account that's a member of your group's repo.
2. In GitHub Desktop: **File → Clone Repository**, pick your group's repo from the list, and choose a folder on your PC to put it in (e.g. `C:\Users\<you>\Documents\LPR381Project`).
3. Copy all the files I gave you (the whole `LPR381Solver` folder, including subfolders like `Models`, `IO`, `Solvers`, `Utils`) into that cloned folder.
4. Go back to GitHub Desktop — it will automatically detect all the new files as "changes".
5. At the bottom-left, type a short commit message like `Add Primal Simplex, Revised Simplex, and Sensitivity Analysis`.
6. Click **Commit to main** (or whatever your branch is called).
7. Click **Push origin** at the top — this uploads your commit to GitHub so your groupmates can see it.

### Option B — using Git from the command line
Open a terminal (Command Prompt, PowerShell, or the terminal inside Visual Studio/VS Code) and run these one at a time:

```bash
# 1. Get a local copy of your group's repo (only do this once, skip if you already have it)
git clone https://github.com/YOUR-GROUP/YOUR-REPO-NAME.git
cd YOUR-REPO-NAME

# 2. Copy the LPR381Solver folder I gave you into this repo folder now (do this in File Explorer)

# 3. Tell git to track the new files
git add .

# 4. Save a snapshot with a message describing what you added
git commit -m "Add Primal Simplex, Revised Simplex, and Sensitivity Analysis"

# 5. Upload it to GitHub
git push
```

If `git push` asks you to log in, use your GitHub username and, instead of your password, a **Personal Access Token** (GitHub stopped accepting plain passwords for this) — GitHub will prompt you to create one the first time, or you can make one at https://github.com/settings/tokens.

### A couple of things to watch out for
- If a teammate already pushed changes to the repo since you cloned it, run `git pull` before you `git add`/`git commit`/`git push`, so you don't overwrite their work.
- If your group already has other C# files in the repo (e.g. someone started the Branch & Bound part separately), just make sure the folder names don't clash — you can put this whole thing in a subfolder like `LPR381Solver/` inside the repo so nothing overwrites anyone else's work, then someone merges the two `.csproj` setups later (or you keep them as two separate projects inside one Visual Studio **solution** — ask if you want help setting that up, it's a `.sln` file that just lists multiple projects).
- Don't commit the `bin/` and `obj/` folders that Visual Studio creates when you build — they're just temporary build output. Add a `.gitignore` file (I've included one) so Git skips them automatically.

---

## Quick test to make sure it's working
Run it with `sample_input_lp.txt` (the plain LP, not the binary knapsack one) — it should reach:
```
Optimal Z = 36.000
  x1 = 2.000
  x2 = 6.000
```
That's the textbook example (max 3x1+5x2 s.t. x1<=4, 2x2<=12, 3x1+2x2<=18), so if you get those numbers, the core algorithm is solid. Then go into the Sensitivity Analysis menu and try option 1 (shadow prices) — for this example you should get: constraint 1 = 0 (it isn't binding, x1=2 is under its limit of 4), constraint 2 = 1.5, constraint 3 = 1.
