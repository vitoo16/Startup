"""Offline regression tests for non-suppressing Unity Console diagnosis."""

import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[2] / 'scripts/Triage-UnityConsole.py'
spec = importlib.util.spec_from_file_location('unity_console_triage', SCRIPT)
triage = importlib.util.module_from_spec(spec)
spec.loader.exec_module(triage)


class ConsoleTriageTests(unittest.TestCase):
    def test_known_unity_core_sampling_warning_requires_review(self):
        warning = ("Shader warning in 'TraceVirtualOffset': conversion from larger "
                   "type 'uint' to smaller type 'min16float', possible loss of data "
                   "[-Wconversion] at Z:\\workspace\\Startup\\Library\\PackageCache\\"
                   "com.unity.render-pipelines.core@hash\\ShaderLibrary\\Sampling\\"
                   "Fibonacci.hlsl(16) (on d3d11)")
        self.assertEqual('external_urp_precision_warning', triage.classify_line(warning))
        report = triage.analyze([warning])
        self.assertEqual('REVIEW_REQUIRED', report['status'])
        self.assertFalse(report['unity_verified'])

    def test_unknown_or_project_shader_warnings_are_never_allowlisted(self):
        for path in (
            'Assets/Shaders/Fibonacci.hlsl(16)',
            'Library/PackageCache/com.other.pkg@xyz/ShaderLibrary/Sampling/Fibonacci.hlsl(16)',
            'Library/PackageCache/com.unity.render-pipelines.core@xyz/ShaderLibrary/Other.hlsl(16)',
        ):
            warning = "Shader warning in 'TraceVirtualOffset': conversion [-Wconversion] at " + path
            self.assertEqual('unclassified_warning', triage.classify_line(warning))
        self.assertEqual('unclassified_warning', triage.classify_line(
            "Shader warning in 'CustomGameShader': possible loss of data at Assets/Test.hlsl(6)"))

    def test_unity_ai_no_subscription_not_game_compile_failure(self):
        warning = ("Error reason is 'NoSubscription' and no additional error information "
                   "was provided (https://generators.ai.unity.com).")
        self.assertEqual('external_unity_ai_subscription_error', triage.classify_line(warning))
        self.assertEqual('unclassified_error', triage.classify_line(
            "Error reason is 'NoSubscription' (https://other-service.example)."))

    def test_localization_mutation_requires_asset_review(self):
        report = triage.analyze(['Startup Life: imported 73 Vietnamese dashboard entries.'])
        self.assertEqual(1, report['counts']['localization_table_modified'])
        self.assertEqual('REVIEW_REQUIRED', report['status'])

    def test_unknown_errors_and_warnings_never_disappear(self):
        report = triage.analyze([
            'Shader error in MyShader: undeclared identifier',
            'Assets/Game.cs(12,3): error CS0103: The name does not exist',
            'InvalidOperationException: Boom',
            'Shader warning in MyShader: uncertain behavior',
            'Warning: missing asset',
            'UnityEngine.Debug:Log (object)',
            'UnityEngine.UnitySynchronizationContext:ExecuteTasks ()',
        ])
        self.assertEqual(3, report['counts']['unclassified_error'])
        self.assertEqual(2, report['counts']['unclassified_warning'])
        self.assertEqual('UNCLASSIFIED_ERRORS_REQUIRE_INVESTIGATION', report['status'])
        self.assertEqual(5, len(report['findings']))

    def test_empty_or_successful_log_is_not_unity_test_pass(self):
        self.assertEqual('NO_RECOGNIZED_DIAGNOSTICS', triage.analyze([])['status'])
        self.assertFalse(triage.analyze(['All tests passed'])['unity_verified'])

    def test_cli_json_and_strict_error_exit(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'Editor.log'
            path.write_text('Shader error in App: failure\n', encoding='utf-8')
            command = [sys.executable, str(SCRIPT), str(path), '--format', 'json',
                       '--fail-on-unclassified-error']
            completed = subprocess.run(command, capture_output=True, text=True, check=False)
            self.assertEqual(3, completed.returncode)
            self.assertIn('UNCLASSIFIED_ERRORS_REQUIRE_INVESTIGATION', completed.stdout)


if __name__ == '__main__':
    unittest.main()
