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
            return new Lab(ImageColorSpaceContainer.AsParallel()
                .Select(x => ColorSpaceHelper.LMStoLAB * ColorSpaceHelper.LmstoLabtoRgb * x.Map(y => y == 0 ? Math.Log((double)3/255) : Math.Log(y)))
                .AsSequential()
                .ToList());
        }

        public Rgb ToRgb()
        {
            return new Rgb(ImageColorSpaceContainer.AsParallel()
                .Select(x => ColorSpaceHelper.LmsToRgb * x.Map(y => Math.Pow(10, y))).AsSequential().ToList());
        }
    }
}