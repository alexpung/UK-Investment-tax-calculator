#!/usr/bin/env python3
"""Sync UkCapitalGainAllowance.cs with the CGT annual exempt amounts published on GOV.UK.

Source page: https://www.gov.uk/guidance/capital-gains-tax-rates-and-allowances
The GOV.UK Content API (same page, JSON form) is used because it is more stable than scraping HTML.

Only adds missing tax years or corrects changed values; never removes historic years.
Prints a summary and writes `changed=true|false` (and `summary=...`) to $GITHUB_OUTPUT when present.
Optionally bumps the patch version in the .csproj when anything changed (--bump-version).
"""

from __future__ import annotations

import argparse
import html
import json
import os
import re
import sys
import urllib.request
from pathlib import Path

CONTENT_API_URL = "https://www.gov.uk/api/content/guidance/capital-gains-tax-rates-and-allowances"
REPO_ROOT = Path(__file__).resolve().parents[2]
ALLOWANCE_FILE = REPO_ROOT / "BlazorApp-Investment Tax Calculator" / "Model" / "UkTaxModel" / "UkCapitalGainAllowance.cs"
CSPROJ_FILE = REPO_ROOT / "BlazorApp-Investment Tax Calculator" / "InvestmentTaxCalculator.csproj"
BOM = "﻿"


def fetch_body() -> str:
    req = urllib.request.Request(CONTENT_API_URL, headers={"User-Agent": "cgt-allowance-updater", "Accept": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as resp:
        data = json.load(resp)
    return data["details"]["body"]


def strip_tags(fragment: str) -> str:
    return html.unescape(re.sub(r"<[^>]+>", "", fragment)).strip()


def parse_allowances(body: str) -> dict[int, int]:
    """Return {tax year start: allowance for individuals} from the annual exempt amount table."""
    for table in re.findall(r"<table.*?</table>", body, re.S | re.I):
        header = re.search(r"<thead.*?</thead>", table, re.S | re.I)
        if not header:
            continue
        headers = [strip_tags(h) for h in re.findall(r"<th.*?</th>", header.group(0), re.S | re.I)]
        if len(headers) < 2 or "tax year" not in headers[0].lower() or "annual exempt amount" not in headers[1].lower():
            continue
        result: dict[int, int] = {}
        for row in re.findall(r"<tr.*?</tr>", table, re.S | re.I):
            cells = [strip_tags(c) for c in re.findall(r"<td.*?</td>", row, re.S | re.I)]
            if len(cells) < 2:
                continue
            year = re.match(r"(\d{4})\s+to\s+(\d{4})", cells[0])
            amount = re.search(r"£\s*([\d,]+)", cells[1])
            if not year or not amount or int(year.group(2)) != int(year.group(1)) + 1:
                continue
            result[int(year.group(1))] = int(amount.group(1).replace(",", ""))
        if result:
            return result
    raise RuntimeError("Could not find the annual exempt amount table on the GOV.UK page; its layout may have changed.")


def read_text(path: Path) -> tuple[str, bool, str]:
    raw = path.read_bytes().decode("utf-8")
    has_bom = raw.startswith(BOM)
    if has_bom:
        raw = raw[1:]
    newline = "\r\n" if "\r\n" in raw else "\n"
    return raw.replace("\r\n", "\n"), has_bom, newline


def write_text(path: Path, text: str, has_bom: bool, newline: str) -> None:
    out = (BOM if has_bom else "") + text.replace("\n", newline)
    path.write_bytes(out.encode("utf-8"))


def update_allowance_file(published: dict[int, int]) -> list[str]:
    text, has_bom, newline = read_text(ALLOWANCE_FILE)
    block = re.search(r"(Allowances\s*\{\s*get;\s*\}\s*=\s*new\(\)\s*\{\n)(.*?)(\n(\s*)\};)", text, re.S)
    if not block:
        raise RuntimeError(f"Could not locate the Allowances dictionary in {ALLOWANCE_FILE}")

    entries = re.findall(r"\{\s*(\d{4})\s*,\s*([\d.]+)m\s*\}", block.group(2))
    current = {int(y): int(float(v)) for y, v in entries}
    indent = re.match(r"(\s*)", block.group(2)).group(1) or "        "

    changes: list[str] = []
    merged = dict(current)
    for year, amount in sorted(published.items()):
        if year not in current:
            changes.append(f"Added {year}/{str(year + 1)[2:]}: £{amount:,}")
        elif current[year] != amount:
            changes.append(f"Changed {year}/{str(year + 1)[2:]}: £{current[year]:,} -> £{amount:,}")
        merged[year] = amount

    if not changes:
        return changes

    new_body = ",\n".join(f"{indent}{{ {y}, {a}m }}" for y, a in sorted(merged.items()))
    text = text[: block.start(2)] + new_body + text[block.end(2):]
    write_text(ALLOWANCE_FILE, text, has_bom, newline)
    return changes


def bump_patch_version() -> str:
    text, has_bom, newline = read_text(CSPROJ_FILE)
    match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", text)
    if not match:
        raise RuntimeError(f"Could not find <Version> in {CSPROJ_FILE}")
    major, minor, patch = (int(g) for g in match.groups())
    new_version = f"{major}.{minor}.{patch + 1}"
    text = text[: match.start()] + f"<Version>{new_version}</Version>" + text[match.end():]
    write_text(CSPROJ_FILE, text, has_bom, newline)
    return new_version


def set_output(name: str, value: str) -> None:
    path = os.environ.get("GITHUB_OUTPUT")
    if not path:
        return
    with open(path, "a", encoding="utf-8") as fh:
        if "\n" in value:
            fh.write(f"{name}<<EOF_{name}\n{value}\nEOF_{name}\n")
        else:
            fh.write(f"{name}={value}\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bump-version", action="store_true", help="Bump the csproj patch version if allowances changed")
    parser.add_argument("--body-file", type=Path, help="Read the page body HTML from a file instead of fetching (for testing)")
    args = parser.parse_args()

    body = args.body_file.read_text(encoding="utf-8") if args.body_file else fetch_body()
    published = parse_allowances(body)
    print("Published allowances:", ", ".join(f"{y}: £{a:,}" for y, a in sorted(published.items())))

    changes = update_allowance_file(published)
    if not changes:
        print("UkCapitalGainAllowance.cs is already up to date.")
        set_output("changed", "false")
        return 0

    summary = "\n".join(f"- {c}" for c in changes)
    if args.bump_version:
        version = bump_patch_version()
        summary += f"\n- Bumped version to {version}"
    print(summary)
    set_output("changed", "true")
    set_output("summary", summary)
    return 0


if __name__ == "__main__":
    sys.exit(main())
