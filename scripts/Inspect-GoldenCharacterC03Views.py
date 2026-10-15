#!/usr/bin/env python3
"""Independent draft review checks for C03 camera-view identity.

Not an artistic, legal, Unity or owner approval system. Exit nonzero when native
projections are demonstrably reused, even if the normal raster validator passes.
"""
import argparse
import importlib.util
import json
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent


def load_renderer(path):
    sys.path.insert(0, str(path.parent))
    spec = importlib.util.spec_from_file_location('c03_review_renderer', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def diff_on_silhouette(a, b):
    """RGB/alpha pixel difference among the union of visibly occupied pixels."""
    if len(a) != len(b) or len(a) % 4:
        raise ValueError('Mismatched RGBA images')
    occupied = changed = 0
    for i in range(0, len(a), 4):
        x, y = a[i:i+4], b[i:i+4]
        if x[3] or y[3]:
            occupied += 1
            changed += x != y
    return round(changed / occupied, 4) if occupied else 0.0


def any_alpha(layer):
    return any(layer[i] for i in range(3, len(layer), 4))


def inspect(module, min_rear_difference=0.25):
    if not (0.0 < min_rear_difference < 1.0):
        raise ValueError('Invalid similarity threshold')
    findings = []
    data = {}
    for style in module.STYLES:
        front = module.draw(style, 'front')
        rear = module.draw(style, 'back')
        delta = diff_on_silhouette(front.merged(), rear.merged())
        data[style] = {'front_to_back_changed_occupied_ratio': delta}
        if any_alpha(rear.parts['face']):
            findings.append(f'{style}: rear projection contains visible face features')
        if any_alpha(rear.parts['hair_front']):
            findings.append(f'{style}: rear projection incorrectly reuses front-fringe layer')
        if delta < min_rear_difference:
            findings.append(f'{style}: rear differs from front on only {delta:.1%} of occupied pixels; distinct anatomy not demonstrated')
    return {'state': 'REJECTED_VISUAL_PROJECTION' if findings else 'STRUCTURAL_HEURISTICS_ONLY_NOT_ART_APPROVAL',
            'metric_scope': 'occupied native 64x80 RGBA pixel union',
            'rear_difference_floor': min_rear_difference,
            'views': data,
            'findings': findings,
            'note': 'Even passing these heuristics does not prove cute, original, Vietnamese or production-ready art.'}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--renderer', type=Path, default=SCRIPT_DIR / 'Render-GoldenCharacterC03.py')
    parser.add_argument('--report', type=Path)
    args = parser.parse_args()
    report = inspect(load_renderer(args.renderer))
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n', encoding='utf8')
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 1 if report['findings'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
