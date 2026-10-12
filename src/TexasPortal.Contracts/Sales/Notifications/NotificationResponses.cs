using System;

namespace TexasPortal.Contracts.Sales.Notifications;

public record NotificationResponse(
    string Id,
    string Type,
    string NotifiableType,
    long NotifiableId,
    string Data,
    DateTime? ReadAt,
    DateTime? CreatedAt
);
