using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace ComputerGraphics.StaticColorCorrection.App
{
    public abstract class BaseColorSpace
    {
        public List<Matrix<double>> ImageColorSpaceContainer { get; }

        public abstract T ChangeColorSpace<T>() where T : BaseColorSpace;

        protected BaseColorSpace(List<Matrix<double>> source)
        {
            ImageColorSpaceContainer = source;
        }
    }
}
