#nullable enable
using System;
using System.Globalization;
using System.Numerics;

namespace StartupLife.Core
{
    public readonly struct ContentId : IEquatable<ContentId>, IComparable<ContentId>
    {
        public ContentId(string value)
        {
            if (!IsValid(value))
            {
                throw new ArgumentException("Content IDs must be nonempty lowercase ASCII identifiers.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public static bool IsValid(string? value)
        {
            if (string.IsNullOrEmpty(value) || value![0] == '.' || value[0] == '/' ||
                value[value.Length - 1] == '.' || value[value.Length - 1] == '/')
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                            c == '.' || c == '/' || c == '-' || c == '_';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        public int CompareTo(ContentId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(ContentId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is ContentId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ContentId left, ContentId right) => left.Equals(right);
        public static bool operator !=(ContentId left, ContentId right) => !left.Equals(right);
    }

    public readonly struct SimDate : IEquatable<SimDate>, IComparable<SimDate>
    {
        public SimDate(int year, int month, int day)
        {
            _ = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
            Year = year;
            Month = month;
            Day = day;
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public DayOfWeek DayOfWeek => ToDateTime().DayOfWeek;
        public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

        public SimDate AddDays(int days)
        {
            var date = ToDateTime().AddDays(days);
            return new SimDate(date.Year, date.Month, date.Day);
        }

        public int CompareTo(SimDate other)
        {
            var year = Year.CompareTo(other.Year);
            if (year != 0) return year;
            var month = Month.CompareTo(other.Month);
            return month != 0 ? month : Day.CompareTo(other.Day);
        }

        public bool Equals(SimDate other) => Year == other.Year && Month == other.Month && Day == other.Day;
        public override bool Equals(object? obj) => obj is SimDate other && Equals(other);
        public override int GetHashCode() => ((Year * 397) ^ Month) * 397 ^ Day;
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:D4}-{1:D2}-{2:D2}", Year, Month, Day);
        public static bool operator ==(SimDate left, SimDate right) => left.Equals(right);
        public static bool operator !=(SimDate left, SimDate right) => !left.Equals(right);
        public static bool operator <(SimDate left, SimDate right) => left.CompareTo(right) < 0;
        public static bool operator >(SimDate left, SimDate right) => left.CompareTo(right) > 0;

        private DateTime ToDateTime() => new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Unspecified);
    }

    public readonly struct SimInstant : IEquatable<SimInstant>
    {
        public SimInstant(SimDate date, int minute)
        {
            if (minute < 0 || minute >= 1440)
            {
                throw new ArgumentOutOfRangeException(nameof(minute));
            }

            Date = date;
            Minute = minute;
        }

        public SimDate Date { get; }
        public int Minute { get; }
        public bool Equals(SimInstant other) => Date.Equals(other.Date) && Minute == other.Minute;
        public override bool Equals(object? obj) => obj is SimInstant other && Equals(other);
        public override int GetHashCode() => (Date.GetHashCode() * 397) ^ Minute;
        public override string ToString() => Date + "+" + Minute.ToString(CultureInfo.InvariantCulture);
    }

    public readonly struct RationalAmount : IEquatable<RationalAmount>
    {
        public RationalAmount(BigInteger numerator, BigInteger denominator)
        {
            if (denominator == BigInteger.Zero)
            {
                throw new DivideByZeroException();
            }

            if (denominator < BigInteger.Zero)
            {
                numerator = -numerator;
                denominator = -denominator;
            }

            var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
            Numerator = numerator / gcd;
            Denominator = denominator / gcd;
        }

        public BigInteger Numerator { get; }
        public BigInteger Denominator { get; }
        public static RationalAmount Zero => new RationalAmount(BigInteger.Zero, BigInteger.One);
        public bool IsNegative => Numerator < BigInteger.Zero;
        public bool IsZero => Numerator.IsZero;

        public long FloorToInt64()
        {
            var quotient = BigInteger.Divide(Numerator, Denominator);
            if (Numerator < 0 && Numerator % Denominator != 0)
            {
                quotient -= BigInteger.One;
            }

            if (quotient < long.MinValue || quotient > long.MaxValue)
            {
                throw new OverflowException("Rational amount does not fit in Int64.");
            }

            return (long)quotient;
        }

        public RationalAmount SubtractWhole(long amount) => this - FromWhole(amount);
        public static RationalAmount FromWhole(long amount) => new RationalAmount(new BigInteger(amount), BigInteger.One);
        public static RationalAmount operator +(RationalAmount left, RationalAmount right) =>
            new RationalAmount(left.Numerator * right.Denominator + right.Numerator * left.Denominator,
                left.Denominator * right.Denominator);
        public static RationalAmount operator -(RationalAmount left, RationalAmount right) =>
            new RationalAmount(left.Numerator * right.Denominator - right.Numerator * left.Denominator,
                left.Denominator * right.Denominator);
        public bool Equals(RationalAmount other) => Numerator == other.Numerator && Denominator == other.Denominator;
        public override bool Equals(object? obj) => obj is RationalAmount other && Equals(other);
        public override int GetHashCode() => (Numerator.GetHashCode() * 397) ^ Denominator.GetHashCode();
        public override string ToString() => Numerator.ToString(CultureInfo.InvariantCulture) + "/" + Denominator.ToString(CultureInfo.InvariantCulture);
    }

    public static class CheckedMath
    {
        public static long Add(long left, long right) => checked(left + right);
        public static long Subtract(long left, long right) => checked(left - right);
        public static long Multiply(long left, long right) => checked(left * right);
    }
}
