using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Users.UpdateUser;

public class UpdateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/{userId:int}", async (int userId, UpdateUserDto dto, ISender sender) =>
            {
                var request = new UpdateUserRequest { UserId = userId, Name = dto.Name };
                var result = await sender.Send(request);
                return result.ToResult();
            })
            .WithName("UpdateUser")
            .WithTags("Users");
    }
}

public record UpdateUserDto(string Name);
