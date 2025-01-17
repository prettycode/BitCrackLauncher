using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using NBitcoin;

namespace BitCrackLauncher;

internal class BitcoinPuzzle
{
    public readonly string AddressToCrack;
    public readonly BigInteger KeyspaceStartInclusive;
    public readonly BigInteger KeyspaceEndInclusive;

    public BitcoinPuzzle(string addressToCrack, string hexRangeLowInclusive, string hexRangeHighInclusive)
    {
        if (!IsValidAddress(addressToCrack))
        {
            throw new ArgumentException("Invalid public key.", nameof(addressToCrack));
        }

        this.AddressToCrack = addressToCrack;

        if (!BigInteger.TryParse(hexRangeLowInclusive, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out this.KeyspaceStartInclusive))
        {
            throw new ArgumentException("Invalid hexadecimal number.", nameof(hexRangeLowInclusive));
        }

        if (!BigInteger.TryParse(hexRangeHighInclusive, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out this.KeyspaceEndInclusive))
        {
            throw new ArgumentException("Invalid hexadecimal number.", nameof(hexRangeHighInclusive));
        }
    }

    public string GetRandomKeyspace()
    {
        return GetRandomBigInteger(this.KeyspaceStartInclusive, this.KeyspaceEndInclusive).ToString("X");
    }

    public static BitcoinPuzzle GetRandomUnsolvedPuzzle()
    {
        throw new NotImplementedException();
    }

    private static bool IsValidAddress(string address)
    {
        try
        {
            BitcoinAddress.Create(address, Network.Main);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static BigInteger GetRandomBigInteger(BigInteger minValueInclusive, BigInteger maxValueInclusive)
    {
        if (minValueInclusive >= maxValueInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(minValueInclusive), $"Cannot be greater than or equal to {maxValueInclusive}");
        }

        BigInteger range = maxValueInclusive - minValueInclusive + 1;
        int bytesNeeded = range.GetByteCount();
        byte[] bytes = new byte[bytesNeeded];
        BigInteger result;

        do
        {
            RandomNumberGenerator.Fill(bytes);

            // Ensure a positive value
            bytes[bytesNeeded - 1] &= 0x7F;

            result = new BigInteger(bytes);
        }
        while (result < 0 || result >= range);

        return result + minValueInclusive;
    }
}