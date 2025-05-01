using System.Drawing;

using Colorful;

using Console = Colorful.Console;
namespace FructoseLib.CLI.Visual.Dynamic
{
    public static class ConsoleQuestion
    {
        public static bool GetYesNoAnswer(string Text)
        {
            Formatter[] QuestionFormat = new Formatter[]
            {
                new Formatter("Y", System.Drawing.Color.GreenYellow),
                new Formatter("n", System.Drawing.Color.OrangeRed),
            };

            Console.WriteFormatted($" {Text.Replace("?", string.Empty)}? [{{0}}/{{1}}]: ", System.Drawing.Color.White, QuestionFormat);
            while (true)
            {
                var Key = Console.ReadKey().KeyChar.ToString().ToLower();
                Console.Write("\b \b");

                if (Key == "y" || Key == "н")
                {
                    Console.WriteLine("Yes\n", System.Drawing.Color.GreenYellow);
                    return true;
                }
                else if (Key == "n" || Key == "т")
                {
                    Console.WriteLine("No\n", System.Drawing.Color.OrangeRed);
                    return false;
                }
                else
                {
                    continue;
                }
            }
        }

        public static bool GetYesNoAnswerStyled(string Text, string Regex, System.Drawing.Color Color)
        {
            StyleSheet StyleSheet = new StyleSheet(System.Drawing.Color.White);
            StyleSheet.AddStyle(Regex, Color);

            Formatter[] QuestionFormat = new Formatter[]
            {
                new Formatter("Y", System.Drawing.Color.GreenYellow),
                new Formatter("n", System.Drawing.Color.OrangeRed),
            };

            Console.WriteStyled($" {Text.Replace("?", string.Empty)}?", StyleSheet);
            Console.WriteFormatted($" [{{0}}/{{1}}]: ", System.Drawing.Color.White, QuestionFormat);

            while (true)
            {
                var Key = Console.ReadKey().KeyChar.ToString().ToLower();
                Console.Write("\b \b");

                if (Key == "y" || Key == "н")
                {
                    Console.WriteLine("Yes", System.Drawing.Color.GreenYellow);
                    return true;
                }
                else if (Key == "n" || Key == "т")
                {
                    Console.WriteLine("No", System.Drawing.Color.OrangeRed);
                    return false;
                }
                else
                {
                    continue;
                }
            }
        }
    }
}
