using okoshko.Models;
using okoshko.Utils;

namespace okoshko.Services;

public interface ITextRangingService
{
    Task ProcessAsync(string filePath, RangerAction action, int blockSize);
}

public sealed class TextRangingService : ITextRangingService
{
    public Task ProcessAsync(string filePath, RangerAction action, int blockSize)
    {
        return Task.Run(() =>
        {
            switch (action)
            {
                case RangerAction.Encode:
                    TextRangingUtil.Encode(filePath, blockSize);
                    break;

                case RangerAction.Decode:
                    TextRangingUtil.Decode(filePath);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        });
    }
}