using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.ChoreOccurrences.SnoozeChore;

public class SnoozeChoreEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chore-occurrences/{choreOccurrenceId:int}/snooze",
            async (int choreOccurrenceId, SnoozeChoreDto dto, ISender sender) =>
            {
                var result = await sender.Send(new SnoozeChoreRequest(dto.UserId, choreOccurrenceId));
                return result.ToResult();
            })
            .WithName("SnoozeChore")
            .WithTags("Chore Occurrences");
    }
}

public record SnoozeChoreDto(int UserId);
