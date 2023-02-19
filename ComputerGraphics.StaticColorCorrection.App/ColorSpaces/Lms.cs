using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace ComputerGraphics.StaticColorCorrection.App.ColorSpaces
{
    public class Lms : BaseColorSpace
    {
        public Lms(List<Matrix<double>> source) : base(source)
        {

        }

        public override T ChangeColorSpace<T>()
        {
            var className = typeof(T).Name;
            var convertorMethod = typeof(Lms).GetMethod("To" + className);
            if (convertorMethod == null)
            {
                throw new ArgumentException($"Метод To{className} для преобразования не реализован.");
            }
            return (T)convertorMethod.Invoke(this, null);
        }

        public Lab ToLab()
        {
            return new Lab(ImageColorSpaceContainer.AsParallel().AsOrdered()
                .Select(x => ColorSpaceHelper.LMStoLAB * ColorSpaceHelper.LmstoLab * x.Map(y => y == 0 ? Math.Log10(3d/255) : Math.Log10(y)))
                .ToList());
        }

        public Rgb ToRgb()
        {
            return new Rgb(ImageColorSpaceContainer.AsParallel().AsOrdered()
                .Select(x => ColorSpaceHelper.LmsToRgb * x.Map(y  => Math.Pow(10, y))).ToList());
        }
    }
}