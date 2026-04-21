#!/usr/bin/env python3
"""
prepareUpdate.py — Patch-day package update script.

Queries NuGet for newer RGB.NET packages (pre-releases accepted) and Sharlayan
(stable only). If any packages are newer than what is installed, updates the
csproj(s), bumps the patch version, builds, runs the test suite, and stages a
git commit.

Usage:
    python prepareUpdate.py [--dry-run]
"""

from __future__ import annotations

import argparse
import json
import logging
import re
import subprocess
import sys
import urllib.request
from pathlib import Path

# ── Paths ─────────────────────────────────────────────────────────────────────
REPO_ROOT     = Path(__file__).resolve().parent   # git repo root (contains build.cmd)
MAIN_CSPROJ   = REPO_ROOT / "Chromatics" / "Chromatics.csproj"
TESTS_CSPROJ  = REPO_ROOT / "Chromatics.Tests" / "Chromatics.Tests.csproj"
ALL_CSPROJS   = [p for p in (MAIN_CSPROJ, TESTS_CSPROJ) if p.exists()]
BUILD_CMD     = "build.cmd"
TEST_CMD      = "test.cmd"

# ── Packages ──────────────────────────────────────────────────────────────────
RGB_NET_PACKAGES = [
    "RGB.NET.Core",
    "RGB.NET.Devices.Asus",
    "RGB.NET.Devices.CoolerMaster",
    "RGB.NET.Devices.Corsair",
    "RGB.NET.Devices.Logitech",
    "RGB.NET.Devices.Msi",
    "RGB.NET.Devices.Novation",
    "RGB.NET.Devices.OpenRGB",
    "RGB.NET.Devices.Razer",
    "RGB.NET.Devices.SteelSeries",
    "RGB.NET.Devices.Wooting",
    "RGB.NET.HID",
    "RGB.NET.Layout",
    "RGB.NET.Presets",
]

SHARLAYAN_PACKAGE = "Sharlayan"

# ── Logging ───────────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)


# ── NuGet ─────────────────────────────────────────────────────────────────────
def nuget_latest(package_id: str, *, prerelease: bool) -> str | None:
    """Return the latest published NuGet version for package_id, or None on failure."""
    url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=15) as r:
            versions: list[str] = json.loads(r.read())["versions"]
        if not prerelease:
            versions = [v for v in versions if "-" not in v]
        return versions[-1] if versions else None
    except urllib.error.HTTPError as exc:
        if exc.code == 404:
            return None  # package not on NuGet — silent skip
        log.warning(f"NuGet query for {package_id} returned HTTP {exc.code}")
        return None
    except Exception as exc:
        log.warning(f"NuGet query failed for {package_id}: {exc}")
        return None


# ── csproj helpers ────────────────────────────────────────────────────────────
def get_package_version(csproj: Path, package_id: str) -> str | None:
    """Return the installed PackageReference version of package_id, or None if absent."""
    content = csproj.read_text(encoding="utf-8")
    m = re.search(
        rf'<PackageReference\s+Include="{re.escape(package_id)}"\s+Version="([^"]+)"',
        content,
        re.IGNORECASE,
    )
    return m.group(1) if m else None


def is_local_reference(csproj: Path, package_id: str) -> bool:
    """Return True if package_id appears as a <Reference> (local DLL) rather than a PackageReference."""
    content = csproj.read_text(encoding="utf-8")
    return bool(re.search(
        rf'<Reference\s+Include="{re.escape(package_id)}"',
        content,
        re.IGNORECASE,
    ))


def bump_patch(csproj: Path) -> tuple[str, str]:
    """Increment the PATCH segment of <Version>M.N.P.0</Version>. Returns (old, new) display strings."""
    content = csproj.read_text(encoding="utf-8")
    m = re.search(r"<Version>(\d+\.\d+\.)(\d+)(\.0)</Version>", content)
    if not m:
        raise RuntimeError(f"<Version> element not found in {csproj}")
    old_full = f"{m.group(1)}{m.group(2)}{m.group(3)}"
    new_full  = f"{m.group(1)}{int(m.group(2)) + 1}{m.group(3)}"
    csproj.write_text(
        content.replace(f"<Version>{old_full}</Version>", f"<Version>{new_full}</Version>"),
        encoding="utf-8",
    )
    # Strip trailing .0 for human-readable display
    return old_full.rsplit(".", 1)[0], new_full.rsplit(".", 1)[0]


