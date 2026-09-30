#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    public static class DeterministicRng
    {
        public static uint Next(ref uint state)
        {
            if (state == 0) throw new ArgumentException("RNG state must be nonzero.");
            unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; }
            return state;
        }
        public static int Below(ref uint state, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            var bound = (uint)count; var threshold = unchecked(0u - bound) % bound;
            uint draw; do { draw = Next(ref state); } while (draw < threshold);
            return (int)(draw % bound);
        }
        public static uint Seed(ulong seed, string stream)
        {
            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes("startup-life/rng/v1/" + seed.ToString(CultureInfo.InvariantCulture) + "/" + stream));
                var value = (uint)bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24);
                return value == 0 ? 1 : value;
            }
        }
    }
    public static class QuotaScheduler
    {
        public static Dictionary<string, int> Apportion(IReadOnlyList<CareerSceneDefinition> scenes, int slots)
        {
            if (slots < 0 || scenes.Sum(x => x.Weight) != 100) throw new ArgumentException("Invalid quota.");
            var counts = scenes.ToDictionary(x => x.Id, x => checked((int)((long)slots * x.Weight / 100)), StringComparer.Ordinal);
            var missing = slots - counts.Values.Sum();
            foreach (var scene in scenes.OrderByDescending(x => (long)slots * x.Weight % 100).ThenBy(x => x.Id, StringComparer.Ordinal).Take(missing))
                counts[scene.Id]++;
            return counts;
        }
        public static List<string> Order(Dictionary<string, int> counts, string previous, ref uint rng)
        {
            var remaining = new Dictionary<string, int>(counts, StringComparer.Ordinal); var deck = new List<string>();
            while (remaining.Values.Any(x => x > 0))
            {
                var choices = remaining.Where(x => x.Value > 0).ToList();
                if (choices.Any(x => x.Key != previous)) choices.RemoveAll(x => x.Key == previous);
                var highest = choices.Max(x => x.Value);
                var ties = choices.Where(x => x.Value == highest).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var selected = ties[DeterministicRng.Below(ref rng, ties.Length)];
                deck.Add(selected); remaining[selected]--; previous = selected;
            }
            return deck;
        }
        public static string Consume(GameState state, CareerDefinition career)
        {
            var scheduler = state.Scheduler;
            var signature = career.Id + "/" + career.Revision + "/" + state.Employment!.Rank.ToString(CultureInfo.InvariantCulture);
            var rng = state.SchedulerRng;
            if (scheduler.Cursor >= scheduler.Deck.Count)
            {
                scheduler.Deck = Order(Apportion(career.Scenes, career.QuotaSlots), scheduler.LastScene, ref rng);
                scheduler.Cursor = 0; scheduler.Cycle = checked(scheduler.Cycle + 1); scheduler.Signature = signature;
                scheduler.Generation = checked(scheduler.Generation + 1);
            }
            else if (scheduler.Signature != signature)
            {
                var suffix = Order(Apportion(career.Scenes, scheduler.Deck.Count - scheduler.Cursor), scheduler.LastScene, ref rng);
                scheduler.Deck = scheduler.Deck.Take(scheduler.Cursor).Concat(suffix).ToList();
                scheduler.Signature = signature; scheduler.Generation = checked(scheduler.Generation + 1);
            }
            var scene = scheduler.Deck[scheduler.Cursor++]; scheduler.LastScene = scene; state.SchedulerRng = rng;
            return scene;
        }
    }
}
