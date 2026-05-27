using System.IO;
using System.Numerics;
using System.Text;

namespace okoshko.Services;

public static class TextRangingUtil
{
    private static readonly byte[] Header = Encoding.ASCII.GetBytes("SSR");

    public static void Encode(string inputFilePath, int blockSize)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Файл не найден.", inputFilePath);

        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(blockSize));

        char[] data = File.ReadAllText(inputFilePath).ToCharArray();

        var charToRank = data
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select((group, index) => new
            {
                Character = group.Key,
                Rank = index + 1
            })
            .ToDictionary(x => x.Character, x => x.Rank);

        char[][] blocks = SplitIntoBlocks(data, blockSize);
        BigInteger[] encodedBlocks = EncodeAllBlocks(blocks, charToRank);

        string outputPath = Path.ChangeExtension(inputFilePath, ".ssr");

        WriteEncodedFile(
            outputPath,
            encodedBlocks,
            blockSize,
            charToRank,
            data.Length);
    }

    public static void Decode(string inputFilePath)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Файл не найден.", inputFilePath);

        var decodedFile = ReadEncodedFile(inputFilePath);

        var rankToChar = decodedFile.CharToRank
            .ToDictionary(pair => pair.Value, pair => pair.Key);

        char[] decodedText = DecodeAllBlocks(
            decodedFile.EncodedBlocks,
            decodedFile.BlockSize,
            decodedFile.OriginalTextLength,
            rankToChar);

        string outputPath = Path.ChangeExtension(inputFilePath, ".decoded.txt");

        File.WriteAllText(outputPath, new string(decodedText));
    }

    private static char[][] SplitIntoBlocks(char[] data, int blockSize)
    {
        int blockCount = (int)Math.Ceiling((double)data.Length / blockSize);
        var blocks = new char[blockCount][];

        for (int i = 0; i < blockCount; i++)
        {
            int start = i * blockSize;
            int length = Math.Min(blockSize, data.Length - start);

            blocks[i] = data
                .Skip(start)
                .Take(length)
                .ToArray();
        }

        return blocks;
    }

    private static BigInteger[] EncodeAllBlocks(
        char[][] blocks,
        Dictionary<char, int> charToRank)
    {
        var result = new BigInteger[blocks.Length];

        for (int i = 0; i < blocks.Length; i++)
            result[i] = EncodeBlock(blocks[i], charToRank);

        return result;
    }

    private static BigInteger EncodeBlock(
        char[] block,
        Dictionary<char, int> charToRank)
    {
        BigInteger result = 0;
        int baseValue = charToRank.Count;

        foreach (char c in block)
        {
            int rank = charToRank[c] - 1;
            result = result * baseValue + rank;
        }

        return result;
    }

    private static char[] DecodeAllBlocks(
        BigInteger[] encodedBlocks,
        int blockSize,
        int originalTextLength,
        Dictionary<int, char> rankToChar)
    {
        var result = new List<char>(originalTextLength);

        for (int i = 0; i < encodedBlocks.Length; i++)
        {
            int currentBlockSize =
                i == encodedBlocks.Length - 1
                    ? originalTextLength - blockSize * i
                    : blockSize;

            char[] block = DecodeBlock(
                encodedBlocks[i],
                rankToChar,
                currentBlockSize);

            result.AddRange(block);
        }

        return result.ToArray();
    }

    private static char[] DecodeBlock(
        BigInteger encoded,
        Dictionary<int, char> rankToChar,
        int blockSize)
    {
        var result = new char[blockSize];
        int baseValue = rankToChar.Count;

        for (int i = blockSize - 1; i >= 0; i--)
        {
            int rank = (int)(encoded % baseValue);
            result[i] = rankToChar[rank + 1];
            encoded /= baseValue;
        }

        return result;
    }

    private static void WriteEncodedFile(
        string outputPath,
        BigInteger[] encodedBlocks,
        int blockSize,
        Dictionary<char, int> charToRank,
        int originalTextLength)
    {
        int encodedBlockByteSize = GetMaxEncodedBlockByteSize(encodedBlocks);

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write(Header);
        writer.Write(originalTextLength);
        writer.Write(blockSize);
        writer.Write(charToRank.Count);
        writer.Write(encodedBlocks.Length);
        writer.Write(encodedBlockByteSize);

        foreach (char c in charToRank.OrderBy(pair => pair.Value).Select(pair => pair.Key))
            writer.Write((ushort)c);

        foreach (BigInteger block in encodedBlocks)
        {
            byte[] bytes = block.ToByteArray(isUnsigned: true, isBigEndian: true);

            if (bytes.Length < encodedBlockByteSize)
            {
                var padded = new byte[encodedBlockByteSize];
                Buffer.BlockCopy(
                    bytes,
                    0,
                    padded,
                    encodedBlockByteSize - bytes.Length,
                    bytes.Length);

                bytes = padded;
            }

            writer.Write(bytes);
        }
    }

    private static EncodedFile ReadEncodedFile(string inputPath)
    {
        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        byte[] header = reader.ReadBytes(Header.Length);

        if (!header.SequenceEqual(Header))
            throw new InvalidDataException("Неверный формат SSR-файла.");

        int originalTextLength = reader.ReadInt32();
        int blockSize = reader.ReadInt32();
        int charCount = reader.ReadInt32();
        int blockCount = reader.ReadInt32();
        int encodedBlockByteSize = reader.ReadInt32();

        var charToRank = new Dictionary<char, int>();

        for (int i = 0; i < charCount; i++)
        {
            char c = (char)reader.ReadUInt16();
            charToRank[c] = i + 1;
        }

        var encodedBlocks = new BigInteger[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            byte[] bytes = reader.ReadBytes(encodedBlockByteSize);
            encodedBlocks[i] = new BigInteger(
                bytes,
                isUnsigned: true,
                isBigEndian: true);
        }

        return new EncodedFile(
            encodedBlocks,
            blockSize,
            charToRank,
            originalTextLength);
    }

    private static int GetMaxEncodedBlockByteSize(BigInteger[] encodedBlocks)
    {
        if (encodedBlocks.Length == 0)
            return 1;

        return encodedBlocks
            .Select(block => block.ToByteArray(isUnsigned: true, isBigEndian: true).Length)
            .Max();
    }

    private sealed record EncodedFile(
        BigInteger[] EncodedBlocks,
        int BlockSize,
        Dictionary<char, int> CharToRank,
        int OriginalTextLength);
}