using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace StartupLife.Tests.PlayMode
{
    // The bootstrap's editor diagnostics are intentional and must remain
    // observable. Consume exactly one expected, non-error save-read record
    // before each genuine scene load; never disable unexpected log assertions.
    internal static class SaveBootLogExpectations
    {
#if UNITY_EDITOR
        private static readonly Regex VerifiedRead = new Regex(
            @"^\[StartupLife\]\[SaveBoot\] path=.+ " +
            @"status=(Missing|Valid|RecoveredBackup) " +
            @"primaryExists=(True|False) backupExists=(True|False) " +
            @"revision=(none|[0-9]+) characterPresent=(True|False)$",
            RegexOptions.CultureInvariant);
#endif

        internal static void ExpectVerifiedRead()
        {
#if UNITY_EDITOR
            LogAssert.Expect(LogType.Log, VerifiedRead);
#endif
        }

        internal static void ExpectUnreadablePrimary()
        {
#if UNITY_EDITOR
            LogAssert.Expect(LogType.Log, new Regex(
                @"^\[StartupLife\]\[SaveBoot\] path=.+ status=Unreadable " +
                @"primaryExists=False backupExists=True " +
                @"revision=none characterPresent=False$",
                RegexOptions.CultureInvariant));
#endif
        }
    }
}
