namespace TexasPortal.Application.Common.Interfaces;

public interface ICurrentUser
{
    long? UserId { get; }
    bool IsAuthenticated { get; }
}
