namespace Modine
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            using Game game = new Game(1920, 1080, "Modine");
            game.Run();
        }
    }
}