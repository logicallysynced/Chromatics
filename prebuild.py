#!/usr/bin/env python3
"""
prebuild.py — Sharlayan reference resolver.

Invoked by build.cmd (and publish.py transitively) before `dotnet build`.
Decides whether Chromatics should pull Sharlayan from NuGet or use a local
DLL in `Build Dependencies/Sharlayan/`, then writes Sharlayan.Reference.props
which both Chromatics.csproj and Chromatics.Tests.csproj import.

Policy
------
  Release builds
    • Always use the latest stable NuGet Sharlayan (version >= 9.0.0).
    • The local DLL is ignored entirely — Release shipping must be
      reproducible from source feeds.

  Debug builds
    • Pick the best of {local DLL, latest stable NuGet, latest prerelease
      NuGet} by semantic version.
    • Between equal versions, prefer local DLL > stable NuGet > prerelease.
    • Otherwise the higher version wins (prerelease can beat stable if
      its M.N.P is strictly higher).

Usage
-----
    python prebuild.py --configuration Release|Debug
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
from typing import NamedTuple

# ── Paths ─────────────────────────────────────────────────────────────────────
REPO_ROOT     = Path(__file__).resolve().parent
LOCAL_DLL     = REPO_ROOT / "Build Dependencies" / "Sharlayan" / "Sharlayan.dll"
LOCAL_DIR     = LOCAL_DLL.parent
PROPS_FILE    = REPO_ROOT / "Sharlayan.Reference.props"

PACKAGE_ID    = "Sharlayan"
FLOOR_VERSION = (9, 0, 0)

# ── Logging ───────────────────────────────────────────────────────────────────
logging.basicConfig(level=logging.INFO, format="[Sharlayan] %(message)s")
log = logging.getLogger(__name__)


# ── Semver ────────────────────────────────────────────────────────────────────
class SemVer(NamedTuple):
    major: int
    minor: int
    patch: int
    is_stable: int  # 1 = release, 0 = prerelease (so release sorts higher at same M.N.P)
    prerelease: str
    raw: str


def parse_semver(v: str) -> SemVer | None:
    """Parse M.N.P[-prerelease][+build] into a sortable tuple. Returns None on failure."""
    if not v:
        return None
    # Strip "+build" metadata, it is ignored for ordering.
    core = v.split("+", 1)[0]
    m = re.match(r"^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$", core)
    if not m:
        return None
    prerelease = m.group(4) or ""
    return SemVer(
        major=int(m.group(1)),
        minor=int(m.group(2)),
        patch=int(m.group(3)),
        is_stable=0 if prerelease else 1,
        prerelease=prerelease,
        raw=v,
    )


def meets_floor(sv: SemVer) -> bool:
    return (sv.major, sv.minor, sv.patch) >= FLOOR_VERSION


# ── NuGet ─────────────────────────────────────────────────────────────────────
def nuget_versions(package_id: str) -> list[str]:
    url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=15) as r:
            return list(json.loads(r.read())["versions"])
    except Exception as exc:
        log.warning(f"NuGet query failed for {package_id}: {exc}")
        return []


def latest(versions: list[str], *, prerelease: bool) -> SemVer | None:
    parsed = [p for v in versions if (p := parse_semver(v)) is not None]
    parsed = [p for p in parsed if meets_floor(p)]
    if not prerelease:
        parsed = [p for p in parsed if p.is_stable]
    else:
        parsed = [p for p in parsed if not p.is_stable]
    parsed.sort()
    return parsed[-1] if parsed else None


# ── Local DLL ─────────────────────────────────────────────────────────────────
def local_dll_version() -> SemVer | None:
    if not LOCAL_DLL.exists():
        return None
    try:
        result = subprocess.run(
            ["powershell", "-NoProfile", "-Command",
             f"(Get-Item -LiteralPath '{LOCAL_DLL}').VersionInfo.ProductVersion"],
            capture_output=True, text=True, check=True,
        )
        raw = result.stdout.strip()
        return parse_semver(raw)
    except Exception as exc:
        log.warning(f"Could not read local DLL version: {exc}")
        return None


# ── Decision ──────────────────────────────────────────────────────────────────
class Decision(NamedTuple):
    source: str            # "NuGet" | "Local"
    version: str           # semver string used in the props
    props_body: str        # full XML to write into PROPS_FILE


def render_nuget_props(version: str) -> str:
    return (
        '<Project>\n'
        '  <PropertyGroup>\n'
        f'    <SharlayanSource>NuGet</SharlayanSource>\n'
        f'    <SharlayanVersion>{version}</SharlayanVersion>\n'
        '  </PropertyGroup>\n'
        '  <ItemGroup>\n'
        f'    <PackageReference Include="{PACKAGE_ID}" Version="{version}" />\n'
        '  </ItemGroup>\n'
        '  <Target Name="SharlayanSourceBanner" BeforeTargets="Build">\n'
        '    <Message Importance="high" Text="[Sharlayan] Using $(SharlayanSource) $(SharlayanVersion)" />\n'
        '  </Target>\n'
        '</Project>\n'
    )


def render_local_props(version: str) -> str:
    # The local DLL brings transitive deps that NuGet would normally resolve —
    # copy them to output so the app can load Sharlayan at runtime.
    return (
        '<Project>\n'
        '  <PropertyGroup>\n'
        f'    <SharlayanSource>Local</SharlayanSource>\n'
        f'    <SharlayanVersion>{version}</SharlayanVersion>\n'
        '  </PropertyGroup>\n'
        '  <ItemGroup>\n'
        f'    <Reference Include="{PACKAGE_ID}">\n'
        '      <HintPath>$(MSBuildThisFileDirectory)Build Dependencies\\Sharlayan\\Sharlayan.dll</HintPath>\n'
        '      <Private>true</Private>\n'
        '    </Reference>\n'
        '    <None Include="$(MSBuildThisFileDirectory)Build Dependencies\\Sharlayan\\*.dll"\n'
        '          Exclude="$(MSBuildThisFileDirectory)Build Dependencies\\Sharlayan\\Sharlayan.dll">\n'
        '      <Link>%(Filename)%(Extension)</Link>\n'
        '      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\n'
        '      <Visible>false</Visible>\n'
        '    </None>\n'
        '  </ItemGroup>\n'
        '  <Target Name="SharlayanSourceBanner" BeforeTargets="Build">\n'
        '    <Message Importance="high" Text="[Sharlayan] Using $(SharlayanSource) DLL $(SharlayanVersion)" />\n'
        '  </Target>\n'
        '</Project>\n'
    )


def decide(configuration: str,
           stable: SemVer | None,
           prerelease: SemVer | None,
           local: SemVer | None) -> Decision:
    if configuration.lower() == "release":
        if stable is None:
            raise RuntimeError(
                "Release builds require a stable Sharlayan NuGet >= 9.0.0, but none was found."
            )
        return Decision("NuGet", stable.raw, render_nuget_props(stable.raw))

    # Debug: rank candidates. Tie-break (same semver key) by source preference:
    # local > stable > prerelease.
    candidates: list[tuple[SemVer, int, str]] = []
    if local is not None:
        candidates.append((local, 2, "local"))
    if stable is not None:
        candidates.append((stable, 1, "stable"))
    if prerelease is not None:
        candidates.append((prerelease, 0, "prerelease"))

    if not candidates:
        raise RuntimeError(
            "No Sharlayan source available for Debug build: "
            "no local DLL and no NuGet version meets the >= 9.0.0 floor."
        )

    candidates.sort(key=lambda t: (t[0], t[1]))
    chosen_sv, _, kind = candidates[-1]
    if kind == "local":
        return Decision("Local", chosen_sv.raw, render_local_props(chosen_sv.raw))
    return Decision("NuGet", chosen_sv.raw, render_nuget_props(chosen_sv.raw))


# ── Main ──────────────────────────────────────────────────────────────────────
def main() -> int:
    parser = argparse.ArgumentParser(description="Resolve Sharlayan reference for the build.")
    parser.add_argument("--configuration", default="Release",
                        help="Build configuration (Release or Debug).")
    args = parser.parse_args()
    config = args.configuration

    log.info(f"Configuration: {config}")

    versions = nuget_versions(PACKAGE_ID)
    stable     = latest(versions, prerelease=False)
    prerelease = latest(versions, prerelease=True)
    local      = local_dll_version()

    log.info(f"NuGet stable:     {stable.raw if stable else '(none)'}")
    log.info(f"NuGet prerelease: {prerelease.raw if prerelease else '(none)'}")
    log.info(f"Local DLL:        {local.raw if local else '(not found)'}")

    decision = decide(config, stable, prerelease, local)
    log.info(f"Decision:         {decision.source} {decision.version}")

    # Only write when content actually changes, to keep git noise down.
    new_body = decision.props_body
    if PROPS_FILE.exists() and PROPS_FILE.read_text(encoding="utf-8") == new_body:
        log.info("Sharlayan.Reference.props unchanged.")
    else:
        PROPS_FILE.write_text(new_body, encoding="utf-8")
        log.info(f"Wrote {PROPS_FILE.name}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
