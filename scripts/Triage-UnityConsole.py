#!/usr/bin/env python3
"""Classify selected Unity Console messages without suppressing any diagnostics.

This is a log-triage aid, NOT a Unity compilation, test, render or release gate.
Unknown errors/warnings remain explicit, and package warnings still require
appropriate on-device/render checks before they can be accepted for release.
"""

import argparse
import json
from pathlib import Path
import re
import sys


_SHADER_WARNING = re.compile(
    r"^Shader warning in 'TraceVirtualOffset': .*\[-Wconversion\] at (.+)$"
)
_SHADER_PACKAGE_PATH = re.compile(
    r"(?:^|/)Library/PackageCache/com\.unity\.render-pipelines\.core@[^/]+/"
    r"ShaderLibrary/Sampling/(?:Fibonacci|Hammersley|Sampling)\.hlsl\(\d+\)"
    r"(?: \(on [^)]+\))?$"
)
_LOCALIZATION_UPDATE = re.compile(
    r"^Startup Life: imported (\d+) Vietnamese dashboard entries\.$"
)
_UNKNOWN_ERROR = re.compile(
    r"^(?:Error\b|Shader error\b|\S+(?:Exception|Error):|\[Error\])"
    r"|\berror CS\d{4}\b",
    re.IGNORECASE,
)
_UNKNOWN_WARNING = re.compile(
    r"^(?:Shader warning\b|Warning\b|\[Warning\])"
    r"|\bwarning CS\d{4}\b",
    re.IGNORECASE,
)


def classify_line(line):
    """Return a non-suppressing classification or None for non-diagnostic lines."""
    message = line.strip()
    if not message:
        return None
    match = _SHADER_WARNING.match(message)
    if match:
        path = match.group(1).replace('\\', '/')
        if _SHADER_PACKAGE_PATH.search(path):
            return 'external_urp_precision_warning'
        return 'unclassified_warning'
    if (message.startswith("Error reason is 'NoSubscription'")
            and 'https://generators.ai.unity.com' in message):
        return 'external_unity_ai_subscription_error'
    if _LOCALIZATION_UPDATE.fullmatch(message):
        return 'localization_table_modified'
    if _UNKNOWN_ERROR.search(message):
        return 'unclassified_error'
    if _UNKNOWN_WARNING.search(message):
        return 'unclassified_warning'
    return None


def analyze(lines):
    findings = []
    counts = {
        'external_urp_precision_warning': 0,
        'external_unity_ai_subscription_error': 0,
        'localization_table_modified': 0,
        'unclassified_error': 0,
        'unclassified_warning': 0,
    }
    for line_number, line in enumerate(lines, 1):
        category = classify_line(line)
        if category is not None:
            counts[category] += 1
            findings.append({
                'line': line_number,
                'category': category,
                'message': line.strip(),
            })
    if counts['unclassified_error']:
        status = 'UNCLASSIFIED_ERRORS_REQUIRE_INVESTIGATION'
    elif any(counts.values()):
        status = 'REVIEW_REQUIRED'
    else:
        status = 'NO_RECOGNIZED_DIAGNOSTICS'
    return {'status': status, 'counts': counts, 'findings': findings,
            'unity_verified': False}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('log', type=Path, help='Path to a saved Unity Console/Editor log')
    parser.add_argument('--format', choices=('text', 'json'), default='text')
    parser.add_argument('--fail-on-unclassified-error', action='store_true',
                        help='Exit 3 when a log has an unclassified error')
    args = parser.parse_args(argv)
    try:
        with args.log.open(encoding='utf-8', errors='replace') as handle:
            report = analyze(handle)
    except OSError as error:
        parser.error(str(error))
    if args.format == 'json':
        print(json.dumps(report, indent=2, ensure_ascii=False))
    else:
        print('Unity Console triage:', report['status'], '(not Unity verification)')
        for category, count in report['counts'].items():
            if count:
                print(f'  {category}: {count}')
        for finding in report['findings']:
            print(f"  L{finding['line']}: {finding['category']}: {finding['message']}")
    return 3 if args.fail_on_unclassified_error and report['counts']['unclassified_error'] else 0


if __name__ == '__main__':
    sys.exit(main())
