namespace GameEngine
{
    class Program
    {
        static void Main()
        {
            using Game game = new Game(1280, 786, "Game Engine");
            game.Run();
        }
    }
}