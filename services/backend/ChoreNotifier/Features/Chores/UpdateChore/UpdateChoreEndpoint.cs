using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Chores.UpdateChore;

public class UpdateChoreEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/chores/{choreId:int}", async (int choreId, UpdateChoreDto dto, ISender sender) =>
            {
                var request = new UpdateChoreRequest(
                    choreId,
                    dto.Title,
                    dto.Description,
                    dto.ChoreSchedule,
                    dto.SnoozeDuration
                );
                var result = await sender.Send(request);
                return result.ToResponse();
            })
            .WithName("UpdateChore")
            .WithTags("Chores");
    }
}

public record UpdateChoreDto(
    string Title,
    string? Description,
    CreateChore.CreateChoreScheduleRequest? ChoreSchedule,
    TimeSpan SnoozeDuration
);
