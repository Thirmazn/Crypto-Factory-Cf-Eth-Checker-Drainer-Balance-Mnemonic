using Colorful;

using System.Collections.Generic;
using System.Drawing;

namespace FructoseLib.CLI.Visual.Static
{
    public class ConsoleOption
    {
        public ConsoleOption(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public ConsoleOption(string Name, string Value, System.Drawing.Color KeyColor, System.Drawing.Color ValueColor)
        {
            this.Name = Name;
            this.Value = Value;
            this.KeyColor = KeyColor;
            this.ValueColor = ValueColor;
        }

        public ConsoleOption(string Name, string Value, System.Drawing.Color ValueColor)
        {
            this.Name = Name;
            this.Value = Value;
            this.ValueColor = ValueColor;
        }

        public ConsoleOption(string Name, int Value)
        {
            this.Name = Name;
            this.Value = Value.ToString();
        }

        public ConsoleOption(string Name, int Value, System.Drawing.Color KeyColor, System.Drawing.Color ValueColor)
        {
            this.Name = Name;
            this.Value = Value.ToString();
            this.KeyColor = KeyColor;
            this.ValueColor = ValueColor;
        }

        public ConsoleOption(string Name, int Value, System.Drawing.Color ValueColor)
        {
            this.Name = Name;
            this.Value = Value.ToString();
            this.ValueColor = ValueColor;
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public System.Drawing.Color KeyColor { get; set; } = System.Drawing.Color.White;
        public System.Drawing.Color ValueColor { get; set; } = System.Drawing.Color.White;
    }
    static partial class ConsoleComponents
    {
        public static void RenderOptions(List<ConsoleOption> ConsoleOptions)
        {
            Colorful.Console.WriteLine(" ************* Settings **************\n", System.Drawing.Color.White);

            foreach (var Option in ConsoleOptions)
            {
                Formatter[] OptionFormat = new Formatter[]
                {
                        new Formatter(Option.Name, Option.KeyColor),
                        new Formatter(Option.Value, Option.Value.Equals("None") || Option.Value.Equals("Nothing") || Option.Value.Equals("False") ? System.Drawing.Color.OrangeRed : Option.ValueColor)
                };

                Colorful.Console.WriteLineFormatted(" - {0}: {1}", System.Drawing.Color.White, OptionFormat);
            }

            Colorful.Console.WriteLine("\n *************************************\n", System.Drawing.Color.White);
        }
    }
}
