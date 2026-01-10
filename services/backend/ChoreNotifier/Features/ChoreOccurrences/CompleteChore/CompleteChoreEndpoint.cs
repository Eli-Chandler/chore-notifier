using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.ChoreOccurrences.CompleteChore;

public class CompleteChoreEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chore-occurrences/{choreOccurrenceId:int}/complete",
            async (int choreOccurrenceId, CompleteChoreDto dto, ISender sender) =>
            {
                var result = await sender.Send(new CompleteChoreRequest(dto.UserId, choreOccurrenceId));
                return result.ToResult();
            })
            .WithName("CompleteChore")
            .WithTags("Chore Occurrences");
    }
}

public record CompleteChoreDto(int UserId);
