using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace ComputerGraphics.StaticColorCorrection.App.ColorSpaces
{
    public class Hsl : BaseColorSpace
    {
        public Hsl(List<Matrix<double>> source) : base(source)
        {
        }

        public override T ChangeColorSpace<T>()
        {
            var className = typeof(T).Name;
            var convertorMethod = typeof(Hsl).GetMethod("To" + className);
            if (convertorMethod == null)
            {
                throw new ArgumentException($"Метод To{className} для преобразования не реализован.");
            }
            return (T)convertorMethod.Invoke(this, null);
        }

        public Rgb ToRgb()
        {
            var rgbList = new List<Matrix<double>>();
            var hueToRgb = new Func<double, double, double, double>((v1, v2, vH) =>
            {
                if (vH < 0)
                    vH += 1;

                if (vH > 1)
                    vH -= 1;

                if ((6 * vH) < 1)
                    return (v1 + (v2 - v1) * 6 * vH);

                if ((2 * vH) < 1)
                    return v2;

                if ((3 * vH) < 2)
                    return (v1 + (v2 - v1) * ((2.0f / 3) - vH) * 6);

                return v1;
            });
            foreach (var hsl in ImageColorSpaceContainer)
            {
                var h = hsl[0, 0];
                var s = hsl[1, 0];
                var l = hsl[2, 0];

                double r, g, b;

                if (s == 0)
                {
                    r = g = b = l;
                }
                else
                {
                    double v1, v2;
                    double hue = (double)h / 360;

                    v2 = (l < 0.5) ? (l * (1 + s)) : ((l + s) - (l * s));
                    v1 = 2 * l - v2;

                    r = (hueToRgb(v1, v2, hue + (1.0d / 3)));
                    g = (hueToRgb(v1, v2, hue));
                    b = (hueToRgb(v1, v2, hue - (1.0d / 3)));
                }

                rgbList.Add(Matrix<double>.Build.DenseOfColumnArrays(new[] { r, g, b }));
            }

            return new Rgb(rgbList);
        }
    }
}