# ── dotnet / build helpers ────────────────────────────────────────────────────
def dotnet_add_package(csproj: Path, package_id: str, version: str) -> bool:
    """Run `dotnet add <csproj> package <id> --version <ver>`. Returns True on success."""
    result = subprocess.run(
        ["dotnet", "add", str(csproj), "package", package_id, "--version", version],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        log.error(f"dotnet add package failed for {package_id}:\n{result.stderr.strip()}")
        return False
    return True


def run_cmd(script: str) -> bool:
    """Run a .cmd script inside WORKBENCH_DIR, streaming output. Returns True on success."""
    result = subprocess.run(["cmd", "/c", script], cwd=str(REPO_ROOT))
    return result.returncode == 0


# ── Main ──────────────────────────────────────────────────────────────────────
def main() -> int:
    parser = argparse.ArgumentParser(description="Chromatics patch-day package updater")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Check for updates and report without applying any changes",
    )
    args = parser.parse_args()
    dry_run: bool = args.dry_run

    if dry_run:
        log.info("=== DRY RUN — no changes will be written ===")
    log.info("=== Chromatics patch-day update ===\n")

    # ── 1. Discover updates ────────────────────────────────────────────────────
    # Each entry: (package_id, installed_version, latest_version, [csproj_paths])
    updates: list[tuple[str, str, str, list[Path]]] = []

    log.info("Checking RGB.NET packages (pre-releases accepted)…")
    for pkg in RGB_NET_PACKAGES:
        latest = nuget_latest(pkg, prerelease=True)
        if latest is None:
            log.warning(f"  {pkg}: NuGet unreachable — skipping")
            continue

        targets: list[Path] = []
        installed: str | None = None
        for csproj in ALL_CSPROJS:
            ver = get_package_version(csproj, pkg)
            if ver is not None:
                targets.append(csproj)
                installed = ver

        if not targets:
            continue  # not referenced in any project

        if installed == latest:
            log.info(f"  {pkg}: {installed} — up to date")
        else:
            log.info(f"  {pkg}: {installed} → {latest}  ✓")
            updates.append((pkg, installed or "?", latest, targets))

    log.info("\nChecking Sharlayan (stable only)…")
    shar_targets: list[Path] = []
    shar_installed: str | None = None
    for csproj in ALL_CSPROJS:
        if is_local_reference(csproj, SHARLAYAN_PACKAGE):
            log.info(f"  {SHARLAYAN_PACKAGE}: local DLL in {csproj.name} — skipping NuGet update")
            continue
        ver = get_package_version(csproj, SHARLAYAN_PACKAGE)
        if ver is not None:
            shar_targets.append(csproj)
            shar_installed = ver

    if shar_targets:
        shar_latest = nuget_latest(SHARLAYAN_PACKAGE, prerelease=False)
        if shar_latest is None:
            log.warning(f"  {SHARLAYAN_PACKAGE}: NuGet unreachable — skipping")
        elif shar_installed == shar_latest:
            log.info(f"  {SHARLAYAN_PACKAGE}: {shar_installed} — up to date")
        else:
            log.info(f"  {SHARLAYAN_PACKAGE}: {shar_installed} → {shar_latest}  ✓")
            updates.append((SHARLAYAN_PACKAGE, shar_installed or "?", shar_latest, shar_targets))

    print()
    if not updates:
        log.info("All packages are up to date — nothing to do.")
        return 0

    log.info(f"{len(updates)} package(s) to update:")
    for pkg, old, new, targets in updates:
        proj_names = ", ".join(p.name for p in targets)
        log.info(f"  {pkg}: {old} → {new}  [{proj_names}]")

    if dry_run:
        log.info("\nDry run complete — no changes applied.")
        return 0

    # ── 2. Apply package updates ───────────────────────────────────────────────
    print()
    log.info("Applying package updates…")
    for pkg, old, new, targets in updates:
        for csproj in targets:
            log.info(f"  dotnet add {csproj.name}: {pkg} {new}")
            if not dotnet_add_package(csproj, pkg, new):
                log.error("Package update failed — aborting before any version bump.")
                return 1

    # ── 3. Bump patch version ─────────────────────────────────────────────────
    old_ver, new_ver = bump_patch(MAIN_CSPROJ)
    log.info(f"  Version: {old_ver} → {new_ver}")

    # ── 4. Build ───────────────────────────────────────────────────────────────
    print()
    log.info("Running build…")
    if not run_cmd(BUILD_CMD):
        log.error("Build FAILED — commit not staged. Fix errors and re-run.")
        return 1
    log.info("Build passed.")

    # ── 5. Test ────────────────────────────────────────────────────────────────
    print()
    log.info("Running tests…")
    if not run_cmd(TEST_CMD):
        log.error("Tests FAILED — commit not staged. Fix failures and re-run.")
        return 1
    log.info("Tests passed.")

    # ── 6. Stage commit ────────────────────────────────────────────────────────
    print()
    log.info("Staging changes…")
    subprocess.run(["git", "add", "-u"], cwd=str(REPO_ROOT), check=True)

    pkg_summary = ", ".join(
        f"{pkg} {old}→{new}" for pkg, old, new, _ in updates
    )
    commit_msg = f"Update {pkg_summary} (v{new_ver})"
    log.info(
        f"\nAll checks passed. Changes staged.\n"
        f"\nSuggested commit:\n"
        f"  git commit -m \"{commit_msg}\"\n"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
