namespace Modine
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            int selection = 0;

            while (selection < 1 || selection > 3)
            {
                Console.WriteLine("Please select a scene:");
                Console.WriteLine("1. Simple plane and a cube");
                Console.WriteLine("2. 255 Point Lights");
                Console.WriteLine("3. 255 Meshes, 3.500.000 triangles");

                string userInput = Console.ReadLine();

                if (!int.TryParse(userInput, out selection))
                {
                    Console.WriteLine("Invalid input. Please enter a valid number between 1-3.");
                }
                else if (selection < 1 || selection > 3)
                {
                    Console.WriteLine("Number must be between 1-3.");
                }
            }

            using Game game = new Game(1920, 1080, "Modine", selection);
            game.Run();
        }
    }
}