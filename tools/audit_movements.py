#!/usr/bin/env python3
"""Checks the TMDB ids of the built-in movements against Wikidata.

Reads the people in DefaultMovements.cs and the films in MovementFilms.json, looks each id up on
Wikidata (properties P4985 for people, P4947 for films) and reports the ids Wikidata does not know
and the names that differ from its label. Spelling differences are often harmless romanizations,
so they only warn; an unknown id fails the run. Standard library only, no API key needed.

    python3 tools/audit_movements.py
"""
import csv
import io
import json
import re
import sys
import time
import unicodedata
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONFIGURATION = ROOT / "src" / "Jellyfin.Plugin.Cinematheque" / "Configuration"
ENDPOINT = "https://query.wikidata.org/sparql"
USER_AGENT = "jellyfin-plugin-cinematheque-audit (https://github.com/gauthier-se/jellyfin-plugin-cinematheque)"
BATCH = 200

# People Wikidata has no TMDB id for, checked by hand against their filmography on themoviedb.org.
CONFIRMED_ON_TMDB = {
    "146295": "Yim Ho",
    "1092246": "Tao Te-chen, credited as Jim Tao on In Our Time",
    "1402406": "Peter Yung",
}


def key(text):
    """Compares names the way the plugin does: no diacritics, no punctuation."""
    text = unicodedata.normalize("NFKD", text)
    return re.sub(r"[^a-z0-9]", "", "".join(c for c in text if not unicodedata.combining(c)).lower())


def sparql(query):
    data = urllib.parse.urlencode({"query": query}).encode()
    for attempt in range(6):
        request = urllib.request.Request(ENDPOINT, data=data, headers={"Accept": "text/csv", "User-Agent": USER_AGENT})
        try:
            with urllib.request.urlopen(request, timeout=100) as response:
                rows = list(csv.DictReader(io.StringIO(response.read().decode("utf-8"))))
            time.sleep(2)  # The endpoint rate-limits bursts.
            return rows
        except urllib.error.HTTPError as error:
            if error.code not in (429, 500, 502, 503, 504):
                raise
            time.sleep(int(error.headers.get("Retry-After") or 0) or 10 * (attempt + 1))
    raise RuntimeError("Wikidata is not answering")


def lookup(property_id, ids):
    """Maps each id to the labels of the Wikidata items that carry it."""
    labels = {}
    ids = sorted(ids, key=int)
    for start in range(0, len(ids), BATCH):
        values = " ".join(f'"{i}"' for i in ids[start:start + BATCH])
        rows = sparql(
            f"SELECT ?id ?itemLabel WHERE {{ VALUES ?id {{ {values} }} ?item wdt:{property_id} ?id . "
            'SERVICE wikibase:label { bd:serviceParam wikibase:language "en,mul,fr". } }'
        )
        for row in rows:
            labels.setdefault(row["id"], set()).add(row["itemLabel"])
    return labels


def people():
    source = (CONFIGURATION / "DefaultMovements.cs").read_text(encoding="utf-8")
    found = {}
    for name, tmdb_id in re.findall(r'"([^"]+) \(tmdb:(\d+)\)"', source):
        found.setdefault(tmdb_id, set()).add(name)
    return found


def films():
    data = json.loads((CONFIGURATION / "MovementFilms.json").read_text(encoding="utf-8"))
    found = {}
    for movement in data.values():
        for film in movement.get("Essentials", []):
            found.setdefault(film["TmdbId"], set()).add(film["Title"])
        for tmdb_id in movement.get("TmdbIds", []) + movement.get("ExcludedTmdbIds", []):
            found.setdefault(tmdb_id, set())
    return found


def report(kind, expected, labels):
    unknown = sorted(set(expected) - set(labels) - set(CONFIRMED_ON_TMDB), key=int)
    renamed = [
        (tmdb_id, sorted(names), sorted(labels[tmdb_id]))
        for tmdb_id, names in expected.items()
        if names and tmdb_id in labels and not {key(n) for n in names} & {key(l) for l in labels[tmdb_id]}
    ]
    print(f"{kind}: {len(expected)} ids, {len(unknown)} unknown to Wikidata, {len(renamed)} named differently")
    for tmdb_id in unknown:
        print(f"  unknown  tmdb:{tmdb_id} {sorted(expected[tmdb_id])}")
    for tmdb_id, names, wikidata in renamed:
        print(f"  differs  tmdb:{tmdb_id} {names} / {wikidata}")
    return len(unknown)


def main():
    expected_people = people()
    expected_films = films() if (CONFIGURATION / "MovementFilms.json").exists() else {}
    failures = report("People", expected_people, lookup("P4985", expected_people))
    if expected_films:
        failures += report("Films", expected_films, lookup("P4947", expected_films))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
