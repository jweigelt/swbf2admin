using MoonSharp.Interpreter;
namespace SWBF2Admin.Structures
{
    [MoonSharpUserData]
    public class PlayerGroup
    {
        public long Id { get; }
        public long Level { get; }
        public string Name { get; }
        public string WelcomeMessage { get; }
        public bool EnableWelcome { get; }

        public PlayerGroup(long id, long level, string name, string welcomeMessage, bool enableWelcome)
        {
            Id = id;
            Level = level;
            Name = name;
            WelcomeMessage = welcomeMessage;
            EnableWelcome = enableWelcome;
        }
    }
}