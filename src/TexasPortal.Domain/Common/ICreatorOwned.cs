namespace TexasPortal.Domain.Common;

public interface ICreatorOwned
{
    long? CreatedBy { get; }
}
