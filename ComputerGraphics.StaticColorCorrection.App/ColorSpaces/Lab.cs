using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace ComputerGraphics.StaticColorCorrection.App.ColorSpaces
{
    public class Lab : BaseColorSpace
    {
        public Lab(List<Matrix<double>> source) : base(source)
        {

        }

        public override T ChangeColorSpace<T>()
        {
            var className = typeof(T).Name;
            var convertorMethod = typeof(Lab).GetMethod("To" + className);
            if (convertorMethod == null)
            {
                throw new ArgumentException($"Метод To{className} для преобразования не реализован.");
            }
            return (T)convertorMethod.Invoke(this, null);
        }

        public Lms ToLms()
        {
            return new Lms(ImageColorSpaceContainer.AsParallel()
                .Select(x => ColorSpaceHelper.LmstoLabtoRgb * ColorSpaceHelper.LMStoLAB * x)
                .AsSequential()
                .ToList());
        }
    }
}