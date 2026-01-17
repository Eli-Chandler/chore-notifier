using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Statistics.GetUserStatistics;

public class GetUserStatisticsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/statistics/users/{userId:int}", async (int userId, DateTimeOffset? startDate,
                DateTimeOffset? endDate, ISender sender) =>
            {
                var request = new GetUserStatisticsRequest(userId, startDate, endDate);
                var result = await sender.Send(request);
                return result.ToResponse();
            })
            .WithName("GetUserStatistics")
            .WithTags("Statistics")
            .WithProblemDetails();
    }
}
