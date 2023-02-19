using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace ComputerGraphics.StaticColorCorrection.App.ColorSpaces
{
    public class Rgb : BaseColorSpace
    {
        public Rgb(List<Matrix<double>> source) : base(source)
        {
        }

        public override T ChangeColorSpace<T>()
        {
            var className = typeof(T).Name;
            var convertorMethod = typeof(Rgb).GetMethod("To" + className);
            if (convertorMethod == null)
            {
                throw new ArgumentException($"Метод To{className} для преобразования не реализован.");
            }
            return (T)convertorMethod.Invoke(this, null);
        }

        public Lms ToLms()
        {
            return new Lms(ImageColorSpaceContainer.AsParallel().AsOrdered()
                .Select(x => ColorSpaceHelper.RGBtoLMS * x.Map(y => Math.Max(y, 3d / 255)))
                .ToList());
        }

        public Hsl ToHsl()
        {
            var hslPixels = new List<Matrix<double>>(ImageColorSpaceContainer.Count);

            foreach (var t in ImageColorSpaceContainer)
            {
                var r = t[0, 0];
                var g = t[1, 0];
                var b = t[2, 0];

                var max = Math.Max(Math.Max(r, g), b);
                var min = Math.Min(Math.Min(r, g), b);

                var h = 0.0;
                var s = 0.0;
                var l = (max + min) / 2.0;

                if (Math.Abs(max - min) > 0.0001)
                {
                    var d = max - min;
                    s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

                    if (Math.Abs(max - r) < 0.0001)
                    {
                        h = ((g - b) / 6) / d;
                    }
                    else if (Math.Abs(max - g) < 0.0001)
                    {
                        h = (1.0d / 3) + ((b - r) / 6) / d;
                    }
                    else if (Math.Abs(max - b) < 0.0001)
                    {
                        h = (2.0d / 3) + ((r - g) / 6) / d;
                    }

                    if (h < 0)
                        h += 1;
                    if (h > 1)
                        h -= 1;
                }

                hslPixels.Add(Matrix<double>.Build.DenseOfColumnArrays(new[] { 360*h, s, l }));
            }

            return new Hsl(hslPixels);
        }
    }
}
