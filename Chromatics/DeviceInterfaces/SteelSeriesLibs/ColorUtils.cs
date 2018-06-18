using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Chromatics.DeviceInterfaces.SteelSeriesLibs
{
    /// <summary>
    /// Various color utilities
    /// </summary>
    public static class ColorUtils
    {
        private static Random randomizer = new Random();

        /// <summary>
        /// Multiplies a byte by a specified double balue
        /// </summary>
        /// <param name="color">Part of the color, as a byte</param>
        /// <param name="value">The value to multiply the byte by</param>
        /// <returns>The color byte</returns>
        public static byte ColorByteMultiplication(byte color, double value)
        {
            byte returnbyte = color;

            if ((double)returnbyte * value >= 255.0)
                returnbyte = 255;
            else if ((double)returnbyte * value <= 0.0)
                returnbyte = 0;
            else
                returnbyte = (byte)((double)returnbyte * value);

            return returnbyte;
        }


        /// <summary>
        /// Multiplies a Drawing Color instance by a scalar value
        /// </summary>
        /// <param name="color">The color to be multiplied</param>
        /// <param name="scalar">The scalar amount for multiplication</param>
        /// <returns>The multiplied Color</returns>
        public static System.Drawing.Color MultiplyColorByScalar(System.Drawing.Color color, double scalar)
        {
            int Red = ColorByteMultiplication(color.R, scalar);
            int Green = ColorByteMultiplication(color.G, scalar);
            int Blue = ColorByteMultiplication(color.B, scalar);
            int Alpha = ColorByteMultiplication(color.A, scalar);

            return System.Drawing.Color.FromArgb(Alpha, Red, Green, Blue);
        }


        /// <summary>
        /// Generates a random color
        /// </summary>
        /// <returns>A random color</returns>
        public static System.Drawing.Color GenerateRandomColor()
        {
            return System.Drawing.Color.FromArgb(randomizer.Next(255), randomizer.Next(255), randomizer.Next(255));
        }

        /// <summary>
        /// Generates a random color within a certain base color range
        /// </summary>
        /// <param name="baseColor">A base color range</param>
        /// <returns>A random color within a base range</returns>
        public static System.Drawing.Color GenerateRandomColor(System.Drawing.Color baseColor)
        {
            int red = (randomizer.Next(255) + baseColor.R) / 2;
            int green = (randomizer.Next(255) + baseColor.G) / 2;
            int blue = (randomizer.Next(255) + baseColor.B) / 2;
            int alpha = (255 + baseColor.A) / 2;

            return System.Drawing.Color.FromArgb(alpha, red, green, blue);
        }

        public static Color GetColorFromInt(int interger)
        {
            if (interger < 0)
                interger = 0;
            else if (interger > 16777215)
                interger = 16777215;

            int R = interger >> 16;
            int G = (interger >> 8) & 255;
            int B = interger & 255;

            return Color.FromArgb(R, G, B);
        }

        public static int GetIntFromColor(Color color)
        {
            return (color.R << 16) | (color.G << 8) | (color.B);
        }

        /// <summary>
        /// Returns a Luma coefficient for brightness of a color
        /// </summary>
        /// <param name="color">Color to be evaluated</param>
        /// <returns>The brightness of the color. [0 = Dark, 255 = Bright]</returns>
        public static byte GetColorBrightness(System.Drawing.Color color)
        {
            //Source: http://stackoverflow.com/a/12043228
            return (byte)(0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B);
        }

    }

}
