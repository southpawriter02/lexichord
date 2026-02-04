#!/usr/bin/env python3
# =============================================================================
# File: check-performance-regression.py
# Project: Lexichord
# Description: Compares benchmark results against baseline for regression detection.
# =============================================================================
# v0.5.8b: CI integration script for performance regression detection.
#   - Compares P95 latency against baseline
#   - Configurable threshold (default 10%)
#   - Exits non-zero on regression
# =============================================================================

"""
Performance Regression Detection Script

Compares current benchmark results (JSON) against a baseline file and flags
any regressions exceeding the specified threshold.

Usage:
    python check-performance-regression.py BASELINE CURRENT [--threshold PCT]

Example:
    python check-performance-regression.py \
        benchmarks/baselines/performance-baselines.json \
        BenchmarkDotNet.Artifacts/results/SearchBenchmarks-report.json \
        --threshold 10
"""

import argparse
import json
import sys
from pathlib import Path


def load_json(path: Path) -> dict:
    """Load and parse a JSON file."""
    with open(path) as f:
        return json.load(f)


def find_baseline(baseline_data: dict, benchmark_name: str, params: dict) -> dict | None:
    """Find matching baseline entry by benchmark name and parameters."""
    for benchmark in baseline_data.get("Benchmarks", []):
        if benchmark.get("Method") != benchmark_name:
            continue

        # Match parameters
        baseline_params = {
            p["Name"]: p["Value"]
            for p in benchmark.get("Parameters", "").split(", ")
            if "=" in p
            for p in [{"Name": p.split("=")[0], "Value": p.split("=")[1]}]
        }

        # Simplified parameter matching via string comparison
        baseline_param_str = benchmark.get("Parameters", "")
        current_param_str = ", ".join(f"{k}={v}" for k, v in params.items())

        if baseline_param_str == current_param_str:
            return benchmark

    return None


def extract_params(benchmark: dict) -> dict:
    """Extract parameters from a benchmark entry."""
    param_str = benchmark.get("Parameters", "")
    if not param_str:
        return {}

    params = {}
    for pair in param_str.split(", "):
        if "=" in pair:
            key, value = pair.split("=", 1)
            params[key] = value
    return params


def get_p95(benchmark: dict) -> float | None:
    """Extract P95 latency from benchmark statistics."""
    stats = benchmark.get("Statistics", {})
    return stats.get("P95")


def check_regressions(baseline_path: Path, current_path: Path, threshold_pct: float) -> int:
    """
    Compare current results against baseline and report regressions.

    Returns:
        0 if no regressions, 1 if regressions detected
    """
    print(f"📊 Checking performance regressions (threshold: {threshold_pct}%)")
    print(f"   Baseline: {baseline_path}")
    print(f"   Current:  {current_path}")
    print()

    baseline = load_json(baseline_path)
    current = load_json(current_path)

    regressions = []
    improvements = []
    no_baseline = []

    for benchmark in current.get("Benchmarks", []):
        name = benchmark.get("Method", "Unknown")
        params = extract_params(benchmark)
        param_key = f"{name}({benchmark.get('Parameters', '')})"

        baseline_entry = find_baseline(baseline, name, params)
        if not baseline_entry:
            no_baseline.append(param_key)
            continue

        baseline_p95 = get_p95(baseline_entry)
        current_p95 = get_p95(benchmark)

        if baseline_p95 is None or current_p95 is None:
            print(f"⚠️  Missing P95 data for {param_key}")
            continue

        change_pct = ((current_p95 - baseline_p95) / baseline_p95) * 100

        if change_pct > threshold_pct:
            regressions.append({
                "name": param_key,
                "baseline_p95": baseline_p95,
                "current_p95": current_p95,
                "change_pct": change_pct
            })
            print(f"🔴 REGRESSION: {param_key}")
            print(f"   Baseline P95: {baseline_p95:.2f}ns → Current P95: {current_p95:.2f}ns")
            print(f"   Change: +{change_pct:.1f}%")
        elif change_pct < -threshold_pct:
            improvements.append({
                "name": param_key,
                "baseline_p95": baseline_p95,
                "current_p95": current_p95,
                "change_pct": change_pct
            })
            print(f"🟢 IMPROVEMENT: {param_key}")
            print(f"   Baseline P95: {baseline_p95:.2f}ns → Current P95: {current_p95:.2f}ns")
            print(f"   Change: {change_pct:.1f}%")
        else:
            print(f"✅ OK: {param_key}")
            print(f"   Baseline P95: {baseline_p95:.2f}ns → Current P95: {current_p95:.2f}ns")
            print(f"   Change: {change_pct:+.1f}%")

        print()

    # Summary
    print("=" * 60)
    print("SUMMARY")
    print("=" * 60)

    if no_baseline:
        print(f"\n⚠️  {len(no_baseline)} benchmark(s) without baseline:")
        for name in no_baseline:
            print(f"   - {name}")

    if improvements:
        print(f"\n🟢 {len(improvements)} improvement(s) detected")

    if regressions:
        print(f"\n🚨 {len(regressions)} REGRESSION(S) DETECTED!")
        print("   The following benchmarks exceeded the regression threshold:")
        for reg in regressions:
            print(f"   - {reg['name']}: +{reg['change_pct']:.1f}%")
        return 1
    else:
        print("\n✅ No regressions detected")
        return 0


def main():
    parser = argparse.ArgumentParser(
        description="Check for performance regressions in benchmark results."
    )
    parser.add_argument(
        "baseline",
        type=Path,
        help="Path to baseline JSON file"
    )
    parser.add_argument(
        "current",
        type=Path,
        help="Path to current benchmark results JSON file"
    )
    parser.add_argument(
        "--threshold",
        type=float,
        default=10.0,
        help="Regression threshold percentage (default: 10)"
    )

    args = parser.parse_args()

    if not args.baseline.exists():
        print(f"❌ Error: Baseline file not found: {args.baseline}")
        sys.exit(2)

    if not args.current.exists():
        print(f"❌ Error: Current results file not found: {args.current}")
        sys.exit(2)

    exit_code = check_regressions(args.baseline, args.current, args.threshold)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
