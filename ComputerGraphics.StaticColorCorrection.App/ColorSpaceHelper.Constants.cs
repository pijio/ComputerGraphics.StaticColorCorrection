using MathNet;
using MathNet.Numerics.LinearAlgebra;

namespace ComputerGraphics.StaticColorCorrection.App
{
    public partial class ColorSpaceHelper
    {
        /// <summary>
        /// Матрица использующаяся для перехода из RGB в LMS
        /// </summary>
        public static Matrix<double> RGBtoLMS = Matrix<double>.Build.DenseOfArray(new double[,] {
            {0.3811, 0.5783, 0.0402},
            {0.1967, 0.7244, 0.0782},
            {0.0241, 0.1288, 0.8444}
        });

        /// <summary>
        /// Матрица использующаяся для перехода из LMS в lab
        /// </summary>
        public static Matrix<double> LMStoLAB = Matrix<double>.Build.DenseOfArray(new double[,] {
            {0.5774, 0.0, 0.0},
            {0.0, 0.4082, 0.0},
            {0.0, 0.0, 0.7071}
        });
        
        /// <summary>
        /// матрица использующаяся для переходов из LMS в lab и из lab в RGB
        /// </summary>
        public static Matrix<double> LmstoLab = Matrix<double>.Build.DenseOfArray(new double[,] {
            {1.0, 1.0, 1.0},
            {1.0, 1.0, -2.0},
            {1.0, -1.0, 0.0}
        });

        /// <summary>
        /// матрица использующаяся для переходов из LMS в lab и из lab в RGB
        /// </summary>
        public static Matrix<double> LabToLms = Matrix<double>.Build.DenseOfArray(new double[,] {
            {1.0, 1.0, 1.0},
            {1.0, 1.0, -1.0},
            {1.0, -2.0, 0.0}
        });

        /// <summary>
        /// матрица использующаяся для переходов из LMS в RGB
        /// </summary>
        public static Matrix<double> LmsToRgb = Matrix<double>.Build.DenseOfArray(new double[,] {
            {4.4679, -3.5873, 0.1193},
            {-1.2186, 2.3809, -0.1624},
            {0.0497, -0.2439, 1.2045}
        });

    }
}
