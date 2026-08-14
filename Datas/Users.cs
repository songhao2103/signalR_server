using SignalingServer.Models;

public static class UserStore
{
    public static List<User> Users = new()
    {
        new User
        {
            Id = 1,
            Name = "Skypie",
        },
        new User
        {
            Id = 2,
            Name = "Dragonfly",
        }
    };

    public static User? Find(int id)
    {
        var user = Users.FirstOrDefault(x => x.Id == id);

        if (user == null)
            throw new Exception($"Không tìm thấy user với ID: {id}");

        return user;
    }
}